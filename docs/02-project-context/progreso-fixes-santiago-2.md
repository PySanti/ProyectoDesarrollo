# Progreso — rama `fixes-santiago-2` (lote de fixes de Santiago)

**Fecha:** 2026-07-17
**Rama:** `fixes-santiago-2` (parte de `master`; base del lote en `d533c45`).
**Ámbito de Santiago:** backend (servicios + gateway) + móvil + auth/infra. **No** toca `frontend/`
(ese es el lote de Mariangel, rama aparte).

> ⚠️ **Coordinación de agentes paralelos.** Esta rama la están trabajando **dos agentes a la vez**,
> ambos commiteando como `Santiago`. Este documento es el **ledger compartido**: registra lo hecho
> por **ambos** agentes para que ninguno **repita ni pise** el trabajo del otro —
> "Hecho y commiteado por Santiago" (1er agente) y "Hecho y commiteado por el 2º agente (detalle)"
> más abajo. La colisión que lo motivó: a un agente se le asignó **S9** y al otro **S10** (ambos de
> móvil, tocan `HomeScreen`/navegación) y empezaron a resolver lo mismo — y de hecho **ambos
> brainstormearon S9**. Antes de tomar una tarea, mirar las secciones "Ficheros ya tocados".

---

## Hecho y commiteado por Santiago

| Fix | Commit | Qué resuelve | Ficheros |
|---|---|---|---|
| **S2** | `0d35d79` | Trivia: el que acierta ya no queda clavado en "Esperando el cierre"; avanza como el resto (cubre también el hermano `conflict`). | `mobile/src/features/partidas/TriviaPlayPanel.tsx`, `mobile/src/features/partidas/partidaLiveFlow.js`, `mobile/tests/partidaLiveFlow.test.js` |
| **Botón menú** | `28ac5e6` | Al terminar la partida, el botón "Volver a partidas" pasa a "Volver al menú" y navega al Home. | `mobile/src/features/partidas/PartidaLiveScreen.tsx`, `mobile/src/features/partidas/PartidaLiveScreenContainer.tsx` |
| **S12** | `8ad83b5` | Operador/monitor: la pregunta ya no queda clavada en su panel. El admin/`GestionarPartidas` entra al grupo del hub y recibe el avance (antes rebotaba con "No inscrito"). | `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SesionHub.cs`, `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/Realtime/SesionHubTests.cs` |
| **Desempate (indicador)** | `ca18baa` | El ranking consolidado indica por qué ganó quien ganó por desempate. | `mobile/src/features/partidas/PartidaLiveScreen.tsx`, `mobile/src/features/partidas/liveLabels.js`, `mobile/src/features/partidas/liveShared.tsx`, `mobile/tests/liveLabels.test.js` |
| **Desempate (criterio)** | `fd029a4` | Corrige el criterio: solo marca el desempate por el criterio **oculto** (tiempo total), tras verificar la doctrina de ranking (juegos → puntos → tiempo). | `mobile/src/features/partidas/liveLabels.js`, `mobile/tests/liveLabels.test.js` |
| **S6** | `e80177e` | 403 de gobernanza: se abre la **lectura** de listados para los dropdowns de la web (directorio de usuarios y lista de equipos), sin aflojar las mutaciones. | `contracts/http/identity-api.md`, `gateway/src/Umbral.Gateway/Program.cs`, `gateway/src/Umbral.Gateway/appsettings.json`, `gateway/tests/Umbral.Gateway.IntegrationTests/GatewayEndpointsTests.cs`, `services/identity-service/src/Umbral.IdentityService.Api/Controllers/AdminTeamsController.cs`, `services/identity-service/src/Umbral.IdentityService.Api/Controllers/UsersController.cs`, `services/identity-service/src/Umbral.IdentityService.Api/Program.cs`, `services/identity-service/tests/Umbral.IdentityService.ContractTests/Teams/S6LecturasListadoContractTests.cs` |
| **S7** | `33e2e88` | "No se pudo cargar el rendimiento del equipo" (móvil): el endpoint pasa de exigir `GestionarEquipos` a solo autenticado, para la vista "mi equipo" del participante. De rebote arregla la web M6 para un operador simple. | `contracts/http/puntuaciones-api.md`, `services/puntuaciones/src/Umbral.Puntuaciones.Api/Controllers/EquiposController.cs`, `services/puntuaciones/tests/Umbral.Puntuaciones.ContractTests/AutorizacionContractTests.cs` |
| **S9** | `9b13024` | El saludo del Home mostraba el correo ("Hola, correo"). Se agrega un campo `nombre` (`given_name \|\| name \|\| preferred_username`) y el saludo lo usa. `username` (correo) queda como id de cuenta para `RoleRestrictedScreen`. | `mobile/src/auth/tokenClaims.js`, `mobile/src/auth/authTypes.ts`, `mobile/src/screens/HomeScreen.tsx`, `mobile/tests/tokenClaims.test.js` |
| **S10** | `6f57dcc` | El botón "Rendimiento de mi equipo" estaba duplicado en el Home (atajo del batch de Mariangel); ya vivía dentro de "Gestión de equipo" (TeamPanel). Se quita el duplicado del Home para que quede solo dentro del panel de equipo. La ruta `RendimientoEquipo` sigue accesible desde TeamPanel. | `mobile/src/screens/HomeScreen.tsx` |

