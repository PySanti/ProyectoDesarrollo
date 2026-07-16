# Orden del listado de partidas del operador (design)

Fecha: 2026-07-16
Origen: reporte del usuario — "en el historial de partidas aparece la lista de partidas creadas pero
desordenadas, debería estar de primera la última que se creó; ¿con qué criterio se está listando?"

## Problema

`GET /partidas` (listado del operador, `PartidasListPage`) devuelve las partidas **sin orden alguno**.

- `PartidaRepository.cs:24` — `ListAsync` es `_dbContext.Partidas.Include(p => p.Juegos).ToListAsync()`:
  **sin `ORDER BY`**.
- `ListPartidasQueryHandler` no ordena: solo mapea a `PartidaSummaryDto`.
- `PartidasListPage.tsx` no ordena en cliente.

Sin `ORDER BY`, Postgres devuelve las filas en el orden físico que le convenga, que además **puede
cambiar** cuando una fila se actualiza. No es que el criterio sea malo: **no hay criterio**.

El contrato (`contracts/http/partidas-config.md:112-118`) tampoco promete ninguno — por eso nadie lo
implementó y nadie lo notó hasta que el operador miró la pantalla.

**El obstáculo real:** `Partida` **no tiene fecha de creación**. Sus campos son `PartidaId`,
`NombrePartida`, `Estado`, `Modalidad`, `ModoInicioPartida`, `TiempoInicio`, mínimos, máximos y
`Juegos` (`Partida.cs:11-20`). `TiempoInicio` es cuándo *arranca* la partida (y es nullable), no
cuándo se creó. Así que "la última creada primero" no está sin implementar: **no es implementable con
el modelo actual**.

## Alcance

Slice acotado: añadir la fecha de creación al dominio de Partidas y ordenar el listado del operador
por ella, descendente, con la fecha visible en la tabla.

**Fuera de alcance:**

- **El panel del participante (`partidas-publicadas`, móvil) tiene el mismo bug** —
  `GetSesionesEnLobbyAsync` tampoco lleva `ORDER BY` y `ListarPartidasPublicadasQueryHandler` no
  ordena—, pero es otro problema disfrazado del mismo síntoma: el operador quiere "lo último que
  creé" (listado de gestión), mientras que al participante eligiendo a cuál unirse probablemente le
  importe cuál arranca antes (`tiempoInicio`) o cuál está por llenarse. Es una decisión de producto
  sin responder, sobre otro servicio (`SesionPartida`, que ni tiene este campo). Slice propio.
