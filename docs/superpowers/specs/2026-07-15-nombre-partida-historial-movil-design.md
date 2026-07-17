# Nombre de partida en el historial del participante (móvil) (design)

Fecha: 2026-07-15
Origen: mejora declarada explícitamente fuera de alcance en
`docs/superpowers/specs/2026-07-15-nombres-partida-juego-design.md` ("`HistorialPartidasScreen.tsx` y
`RendimientoEquipoScreen.tsx` (móvil) … **añadirles identidad de partida es una mejora distinta**").
Este slice es esa mejora.

## Problema

El participante no puede saber **qué partida** está mirando en su propio historial.

- `HistorialPartidasScreen.tsx:79` (móvil, Participante) — la tarjeta encabeza con
  `{p.modalidad} · {p.puntosTotales} pts` → *"Individual · 120 pts"*.
- `RendimientoEquipoScreen.tsx:72` (móvil, Participante) — la tarjeta encabeza con
  `Posición {p.posicion}`.

Ninguna muestra un GUID: simplemente **no hay identidad de partida**, que es peor. El slice hermano
resolvió el caso simétrico del operador (`RendimientoEquipoPage.tsx:120` ya pinta el nombre vía
`useNombresPartida`), dejando al participante sin el equivalente.

**Hallazgo adicional (verificado al diseñar):** `PartidasPanelScreenContainer.tsx:25-26` navega a la
sesión con el literal hardcodeado `nombre: "Mi partida"` al **retomar** una sesión activa. El
participante que entra desde el panel de publicadas ve el nombre real
(`PartidasPanelScreen.tsx:98-100`, propagado desde `partidas-publicadas`), pero el que reconecta ve
*"Mi partida"* en la cabecera de `PartidaLiveScreen.tsx:205` / `PartidaLobbyScreen.tsx:132`. Es el
mismo déficit de identidad, en pleno juego, y lo arregla el mismo dato. Entra en el alcance.

## Alcance

**Refinamiento transversal de usabilidad**, tercer hermano de `nombres-competidores` (2026-07-14) y
`nombres-partida-juego` (2026-07-15). No añade capacidad de negocio, no cambia reglas de dominio ni
cálculos de ranking. **No introduce HU nueva.**

HU gobernante: **HU-27** (historial de partidas jugadas del participante, implementada en SP-4d).
Toca incidentalmente la superficie de HU-49 (rendimiento de equipo) y la de HU-26/HU-42 (cabecera de
sesión en vivo) sin alterar su comportamiento de negocio.

**Fuera de alcance:**

- **Cabeceras de `SesionOperadorPage`** ("Lobby de la partida", "Sesión en curso"). Observación
  heredada del slice anterior y confirmada aquí: el operador no ve el nombre de la partida mientras
  monitorea. Pero el operador *puede* alcanzar `/partidas` y ya tiene `useNombresPartida` disponible:
  su carencia es de cableado, no de acceso al dato. Distinto problema, distinta superficie.
- **Migrar `useNombresPartida` (web) al resolver nuevo.** El web seguirá resolviendo por
  `GET /partidas`. La asimetría web/móvil la impone el gateway y es deliberada (ver Decisión 3).
- Cambios a eventos RabbitMQ, a las proyecciones de Puntuaciones, a Partidas o al gateway.

## Decisiones tomadas en brainstorming

1. **Endpoint de directorio nuevo en Operaciones de Sesión**, no en Partidas.
   El slice hermano descartó "un endpoint de directorio en Partidas espejo del de Identity" por dos
   razones (líneas 44-49): *(a)* exigiría una ruta de gateway que anulara la política
   `OperadorOAdministrador` del catch-all de `/partidas/**` para el móvil, y *(b)* no resolvería la
   columna Juego. **Ninguna de las dos aplica a este diseño:** `/operaciones-sesion/{**catch-all}` ya
   es `Default` en el gateway (cualquier autenticado), así que no hay ruta nueva ni política que
   anular; y la columna Juego ni está en alcance ni tiene nombre que resolver (`Juego` no tiene nombre
   en el dominio — ya zanjado). El rechazo era al **sitio**, no al patrón.

2. **Descartado denormalizar el nombre en `PartidaPublicadaEnLobby` → `PartidaProyectada`**
   (Puntuaciones). Mismo motivo que registró el slice hermano: dejaría sin nombre a las partidas ya
   proyectadas salvo backfill — y son justo las que el participante ya jugó y va a mirar al abrir la
   pantalla. Además metería una segunda fuente del nombre compitiendo con la que ya existe.

3. **Enfoque A heredado: cada superficie usa la fuente que su actor puede alcanzar.** Web (Operador)
   → `GET /partidas`. Móvil (Participante) → Operaciones de Sesión, que ya snapshotea el nombre. Es
   el principio rector del slice hermano y la razón de que el móvil ya se sirva de Operaciones para
   las convocatorias.

4. **El nombre va como título de la tarjeta**, y la modalidad baja a la línea de contexto. La
   identidad manda sobre el atributo.

## Hechos verificados en código

Anclan el diseño; no repetir la verificación al implementar.

- **`SesionPartida.Nombre` ya existe y está poblado**
  (`Umbral.OperacionesSesion.Domain/Entities/SesionPartida.cs:17`): Operaciones snapshotea el nombre
  al publicar. **Corolario decisivo:** existe una fila `SesionPartida` para toda partida publicada, y
  el historial solo contiene partidas `Terminada`, que necesariamente fueron publicadas. **La
  cobertura es total desde el día uno y no hace falta backfill** — que es exactamente lo que la
  opción descartada en la Decisión 2 no podía ofrecer.
- **`PartidaProyectada` (Puntuaciones) NO tiene nombre** y `PartidaPublicadaEnLobby` no lo lleva en su
  payload — confirmado en `contracts/events/operaciones-sesion-events.md:79-87`. La palabra `Nombre`
  no aparece en ningún archivo de `services/puntuaciones/src`.
- **Gateway:** `/operaciones-sesion/{**catch-all}` → `AuthorizationPolicy: "Default"`
  (`gateway/src/Umbral.Gateway/appsettings.json:53-57`). El endpoint nuevo es alcanzable por el
  Participante **sin tocar el gateway**. `/partidas/{**catch-all}` sigue siendo
  `OperadorOAdministrador` (líneas 48-52).
- **Patrón de referencia:** `DirectoryController` de Identity
  (`services/identity-service/src/Umbral.IdentityService.Api/Controllers/DirectoryController.cs`) —
  `[Route("identity/directory")]`, `[Authorize]` de clase, `[HttpPost("names")]`, valida con
  `IValidator<T>` y devuelve `ValidationProblemDetails` en 400. Vive **fuera** del controller de su
  agregado precisamente porque aquel tiene una policy más estrecha.
- **`SesionesController` es `[Route("operaciones-sesion")]`** (línea 13) y ya carga 26 endpoints. El
  directorio va en **controller propio** (`[Route("operaciones-sesion/directory")]`), por la regla de
  estructura "cada controller define su propia ruta" y por paridad con Identity.
- **Cliente móvil, patrón de referencia:** `mobile/src/features/shared/directoryApi.js` (POST a
  `/identity/directory/names`, estilo `{ ok, data }`, reusa `mapCommonError`/`networkError` de
  `partidasPublicadasApi.js`) y `mobile/src/features/shared/useNombres.js` (caché de módulo, troceo a
  `MAX_LOTE = 200`, `nombreCorto` como fallback, `{ ok: false }` degrada sin romper).
- **`GET /partidas/{id}` arrastra respuestas correctas y QR esperados**: no usarlo para leer un
  nombre (nota heredada del slice hermano; aquí ni aplica, el móvil no llega a Partidas).

## Arquitectura

### Servicio tocado

Un solo cambio de backend, aditivo, en el servicio que ya tiene el dato. **Cero cambios en Partidas,
en Puntuaciones y en el gateway.**

| Servicio | Cambio |
|---|---|
| **Operaciones de Sesión** | `DirectoryController` nuevo con `POST /operaciones-sesion/directory/partidas`. Resuelve lotes de `partidaId` → `nombre` desde `SesionPartida.Nombre`. |

### Contrato

```txt
POST /operaciones-sesion/directory/partidas
Auth: [Authorize] — cualquier rol autenticado (policy Default en el gateway)

Request:  { "partidaIds": ["guid", ...] }        // opcional, default []
200:      { "partidas": [ { "partidaId": "guid", "nombre": "Copa UMBRAL" } ] }
400:      partidaIds.length > 200 → { "message": "..." }
401:      sin token
```

**Corrección (T3, hallada al implementar):** el 400 es `{ "message": "..." }`, **no**
`ValidationProblemDetails`. Operaciones valida con un `ValidationBehavior` en el pipeline de MediatR
—que lanza `ValidationException`— y su `ExceptionHandlingMiddleware` la mapea a 400 serializando
`new { message = ex.Message }`, igual que todos los demás 400 del servicio. El controller **no**
inyecta `IValidator<T>` como hace el de Identity: aquí eso rompería la doctrina "controllers stay
pure dispatchers" (doctrine audit M-2), que el propio `ValidationBehavior.cs:6-8` documenta.

- **Los ids desconocidos se omiten de la respuesta**, no se devuelven con `nombre: null`. El cliente
  ya trata "pedido y no volvió" como no-resoluble y cachea el fallback (`useNombres.js:63-66`).
- **Campo `nombre`, no `nombrePartida`.** Dentro de Operaciones, `PartidaPublicadaDto` ya usa
  `nombre`; bajo una clave `partidas` no hay ambigüedad. (`ConvocatoriaPendienteDto` usa
  `nombrePartida` porque ahí el nombre viaja junto a `equipoId` y sí necesitaba desambiguar.)
- **Tope de 200 por lote**, igual que `/identity/directory/names`, para que el troceo del cliente sea
  el mismo.
- **Privacidad:** el nombre de una partida no es sensible — ya se expone a cualquier autenticado en
  `GET /operaciones-sesion/partidas-publicadas`. Misma postura que el directorio de Identity, que
  resuelve cualquier nombre para cualquier autenticado. No se filtra por participación del caller:
  hacerlo obligaría a una consulta de participación por id y no protegería nada.

### Capas (Operaciones de Sesión)

| Capa | Elemento |
|---|---|
| `Application/Queries/` | `ResolverNombresPartidaQuery(IReadOnlyList<Guid> PartidaIds)` |
| `Application/DTOs/` | `NombrePartidaDto(Guid PartidaId, string Nombre)` · `ResolverNombresPartidaResponse(IReadOnlyList<NombrePartidaDto> Partidas)` |
| `Application/Validators/` | `ResolverNombresPartidaQueryValidator` — `PartidaIds.Count <= 200` |
| `Application/Handlers/Queries/` | `ResolverNombresPartidaQueryHandler` |
| `Domain/` (interfaz) | `ISesionPartidaRepository.GetNombresByPartidaIdsAsync(IReadOnlyList<Guid>, CancellationToken)` → `IReadOnlyList<NombrePartidaProyeccion>` |
| `Application/DTOs/` | `ResolverNombresPartidaRequest { Guid[]? PartidaIds }` |
| `Api/Controllers/` | `DirectoryController` — despachador puro, sin validador inyectado |

**Corrección (T3):** el request record va en `Application/DTOs/`, **no** en `Api/Contracts/` como
decía este diseño por copiar a Identity. Operaciones no tiene carpeta `Api/Contracts/`; sus requests
(`ResponderPreguntaRequest`, `ValidarTesoroRequest`, `EnviarPistaRequest`) viven en
`Application/DTOs/`, que además es lo que exige la regla graded ("`DTOs/` holds request/response
models").

`NombrePartidaProyeccion(Guid PartidaId, string Nombre)` es un record de lectura junto a la interfaz
del repositorio — mismo patrón que `ConvocatoriaPendienteProyeccion` (introducido por el slice
hermano) y `ParticipacionEquipoHistorial` en Puntuaciones. **Proyecta solo los dos campos**: no
devolver `SesionPartida` entera para leer un nombre.

`PartidaIds` vacío → `200` con `partidas: []` sin tocar la base.

### Clientes (móvil)

| Archivo | Responsabilidad |
|---|---|
| `mobile/src/features/shared/partidaDirectoryApi.js` (nuevo) | `resolverNombresPartida(apiBaseUrl, token, payload, fetchImpl)` — espejo exacto de `directoryApi.js` |
| `mobile/src/features/shared/useNombresPartida.js` (nuevo) | `useNombresPartida(partidaIds, apiBaseUrl, token) → (partidaId) => string` |

**El hook móvil SÍ lleva caché de módulo y troceo**, a diferencia del `useNombresPartida.ts` del web.
No es copiar complejidad: es que el problema es distinto. El web baja **todas** las partidas en un
`GET /partidas` por montaje y no necesita saber qué ids pedir. El móvil **no puede** — no llega a
Partidas — así que pide por lote de ids conocidos, y esos ids solo se conocen **después** de que
cargue el historial. La caché evita repedir entre las tres superficies que comparten el hook
(historial, rendimiento, panel). La forma es la de `useNombres.js`, ya probada.

Firmas exactas:

```js
// partidaDirectoryApi.js
export async function resolverNombresPartida(apiBaseUrl, token, payload, fetchImpl = fetch);
// payload: { partidaIds: [guid] } → { ok: true, data: { partidas: [{ partidaId, nombre }] } }
//                                 | { ok: false, type, message }

// useNombresPartida.js
export function useNombresPartida(partidaIds, apiBaseUrl, token);
// nombrePartidaDe(id) → "Copa UMBRAL" | "a3f9c1d2" si no resuelve
export function resetNombresPartidaCache();  // para tests, espejo de resetNombresCache
export function trocearPartidas(partidaIds); // lotes de 200, exportado para test puro
```

### Superficies a cambiar

| Archivo | Cambio |
|---|---|
| `HistorialPartidasScreen.tsx:78-86` | Título → `{nombrePartidaDe(p.partidaId)}`; `{p.modalidad} · {p.puntosTotales} pts` baja a la línea de contexto junto a posición y fecha |
| `RendimientoEquipoScreen.tsx:72` | Título → `{nombrePartidaDe(p.partidaId)}`; `Posición {p.posicion}` baja a la línea de contexto |
| `PartidasPanelScreenContainer.tsx:25-26` | `nombre: "Mi partida"` → nombre resuelto; `"Mi partida"` queda solo como fallback si el resolver falla |

Resultado en `HistorialPartidasScreen`:

```txt
Copa UMBRAL                                    [Ganó]
Individual · 120 pts · Posición 1 · 15/7/2026
1. Trivia — 80 pts
2. Búsqueda del tesoro — 40 pts
```

## Manejo de errores

**Principio rector heredado: resolver un nombre nunca rompe la pantalla.** Si el resolver falla
(red, 4xx, 5xx), `nombrePartidaDe` cae al GUID corto y la tarjeta sigue mostrando puntos, posición y
juegos. El cliente móvil no lanza: `{ ok: false }` se trata igual que un fallo de red, exactamente
como `useNombres.js:58-60`.

| Caso | Se muestra | Por qué |
|---|---|---|
| Resolución OK | `Copa UMBRAL` | Caso normal |
| Resolver caído / id desconocido | `a3f9c1d2` | Hay una partida pero no se sabe cuál; degradar al GUID es el estado de hoy en el web |
| `PartidasPanelScreenContainer` sin resolver | `Mi partida` | Se conserva el literal actual como fallback: es mejor copy que un GUID en una cabecera de juego en vivo |

La degradación del historial es **mejor que el estado actual** (hoy no hay ni GUID), así que ningún
modo de fallo empeora la pantalla.

## Testing

| Nivel | Qué cubre |
|---|---|
| Operaciones unit (handler) | resuelve nombres por lote; ids desconocidos omitidos; lista vacía → `partidas: []` sin tocar repositorio |
| Operaciones unit (validator) | 200 ids OK; 201 → inválido; null → `[]` |
| Operaciones unit (controller) | `DirectoryController` despacha por MediatR y devuelve 200; body sin `partidaIds` → lote vacío (regla graded: todo controller tiene unit tests). **El 400 del tope no se prueba aquí** (T3): lo aplica el `ValidationBehavior`, que no existe con `FakeSender` |
| Operaciones integration | fila `SesionPartida` persistida → nombre resuelto; solo los ids pedidos; id desconocido omitido; **partida en estado terminal resuelta** (el caso real: el historial son partidas `Terminada`, y un filtro de estado lo dejaría sin nombres) |
| Operaciones contract | forma de la respuesta `{ partidas: [{ partidaId, nombre }] }`; 401 sin token; 200 con rol `Participante` (el punto del slice); **400 del tope con el pipeline real** (movido desde el unit de controller) |
| Móvil (`node --test`) | `partidaDirectoryApi` (ok, error mapeado, fallo de red); `trocearPartidas` puro; `useNombresPartida` (resuelve, cachea, degrada al GUID si falla) |

**Limitación declarada (móvil):** las tres pantallas son `.tsx` y el harness `node --test` no puede
importarlas, así que el render no queda cubierto — solo el hook y el cliente que las alimentan. Es la
misma limitación que declaró el slice hermano y por el mismo motivo estructural.

**Limitación declarada (persistencia) — corrección de T4.** Este diseño afirmaba que el integration
test sería "la primera ejecución real de la query EF" y que probaría su traducción a SQL. **Es
falso.** Todo el suite de persistencia de Operaciones corre sobre el proveedor **InMemory**
(`UseInMemoryDatabase`), que no valida traducción; el único `UseNpgsql` del suite
(`ConcurrencyTokenTests.cs:13-15`) solo construye el modelo sin conectar. Por tanto la traducción
Npgsql de `partidaIds.Contains(...)` **no queda verificada por ningún test**, aquí ni en el resto del
servicio. Es un hueco **sistémico y preexistente** —lo comparten todos los métodos de
`SesionPartidaRepository`—, no algo que introduzca este slice, y cerrarlo exigiría un harness de
Postgres real (opt-in por env var, como ya hace `RabbitMqRoundTripTests`) que es trabajo de su propio
slice. El riesgo concreto aquí es bajo: `Contains` sobre una colección de parámetros es la traducción
más estándar de EF Core. Queda anotado, no resuelto.

**Regresión esperada = cero.** No se toca ningún test existente: las tres superficies no asertan hoy
sobre identidad de partida porque no la muestran. Contrastar con el slice hermano, que sí tuvo que
actualizar los tests que asertaban el GUID corto.

## Impacto en el operador

**Ninguno.** Verificado, no asumido:

- El endpoint es aditivo bajo un prefijo que ya existe con la misma policy.
- No cambia ningún DTO, contrato, evento ni proyección existente.
- `RendimientoEquipoPage` e `HistorialPartidaPage` (web) siguen usando `useNombresPartida.ts` →
  `GET /partidas`, intocados.
- Las cabeceras de `SesionOperadorPage` siguen genéricas — fuera de alcance, y sus tests asertan
  sobre `data-testid`, no sobre el texto del `<h1>` (`SesionOperadorPage.test.tsx:207,257,350…`), así
  que un slice futuro podrá cambiarlas sin romper nada.

## Documentación a actualizar

- `contracts/http/operaciones-sesion-api.md` — endpoint nuevo + DTO en la tabla de capacidades y en
  la matriz de autorización (fila "Lectura compartida", que pasa de 5 a 6 entradas).
- `docs/04-sdd/SPECS-LIST.md` — fila del slice como refinamiento transversal.
- `docs/04-sdd/traceability-matrix.md`.