Todos con TDD (test rojo primero) y gate verde antes de commitear (ver "Verificación").

---

## En curso (Santiago) — NO commiteado

_(nada en curso)_

> **Colisión S9/S10 — resuelta.** El humano asignó **ambas al 1er agente**, en serie: primero
> **S9** (campo `nombre` aparte, no el 1-liner que repurposa `username`, para dejar intacto
> `RoleRestrictedScreen`) y luego **S10**. Ambas commiteadas (ver tabla). Al implementar S10 se vio
> que el botón ya estaba dentro de TeamPanel; el arreglo fue quitar el duplicado del Home.

---

## Pendientes del lote de Santiago (según el reparto)

- **S1** — Partida por equipos se cancela al iniciar. ✅ **Hecho por el 2º agente** (`a3467a7`, detalle abajo).
- **S3** — BDT etapa 2+: QR válido rechazado + intentos invisibles en panel operador. 🔍 **Investigado
  por el 2º agente: NO es bug de backend** (reproducido en vivo por API — el backend valida y lista
  las etapas correctamente; en la DB el participante escaneó el QR de la etapa 1 en la etapa 2). Es
  frontend/proceso (QR reimpreso/escaneado equivocado). Sin cambio de código; va a `PENDIENTES-WEB.md`.
  **No** lo resuelve `5cdf52d` (ese es el banner/fin-de-juego, cosa distinta).
- **S4** — Estado de partida no cambia tras publicar/cancelar/terminar. ✅ **Hecho** (`6e914d4`).
- **S5** — SignalR "disconnected" tras compartir ubicación. ✅ **Hecho** (`8ed1ae5`).
- **S8** — Sesión única por usuario (Keycloak/Identity). ✅ **Hecho por el 2º agente** (detalle abajo):
  autenticador nativo `user-session-limits` en el flujo `browser-umbral` del realm; 2º login del mismo
  usuario → 403.
- **S9** — "Hola, correo" → nombre. ✅ **Hecho por el 1er agente** (`9b13024`): campo `nombre`
  aparte en `buildAuthUser`; el saludo del Home lo usa; `username` (correo) intacto para
  `RoleRestrictedScreen`.
- **S10** — Botón "Rendimiento de mi equipo" dentro de "Gestión de equipo" (móvil). ✅ **Hecho por
  el 1er agente** (`6f57dcc`): el botón ya vivía en TeamPanel; se quitó el duplicado del Home.

### Extras del 2º agente (fuera del reparto S1-S10, reportados por el humano) — ✅ commiteados
- **Orden de preguntas/opciones** (`211dd9e`) — se barajaban al pasar al pre-lobby.
- **Miembro de equipo no transiciona al iniciar** (`4bf33ba`) — el integrante que acepta la convocatoria.
- **Banner "participación activa" con partida terminada** (`5cdf52d`) — el juego no se finalizaba al
  completar el último paso por acierto/ganar.