- **La columna "Estado" del mismo listado está estructuralmente muerta**: `Partida.Estado` se
  inicializa en `null` (`Partida.cs:39`) y **nada en todo el servicio Partidas lo vuelve a escribir**
  — no existe un método `Publicar`. Por ADR-0010 el estado de runtime vive en Operaciones de Sesión,
  así que `estadoPill(null)` pinta **"Sin publicar"** para siempre, incluso en una partida terminada
  (`PartidasListPage.tsx:131-134`). El comentario de `Partida.cs:13` (*"null = configured, not yet
  published (SP-3 sets Lobby)"*) es **stale**: describe un diseño que ADR-0010 abandonó. Arreglarlo
  es una decisión de arquitectura (¿el web resuelve contra Operaciones? ¿Partidas proyecta desde
  eventos, reabriendo el ADR? ¿la columna desaparece?) que merece brainstorming y probablemente un
  ADR propio.
- Paginación, filtros u ordenamiento configurable por el usuario. YAGNI.

## Decisiones tomadas en brainstorming

Este slice **sí** pasó por `superpowers:brainstorming` (a diferencia del slice de nombres del
2026-07-15, cuyo encabezado homónimo insinuaba un proceso que no se corrió).

1. **La fecha se muestra como columna, no solo ordena.** Alternativa descartada: campo invisible.
   Motivo del usuario: ver la fecha explica el orden; un orden por criterio invisible vuelve a
   parecer arbitrario.
2. **La data existente es descartable** (el proyecto se re-siembra; `docker compose down -v`). Por
   eso `FechaCreacion` es **obligatorio** (`DateTime`, no nullable) en vez del `DateTime?` con
   nulls-al-final que habría hecho falta para preservar filas históricas.
3. **Solo el listado del operador.** El panel del participante queda fuera (ver Alcance).
4. **El reloj entra por `TimeProvider` inyectado en el handler**, y la fecha viaja como **parámetro**
   al dominio. Alternativas descartadas: `DateTime.UtcNow` dentro de `Partida.Crear()` (el dominio
   pasaría a depender del reloj ambiente y **ningún test podría fijar la fecha** — el test de orden,
   que es el que prueba este bug, necesitaría `Sleep` real y aun así dos creaciones seguidas podrían
   caer en el mismo instante) y default de base de datos (la entidad dejaría de ser dueña de su
   estado; los tests de dominio verían `default(DateTime)`).
5. **Se guarda y se muestra el instante completo, no el día.** Ordenar por fecha sola dejaría las
   partidas del mismo día empatadas; mostrar solo el día haría que el operador viera tres partidas de
   hoy y no entendiera por qué una va primera — el orden sería correcto y aun así *parecería*
   arbitrario, que es el problema del que partimos.

**Enfoques descartados de entrada:**

- **Ordenar en el cliente web**: no hay nada por lo que ordenar (el payload no trae fecha). Si se
  añade el campo igual, ordenar en el servidor es estrictamente mejor: es garantía del contrato,
  sirve a cualquier consumidor y sobrevive a una futura paginación.
- **UUIDv7 para `PartidaId`** (GUIDs ordenables por tiempo, sin campo ni migración): choca con la
  decisión 1 —de un UUIDv7 no se saca una fecha honesta que pintar—, .NET 8 no trae
  `Guid.CreateVersion7` (es de .NET 9), y mezcla identidad con tiempo.

## Hechos verificados en código

Anclan el diseño; no repetir la verificación al implementar.

- **`Partida` no tiene fecha de creación** (`Partida.cs:11-20`), y **`PartidaId` es aleatorio**:
  `PartidaId.New() => new(Guid.NewGuid())` (`PartidaId.cs:5`) — sin información temporal dentro.
- **Partidas no tiene reloj**: cero `TimeProvider` y cero `DateTime.UtcNow`/`DateTime.Now` en todo
  `services/partidas/src`. `Partida.Crear(...)` no recibe fecha y `CrearPartidaCommandHandler` solo
  inyecta `IPartidaRepository` + `IPartidasUnitOfWork`.
- **Partidas no publica ningún evento** — sin publisher, sin RabbitMQ. **Corolario:** la fecha de
  creación de las filas existentes es **irrecuperable**: no está en el modelo, ni en el id, ni en
  ningún registro de auditoría de otro servicio. Cualquier valor que se les ponga es inventado.
- **Una sola migración**: `20260625110302_InitialPartidasModel`. El campo pide una segunda.
- **Patrón de reloj de la casa (Operaciones):** `services.AddSingleton(TimeProvider.System)` en su
  `DependencyInjection`; los handlers hacen `var now = _timeProvider.GetUtcNow().UtcDateTime`
  (`EnviarPistaCommandHandler.cs:29`) y **el dominio recibe la fecha como parámetro**, siempre el
  último (`Inscribir(..., fecha)`, `Cancelar(now)`). Sus tests declaran
  `static readonly DateTime T0 = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc)` y lo pasan al dominio.
- **El contrato no promete orden** (`partidas-config.md:112-118`).

## Arquitectura

Un solo servicio tocado (**Partidas**) más la tabla del web. Cero cambios en Operaciones de Sesión,
Puntuaciones, Identity, gateway y móvil (el móvil no alcanza `/partidas`: el gateway se lo cierra).

### Dominio

`Partida` gana `public DateTime FechaCreacion { get; private set; }`, y `Partida.Crear(...)` recibe
`DateTime fechaCreacion` como **último parámetro**, por paridad con Operaciones.

El dominio **no lee el reloj**: recibe el instante. Es lo que hace que sus tests sean deterministas
sin ninguna maquinaria.

### Reloj

`CrearPartidaCommandHandler` inyecta `TimeProvider` y pasa `_timeProvider.GetUtcNow().UtcDateTime`.
Requiere registrar `services.AddSingleton(TimeProvider.System)` en el `DependencyInjection` de
`Umbral.Partidas.Application` — hoy no existe; es la línea que Operaciones ya tiene.

### Orden

`PartidaRepository.ListAsync` pasa a:

```csharp
.OrderByDescending(p => p.FechaCreacion)
.ThenBy(p => p.PartidaId)
```

El `ORDER BY` vive en el **repositorio**, no en el handler: es trabajo de la base y sobrevive a una
futura paginación.

El desempate por `PartidaId` no es paranoia: con precisión de microsegundos dos partidas reales nunca
empatan, pero un **reloj falso en tests** devuelve el mismo instante, y sin `ThenBy` ese test tendría
orden indefinido e intermitente.

### Migración

Segunda migración de Partidas: columna `FechaCreacion` `NOT NULL`.

**Decisión sobre el default de las filas existentes** (la columna es `NOT NULL`, así que hace falta):

| Default | Efecto |
|---|---|
| `now()` | Las partidas viejas quedan **fechadas hoy y al tope de la lista** — silenciosamente equivocadas, justo el lugar reservado a lo último creado. **Peor que el desorden actual**: hoy el orden es aleatorio y se nota; así sería confiadamente incorrecto y no se notaría. |
| Centinela `'0001-01-01 00:00:00+00'` | Las viejas quedan **al fondo** (donde va lo desconocido) y muestran una fecha obviamente falsa, que nadie se cree. |

Se usa el **centinela**, vía `defaultValueSql` en crudo — que además evita que Npgsql rechace un
`DateTime` con `Kind=Unspecified` contra `timestamptz`. Como la data es descartable (decisión 2) esto
es transitorio, pero la migración debe comportarse bien **aunque no se borre la base**, en vez de
depender de que se borre.

### Contrato

`PartidaSummaryDto` gana `FechaCreacion`. `contracts/http/partidas-config.md` documenta el campo y
—por primera vez— **el orden**, que hasta hoy nadie había prometido.

**`GET /partidas/{id}` no cambia.** El detalle de una partida no gana `fechaCreacion`: nadie lo pidió
y ninguna pantalla lo usaría. El campo existe en el dominio y se expone solo donde sirve —el listado,
para explicar su orden—. Añadirlo al detalle "por simetría" sería alcance inventado.

## Web (frontend)

| Archivo | Cambio |
|---|---|
| `frontend/src/api/partidasApi.ts` | `PartidaSummary` gana `fechaCreacion: string` |
| `frontend/src/features/partidas/PartidasListPage.tsx` | Columna **"Creada"** con `new Date(p.fechaCreacion).toLocaleString()` |

**Posición: después de "Nombre"**, no al final. La tabla se lee de izquierda a derecha; si la lista
está ordenada por un criterio que solo aparece en la última columna, el operador tiene que barrer
toda la fila para entender el orden. Pegada al nombre, la explicación cae donde va la vista.

**`toLocaleString()`, no `toLocaleDateString()`**: pinta fecha **y hora** (decisión 5). Es además lo
que ya hacen `HistorialPartidaPage` y `RendimientoEquipoPage`.

**Sin ordenar en cliente**: el backend ya entrega ordenado.

Añadir una columna no toca `data-testid`, `label` ni roles ARIA (`PartidasListPage.test.tsx` asevera
sobre `fila-partida-${partidaId}`), así que respeta la regla de la reconstrucción visual.

## Manejo de errores

No hay modos de fallo nuevos. `FechaCreacion` es obligatorio y siempre lo escribe el handler; no hay
camino en que falte. El listado ya maneja su error de carga y no cambia.

El único valor "raro" posible es el centinela de las filas migradas, que es intencional y se pinta
como fecha visiblemente falsa.

## Testing

| Nivel | Qué cubre |
|---|---|
| Dominio | `Partida.Crear` guarda la `fechaCreacion` que recibe (pasando `T0` literal) |
| Handler unit | `CrearPartidaCommandHandler` toma la fecha del `TimeProvider` inyectado |
| Repositorio (integration) | tres partidas con `T0`, `T0+1h`, `T0+2h` → salen en orden inverso exacto |
| Contract | `fechaCreacion` presente en la forma de `GET /partidas` |
| Web (vitest) | la columna "Creada" renderiza fecha y hora; la tabla respeta el orden recibido (no reordena) |

El de **repositorio** es el que prueba el bug reportado; los demás lo sostienen.

**Límites declarados** (por adelantado, para no repetir la sobreventa del slice del 2026-07-15):

- Si los integration tests de Partidas corren sobre **InMemory** —como todo el suite de Operaciones;
  **verificar al implementar**—, el `ORDER BY` queda probado como ordenamiento LINQ, **no contra SQL
  real**.
- El **`defaultValueSql` del centinela no lo prueba ningún test**: las migraciones no se ejercitan en
  la suite. Se verifica corriendo la migración contra la base local, a mano.

## Documentación a actualizar

- `contracts/http/partidas-config.md` — campo `fechaCreacion` + orden garantizado en `GET /partidas`.
- `docs/04-sdd/SPECS-LIST.md` — fila del slice.
- `docs/04-sdd/traceability-matrix.md` — fila del slice, incluyendo los dos hallazgos fuera de
  alcance (panel del participante sin orden; columna "Estado" muerta por ADR-0010).
- `Partida.cs:13` — **borrar el comentario stale** (*"SP-3 sets Lobby"*) al tocar la entidad: describe
  un diseño que ADR-0010 abandonó y hace creer que la columna Estado funciona.
