# Nombres de partida y de juego en las pantallas (design)

Fecha: 2026-07-15
Origen: slice diferido explícitamente en `docs/superpowers/specs/2026-07-14-nombres-competidores-design.md`
("Fuera de alcance: nombres de **partida** y de **juego** … pertenecen al servicio Partidas … son un
slice propio").

## Problema

Tres superficies siguen mostrando GUIDs recortados donde el usuario espera identidad legible:

- `RendimientoEquipoPage.tsx:118` (web, Operador/Admin) — `partidaId`.
- `HistorialPartidaPage.tsx:137` (web, Operador/Admin) — `juegoId`.
- `ConvocatoriasScreen.tsx:79` (móvil, Participante) — `partidaId`.

Además, hallazgo del brainstorming: la **cabecera** de `HistorialPartidaPage` dice solo "Historial de
la partida", sin ninguna identidad — ni nombre ni id. Es peor que un GUID: el operador no sabe qué
partida está viendo. Entra en el alcance.

## Alcance

**Refinamiento transversal de usabilidad**, hermano del slice de nombres de competidores. No añade
capacidad de negocio, no cambia reglas de dominio ni cálculos de ranking. No introduce HU nueva.

**Fuera de alcance:**

- `HistorialPartidasScreen.tsx` y `RendimientoEquipoScreen.tsx` (móvil): usan `partidaId` solo como
  `key` de React y **no muestran ningún GUID**. Este slice sustituye GUIDs visibles; añadirles
  identidad de partida es una mejora distinta.
- Cabeceras de `SesionOperadorPage` ("Lobby de la partida", "Sesión en curso"), que también carecen
  del nombre pese a tener `config` cargada. Mismo motivo: no muestran GUID. Anotado como observación,
  no como trabajo de este slice.
- Cualquier cambio a eventos RabbitMQ, proyecciones de Puntuaciones o al gateway.

## Decisiones tomadas en brainstorming

1. **La columna "Juego" muestra una etiqueta derivada de orden y tipo** — `"Juego 1 · Trivia"`,
   `"Juego 2 · Búsqueda del Tesoro"`. No se añade `NombreJuego` al dominio: sería capacidad de
   negocio nueva (entidad, migración, contrato de creación, UI y HU propia) y dejaría de ser un
   refinamiento.
2. **El nombre de la partida va en la cabecera del historial, no en cada fila.** La página está
   acotada a una partida por la ruta (`/partidas/:partidaId/historial`), así que repetir el nombre
   por fila no distinguiría un juego de otro.
3. **Enfoque A — cada superficie usa la fuente que su actor puede alcanzar.** Descartados:
   denormalizar el nombre en `PartidaPublicadaEnLobby` → `PartidaProyectada` (dejaría sin nombre a
   las partidas ya publicadas salvo backfill, y no hacía falta) y un endpoint de directorio en
   Partidas espejo del de Identity (exigiría una ruta de gateway que anulara la política
   `OperadorOAdministrador` del catch-all de `/partidas/**` para el móvil, y **no resolvería la
   columna Juego**, porque no hay nombre de juego que resolver).

## Hechos verificados en código

Anclan el diseño; no repetir la verificación al implementar.

- **`Juego` no tiene nombre.** `JuegoTrivia` y `JuegoBDT` (`Umbral.Partidas.Domain/Entities/`) solo
  exponen `JuegoId`, `PartidaId`, `Orden`, `Estado` (y `AreaBusqueda` en BDT).
- **El nombre de la partida es inmutable.** `contracts/http/partidas-config.md` registra solo
  `POST /partidas`, `POST /partidas/{id}/juegos/trivia`, `POST /partidas/{id}/juegos/bdt`,
  `GET /partidas/{id}` y `GET /partidas`. No hay renombrado, así que la distinción "nombre actual vs
  histórico" del slice anterior aquí no aplica.
- **`JuegoProyectado` (Puntuaciones) ya guarda `Orden` y `TipoJuego`** por `JuegoId`. La etiqueta se
  compone sin datos nuevos.
- **`SesionPartida.Nombre` (Operaciones) ya existe** — Operaciones snapshotea el nombre al publicar.
  `PartidaPublicadaDto` ya lo expone como `nombre` para el panel móvil.
- **Corrección (2026-07-15, hallada al escribir el plan):** el handler de convocatorias **no** tiene
  el nombre a mano. `ISesionPartidaRepository.GetConvocatoriasPendientesByUsuarioAsync` devuelve
  `IReadOnlyList<Convocatoria>`: hace `SelectMany` de `Sesiones` → `Inscripciones` → `Convocatorias`,
  así que la `SesionPartida` (y con ella el `Nombre`) se pierde por el camino. Hay que **cambiar la
  firma del repositorio** para que proyecte también el nombre. Solo 4 archivos fuente referencian la
  interfaz (`FakeSesionPartidaRepository.cs`, `SesionHubTests.cs`,
  `BarrerIniciosAutomaticosCommandHandlerTests.cs`, `BarrerTimeoutsCommandHandlerTests.cs`) y un
  único caller de producción, así que el costo está acotado.
- **`PartidaProyectada` (Puntuaciones) NO tiene nombre**, y `PartidaPublicadaEnLobby` no lo lleva en
  su payload (`{ partidaId, sesionPartidaId, modalidad, minimosParticipacion, maximosParticipacion }`).
- **`GET /partidas` devuelve un resumen ligero**: `{ partidaId, nombrePartida, modalidad,
  modoInicioPartida, tiempoInicio, minimosParticipacion, maximosParticipacion, estado,
  cantidadJuegos }` — sin preguntas ni códigos QR. `GET /partidas/{id}` en cambio arrastra respuestas
  correctas y QR esperados: **no usarlo solo para leer un nombre**.
- **El cliente web ya tiene `getPartidas()` y el tipo `PartidaSummary`** (`frontend/src/api/partidasApi.ts`).
- **Gateway:** `/partidas/{**catch-all}` es `OperadorOAdministrador` — el Participante no llega a
  Partidas. Por eso el móvil se sirve de Operaciones y no de Partidas.

## Arquitectura

### Servicios tocados

Dos cambios, ambos aditivos y cada uno en el servicio que ya tiene el dato. **Cero cambios en
Partidas y cero en el gateway.**

| Servicio | Cambio |
|---|---|
| **Puntuaciones** | `EntradaHistorialDto` gana `JuegoOrden` (int?) y `TipoJuego` (enum?), unidos desde `JuegoProyectado` por `JuegoId` |
| **Operaciones de Sesión** | `ConvocatoriaPendienteDto` gana `NombrePartida`. Requiere que `GetConvocatoriasPendientesByUsuarioAsync` proyecte el nombre: cambia de `IReadOnlyList<Convocatoria>` a `IReadOnlyList<ConvocatoriaPendienteProyeccion>`, un record de lectura junto a la interfaz — mismo patrón que `ParticipacionEquipoHistorial` en Puntuaciones |

`JuegoOrden`/`TipoJuego` son nullable porque hay eventos de partida (`PartidaIniciada`,
`PartidaFinalizada`) que no tienen juego.

**El backend no fabrica strings de presentación**: devuelve orden y tipo; el cliente compone la
etiqueta.

### Clientes

| Cliente | Archivo | Responsabilidad |
|---|---|---|
| web | `frontend/src/features/partidas/juegoLabels.ts` | `etiquetaJuego(orden, tipoJuego, juegoId)` — módulo puro, testeable sin render |
| web | `frontend/src/features/shared/useNombresPartida.ts` | hook: `getPartidas()` al montar + fallback |

Firmas exactas:

```ts
// juegoLabels.ts — puro, sin React
export function etiquetaJuego(
  orden: number | null | undefined,
  tipoJuego: string | null | undefined,
  juegoId: string | null | undefined
): string;
// (1, "Trivia", guid)            → "Juego 1 · Trivia"
// (2, "BusquedaDelTesoro", guid) → "Juego 2 · Búsqueda del Tesoro"
// (null, null, null)             → "—"                (evento de partida)
// (null, null, guid)             → "abcdef12"         (juego sin proyección)

// useNombresPartida.ts
export function useNombresPartida(accessToken: string): (partidaId: string) => string;
// nombrePartidaDe(id) → "Copa UCAB" | "abcdef12" si no resuelve
```

**`useNombresPartida` NO lleva caché incremental ni troceo, a diferencia de `useNombres`.** Aquella
maquinaria existía porque llegaban competidores nuevos por push de SignalR y había que pedir solo los
faltantes. Estas dos pantallas son vistas de revisión del operador: no hay push, `GET /partidas` trae
todas las partidas en un request, y un fetch por montaje basta. Replicarla aquí sería copiar
complejidad sin el problema que la justificaba.

`etiquetaJuego` también traduce el enum: el backend serializa `"BusquedaDelTesoro"` y el operador debe
leer `"Búsqueda del Tesoro"`.

### Superficies a sustituir

| Cliente | Archivo | Cambio |
|---|---|---|
| web | `HistorialPartidaPage.tsx` (cabecera, `<h1>`) | `Historial de la partida` → `Historial de la partida — Copa UCAB` |
| web | `HistorialPartidaPage.tsx:137` | `guidCorto(e.juegoId)` → `etiquetaJuego(e.juegoOrden, e.tipoJuego, e.juegoId)` |
| web | `RendimientoEquipoPage.tsx:118` | `p.partidaId.slice(0, 8)` → `nombrePartidaDe(p.partidaId)` |
| móvil | `ConvocatoriasScreen.tsx:79` | `Partida {c.partidaId.slice(0, 8)}` → `{c.nombrePartida}` |

Móvil no necesita hook, cliente HTTP nuevo ni caché: el nombre llega en la misma respuesta que ya se
pide. Solo se añade el campo al tipo `Convocatoria`.

`guidCorto` **no se borra** de `HistorialPartidaPage`: lo sigue usando `etiquetaJuego` como fallback.

## Manejo de errores

**Principio rector: resolver un nombre nunca rompe la pantalla.** Si `GET /partidas` falla, la web se
queda con el GUID corto y la cabecera vuelve a decir solo "Historial de la partida" — exactamente lo
que dice hoy. Degradar al estado actual es aceptable.

**La columna Juego tiene dos "vacíos" que significan cosas distintas y no deben colapsarse:**

| Caso | `juegoId` | `juegoOrden` | Se muestra | Por qué |
|---|---|---|---|---|
| Evento de partida (`PartidaIniciada`, `PartidaFinalizada`) | null | null | `—` | No hay juego: es la verdad |
| Evento de juego con proyección presente | guid | int | `Juego 1 · Trivia` | Caso normal |
| Evento de juego sin `JuegoProyectado` (lag / evento perdido) | guid | null | `guidCorto(juegoId)` | Hay un juego pero no se sabe cuál; pintar `—` mentiría |

En móvil, si `nombrePartida` no viniera (servidor viejo), cae al GUID corto.

## Testing

| Nivel | Qué cubre |
|---|---|
| Puntuaciones unit | el handler del historial une `JuegoProyectado` por `juegoId`; evento de partida → orden/tipo null; `juegoId` sin proyección → orden/tipo null sin lanzar |
| Puntuaciones contract | `juegoOrden` y `tipoJuego` presentes en la forma de la respuesta |
| Operaciones unit | `ObtenerMisConvocatoriasPendientesQueryHandler` devuelve `nombrePartida` desde `SesionPartida.Nombre` |
| Operaciones contract | el DTO de `/mis-convocatorias` incluye `nombrePartida` |
| Web (vitest) | `juegoLabels` puro (Trivia, BDT con tilde, null → `—`, juegoId sin orden → GUID); `useNombresPartida` (resuelve y cae al GUID si falla); cabecera y columna del historial; celda de rendimiento |
| Móvil (`node --test`) | `convocatoriasFlow.js` propaga `nombrePartida` |

**Limitación declarada:** `ConvocatoriasScreen` es `.tsx` y el harness `node --test` no puede
importarlo, así que el render no queda cubierto — solo el flujo que lo alimenta. A diferencia del
slice anterior, aquí no hay lógica que extraer a un `.js`: el cambio es leer un campo del DTO. Crear
un módulo `.js` para `{c.nombrePartida}` sería ceremonia sin valor.

Los tests existentes de `HistorialPartidaPage` y `RendimientoEquipoPage` asertan hoy el GUID corto y
hay que actualizarlos. Es cambio de comportamiento legítimo y no toca `data-testid`, `label` ni roles
ARIA.

## Documentación a actualizar

- `contracts/http/puntuaciones-api.md` — `juegoOrden` + `tipoJuego` en la entrada del historial.
- `contracts/http/operaciones-sesion-api.md` — `nombrePartida` en `ConvocatoriaPendienteDto`.
- `docs/04-sdd/SPECS-LIST.md` — fila del slice como refinamiento transversal.
- `docs/04-sdd/traceability-matrix.md`.