### Fase 2 (solo tras mergear ambas ramas)
- Websockets para todo lo posible (importantísimo).
- QA de rendimiento del módulo de partidas por equipos.

---

## Ficheros ya tocados por Santiago (referencia anti-colisión)

Antes de editar cualquiera de estos, coordinar — ya tienen cambios commiteados de Santiago:

**Móvil**
- `mobile/src/features/partidas/TriviaPlayPanel.tsx`
- `mobile/src/features/partidas/partidaLiveFlow.js`
- `mobile/src/features/partidas/PartidaLiveScreen.tsx`
- `mobile/src/features/partidas/PartidaLiveScreenContainer.tsx`
- `mobile/src/features/partidas/liveLabels.js`
- `mobile/src/features/partidas/liveShared.tsx`
- `mobile/tests/partidaLiveFlow.test.js`
- `mobile/tests/liveLabels.test.js`
- `mobile/src/auth/tokenClaims.js`, `mobile/src/auth/authTypes.ts` (S9)
- `mobile/src/screens/HomeScreen.tsx` (S9 + botón menú), `mobile/tests/tokenClaims.test.js` (S9)

**Backend**
- `services/operaciones-sesion/.../Realtime/SesionHub.cs` (+ su test)
- `gateway/src/Umbral.Gateway/Program.cs`, `gateway/src/Umbral.Gateway/appsettings.json` (+ su test)
- `services/identity-service/.../Controllers/UsersController.cs`, `.../AdminTeamsController.cs`, `.../Api/Program.cs` (+ contract test S6)
- `services/puntuaciones/.../Controllers/EquiposController.cs` (+ contract test)

**Contratos**
- `contracts/http/identity-api.md`
- `contracts/http/puntuaciones-api.md`

---

## Hecho y commiteado por el 2º agente (detalle)

Los **dos** agentes commitean como `Santiago` (misma identidad git); esta tabla detalla el trabajo
del **segundo agente** (lo que el doc llamaba antes "del agente concurrente"), con el mismo formato
de arriba. Todo con TDD (rojo primero) + gate verde + verificación en vivo (docker) + frontera
respetada (nada en `frontend/`), y cada commit stageado por ruta explícita para no arrastrar
trabajo ajeno del working tree.

| Fix | Commit | Qué resuelve | Ficheros |
|---|---|---|---|
| **S1** | `a3467a7` | Partida por equipos se cancelaba sola al **iniciar en manual**: con el quórum aún **Pendiente** (HU-19: la preinscripción nace sin aprobar), el inicio veía 0 confirmadas < mínimos y cancelaba irreversible. Ahora el **inicio manual rechaza (409 `MinimosNoAlcanzadosException`) y sigue en `Lobby`**; la autocancelación por mínimos queda **solo para el inicio por tiempo**. `LobbyDto` expone `participacionesConfirmadas`. | `services/operaciones-sesion/.../Domain/Entities/SesionPartida.cs`, `.../Domain/Exceptions/MinimosNoAlcanzadosException.cs` (nuevo), `.../Application/DTOs/LobbyDto.cs`, `.../Api/Middleware/ExceptionHandlingMiddleware.cs`, `.../Handlers/Commands/PublicarPartidaCommandHandler.cs`, `contracts/http/operaciones-sesion-api.md`, `PENDIENTES-WEB.md`, + 11 tests actualizados |
| **S4** | `6e914d4` | El estado de la partida **no cambiaba** en el listado del operador (siempre "Sin publicar"): `Partida.Estado` era `null` por diseño (ADR-0010) y **Partidas no tenía consumidor RabbitMQ**. Se añade el consumidor que **proyecta el estado** desde `PartidaPublicadaEnLobby/Iniciada/Cancelada/Finalizada` (idempotente; un estado terminal no se pisa por eventos rezagados). | `services/partidas/.../Api/Workers/{OperacionesSesionEventsConsumer,EstadoPartidaEventMapper,EnvelopeReader,RabbitMqConsumerOptions}.cs` (nuevos), `.../Application/Commands/ProyectarEstadoPartidaCommand(+Handler).cs`, `.../Domain/Entities/Partida.cs`, `.../Api/Program.cs` + `.csproj` (RabbitMQ.Client), `infra/docker-compose.yml`, `contracts/http/partidas-config.md`, `docs/03-microservices/events-catalog.md`, + 3 tests |
| **Orden preguntas** | `211dd9e` | Al crear una partida, el orden de **preguntas y opciones se barajaba** al pasar al pre-lobby: no tenían columna de orden ni `OrderBy`, así que Postgres las devolvía por su PK GUID aleatorio. Se añade columna `orden` (por índice de inserción) + `OrderBy(orden)` en el GET + migración `AddOrdenAPreguntaYOpcion` (filas previas quedan en `orden 0`). | `services/partidas/.../Domain/Entities/{Pregunta,Opcion,JuegoTrivia}.cs`, `.../Handlers/Queries/GetPartidaByIdQueryHandler.cs`, `.../Persistence/PartidasDbContext.cs` + migración, `contracts/http/partidas-config.md`, + 2 tests |
| **S5** | `8ed1ae5` | SignalR se desconectaba **y no volvía** tras compartir ubicación (BDT): `withAutomaticReconnect()` por defecto reintenta 4 veces (0,2,10,30s) y **se rinde**; caminando por zonas muertas el socket moría para siempre. Nueva política `reconexionIndefinida` (backoff corto y luego cada 30s **sin rendirse**) en ambos hubs + aviso "Reconectando…". Verificado en vivo con un cliente `@microsoft/signalr` real (29/29 envíos, backend sano). | `mobile/src/features/partidas/reconexion.js` (nuevo), `.../sesionHub.js`, `.../rankingHub.js`, `.../PartidaLiveScreen.tsx`, `mobile/tests/reconexion.test.js` |
| **Miembro → lobby** | `4bf33ba` | En partidas Equipo, al iniciar **solo transicionaba el líder**, no los integrantes que aceptaron la convocatoria: el miembro acepta en `ConvocatoriasScreen` y **no se navegaba al lobby**, así que nunca hacía `SuscribirAPartida` ni entraba al grupo SignalR (`PartidaIniciada` se difunde a ese grupo). Ahora **aceptar navega al `PartidaLobby`**, como el líder. Backend correcto (resuelve al miembro por su convocatoria; no se toca). | `mobile/src/features/partidas/ConvocatoriasScreen.tsx`, `.../ConvocatoriasScreenContainer.tsx`, `.../convocatoriasFlow.js`, `mobile/tests/convocatoriasFlow.test.js` |
| **Banner participación / fin de juego** | `5cdf52d` | El banner "Tienes una participación activa" persistía con la **partida ya terminada**, y en multi-juego el siguiente juego no arrancaba: acertar la **última pregunta** o ganar la **última etapa** cerraba el paso pero **no finalizaba el juego** (solo lo hacían el timeout y el finalizar-manual). Ahora el cierre **por éxito finaliza el juego** (avanza al siguiente o emite `PartidaFinalizada`), igual que el timeout → `GET /mi-sesion` deja de devolverla y el banner desaparece. | `services/operaciones-sesion/.../Domain/Entities/SesionPartida.cs`, `.../Domain/Results/{ResultadoRespuesta,ResultadoRegistroTesoro}.cs`, `.../Handlers/Commands/{ResponderPregunta,ValidarTesoro,IniciarPartida}CommandHandler.cs`, `contracts/http/operaciones-sesion-api.md`, + 6 tests |
| **S8** | `0b1289c` | Un mismo usuario podía tener **dos sesiones abiertas a la vez**. Se aplica en Keycloak (no en código de app): autenticador nativo `user-session-limits` (`Deny new session`, `userRealmLimit=1`) dentro de un flujo `browser-umbral` que copia el `browser` built-in y queda fijado como `browserFlow`. Va **después** de usuario/contraseña y **dentro** del subflow `forms`, así el re-ingreso por cookie SSO en el mismo navegador no cuenta como sesión nueva; un 2º dispositivo con login fresco recibe **403**. Sin cambio de contratos (config pura de Keycloak). | `infra/keycloak/import/umbral-realm.json`, `infra/keycloak/README.md` |

**Verificación por fix (2º agente):**
- **S1:** operaciones-sesion unit/integration/contract verde; repro en vivo (sin aprobar → 409 sigue Lobby; aprobado → Iniciada).
- **S4:** 121 unit + 7 integration + 16 contract; en vivo crear→None, publicar→Lobby, cancelar→Cancelada (+DB `estado`).
- **Orden preguntas:** 123 unit + 7 + 16; en vivo 6 preguntas/opciones vuelven en orden de creación (DB `orden` 0..5).
- **S5:** 196 mobile + typecheck; repro `@microsoft/signalr` 29/29 sin caer.
- **Miembro → lobby:** 198 mobile + typecheck.
- **Banner / fin de juego:** 439 unit + 38 integration + 86 contract; en vivo `mi-sesion` pasa a **204** tras acertar la última pregunta.
- **S8:** verificado en Keycloak 25 efímero (sin tocar el stack compartido): (1) import en frío `--import-realm` y (2) convergencia de `keycloak-config-cli` sobre un realm viejo — en ambos, 1er login OK, **2º login 403**, y tras logout vuelve a entrar (sin bloqueo permanente).

**Ficheros ya tocados por el 2º agente (anti-colisión).** El otro agente **también toca móvil y
operaciones-sesion**; coordinar por fichero antes de editar:
- Móvil: `mobile/src/features/partidas/{reconexion.js, sesionHub.js, rankingHub.js, PartidaLiveScreen.tsx, ConvocatoriasScreen.tsx, ConvocatoriasScreenContainer.tsx, convocatoriasFlow.js}` (+ sus tests).
- operaciones-sesion: `Domain/Entities/SesionPartida.cs`, `Domain/Results/{ResultadoRespuesta,ResultadoRegistroTesoro}.cs`, `Application/Handlers/Commands/{ResponderPregunta,ValidarTesoro,IniciarPartida,PublicarPartida}CommandHandler.cs`, `Application/DTOs/LobbyDto.cs`, `Api/Middleware/ExceptionHandlingMiddleware.cs` (+ tests).
- partidas: `Domain/Entities/{Partida,Pregunta,Opcion,JuegoTrivia}.cs`, `Api/Workers/*`, `Application/{Commands,Handlers,Queries}/*` de estado y orden, `Persistence/PartidasDbContext.cs` + migraciones, `Api/Program.cs`.
- Contratos: `contracts/http/operaciones-sesion-api.md`, `contracts/http/partidas-config.md`, `docs/03-microservices/events-catalog.md`.

> `25f56c3` ("Modificaciones pequeñas") **no es del 2º agente**; sin identificar. Riesgo de colisión
> real en `mobile/src/features/partidas/` y en `SesionHub`/hubs — coordinar por fichero.

> ⚠️ **`SesionPartida.cs` lo tocan ambos** (el otro agente en `SesionHub`/Realtime y S12; el 2º
> agente en S1 y fin-de-juego). Es el punto caliente de colisión.

---

## Verificación (gates corridos por Santiago)

- **S2 / botón menú / desempate / S9(plan):** `cd mobile && npm test && npm run typecheck` verde.
- **S12:** `dotnet test` de la solución de operaciones-sesion (hub) verde.
- **S6:** gateway 5/5 (S6), Identity ContractTests 59/59, UnitTests 276/276.
- **S7:** Puntuaciones ContractTests 25/25, UnitTests 170/170; móvil 198/198 + typecheck.
  - Nota: 2 IntegrationTests de `HistorialJuegoLabelE2ETests` fallan, pero son **pre-existentes**
    (verificado stasheando el cambio) — ajenos a S7.
- Frontera respetada en todos: `git diff --name-only` sin nada en `frontend/`.

---

## Regla de higiene aplicada

Cada commit de Santiago **stagea solo sus propios ficheros por ruta explícita**, para no arrastrar
el trabajo no commiteado del agente concurrente (que convive en el working tree). Ejemplos de
ficheros ajenos que se dejaron sin tocar al commitear: `GUIA-LEVANTAMIENTO.md`,
`gateway/src/Umbral.Gateway/Program.cs` cuando el cambio en curso no era de S6/S7, etc.
