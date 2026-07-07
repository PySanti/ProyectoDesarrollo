# SP-4c — Puntuaciones: ranking en vivo por SignalR + hardening del servicio

- **Slice:** SP-4c (tercer sub-slice de SP-4, descomposición aprobada en el spec SP-4a). Base: SP-4b completo (`2eade72`), rama `feature/sp-4c-signalr-ranking` creada desde `feature/sp-4b-consolidado` (la serie SP-4 sigue sin integrarse a develop por decisión del responsable).
- **Servicio:** Puntuaciones (`services/puntuaciones`) + `contracts/http/puntuaciones-api.md`. **Sin cambios** en `contracts/events/operaciones-sesion-events.md` (la cola y los 7 bindings de SP-4a quedan intactos) y **sin cambios** en el gateway (la ruta `/puntuaciones/{**catch-all}` con policy `Default` ya cubre WebSocket).
- **Decisiones del responsable (brainstorm 2026-07-06):** alcance = SignalR de ranking en vivo **y** hardening `[Authorize]` (salda toda la deuda etiquetada SP-4c); payload = **ranking completo recalculado** por push (delta y delta+completo descartados); suscripción = **autenticado + partida existente en proyecciones** (validar marcador propio y sin-validación descartados); slice **backend-only** (cableado de clientes → SP-5); integración de la difusión = **enfoque A: el worker orquesta** (handlers-llaman-al-port y notificaciones MediatR descartados).
- **Fuera de alcance:** publisher RabbitMQ propio de Puntuaciones y outbox (criterio de activación en ADR-0012), auditoría/historial y ubicaciones (**SP-4d**), cableado de clientes web/móvil (**SP-5**), difusión de rankings provisionales del consolidado (solo se difunde al finalizar), reintentos de difusión.

## 1. Objetivo

Sobre las proyecciones y queries de SP-4a/SP-4b, difundir en tiempo real por SignalR (a través del gateway) el ranking nativo de un juego cada vez que un evento de scoring se proyecta, y el ranking consolidado cuando la partida termina — materializando `RankingTriviaActualizado`, `RankingBDTActualizado` y `RankingConsolidadoCalculado`, diferidos a Puntuaciones desde SP-3c/SP-3d. Además se salda la deuda de hardening: `[Authorize]` en los endpoints de lectura y en el hub.

## 2. Hub (`Api/Realtime/RankingHub.cs`)

- `RankingHub : Hub` con `[Authorize]`, mapeado en **`puntuaciones/hubs/ranking`** (paridad con `operaciones-sesion/hubs/sesion`: el servicio hospeda la ruta bajo su propio prefijo; el gateway no reescribe paths).
- **`SuscribirAPartida(Guid partidaId)`**: valida que la partida exista en `partidas_proyectadas` vía `IProyeccionesRepository` (paridad con ADR-0011: el repositorio en el hub se reserva para membresía de grupos); si no existe → `HubException("Partida no proyectada.")`; si existe → `Groups.AddToGroupAsync` al grupo `puntuaciones-partida-{partidaId}`.
- **`DesuscribirDePartida(Guid partidaId)`**: `Groups.RemoveFromGroupAsync` del mismo grupo.
- Cualquier rol autenticado puede suscribirse (paridad con la postura de lectura HTTP de SP-4a/4b: el ranking es visible para operador y participantes; sin permiso funcional específico). El hub **no** contiene lógica de negocio ni cálculo.
- Nombres de grupo y de mensaje centralizados en `Api/Realtime/RankingRealtimeMessages.cs` (patrón `SesionRealtimeMessages`).

## 3. Mensajes y payloads (reuso exacto de los DTOs HTTP)

Todos los mensajes van al grupo `puntuaciones-partida-{partidaId}`. Los payloads son los response DTOs existentes — el shape ya documentado en el contrato HTTP es el contrato del push:

| Evento proyectado (RabbitMQ) | Mensaje SignalR | Payload |
|---|---|---|
| `PuntajeTriviaIncrementado` | `RankingTriviaActualizado` | `RankingJuegoResponse` (SP-4a) del juego afectado |
| `EtapaBDTGanada` | `RankingBDTActualizado` | `RankingJuegoResponse` (SP-4a) del juego afectado |
| `PartidaFinalizada` | `RankingConsolidadoCalculado` | `RankingConsolidadoResponse` (SP-4b) de la partida |

Los demás eventos proyectados (partida publicada/iniciada/cancelada, juego activado) **no** difunden nada desde Puntuaciones: sus notificaciones en vivo son responsabilidad de Operaciones de Sesión (SP-3f) y duplicarlas crearía dos fuentes.

## 4. Difusión — el worker orquesta (enfoque A)

- **Port:** `IRankingRealtimePublisher` en `Application/Interfaces/`, con `PublicarRankingTriviaActualizadoAsync(RankingJuegoResponse)`, `PublicarRankingBdtActualizadoAsync(RankingJuegoResponse)` y `PublicarRankingConsolidadoCalculadoAsync(RankingConsolidadoResponse)`.
- **Implementación:** `SignalRRankingRealtimePublisher` en `Api/Realtime/` sobre `IHubContext<RankingHub>` (patrón `SignalRSesionEventsPublisher`).
- **Colaborador:** `RankingBroadcastDispatcher` (en `Api/Workers/`, registrado scoped) que recibe el comando de proyección ya despachado con éxito y, si es uno de los 3 tipos de la tabla, resuelve la query correspondiente (`ObtenerRankingJuegoQuery(partidaId, juegoId)` / `ObtenerRankingConsolidadoQuery(partidaId)`) por `ISender` y llama al port. Para los demás comandos, no hace nada.
- **Enganche en `OperacionesSesionEventsConsumer`:** tras `DespacharAsync` exitoso — tanto en el primer intento como en el reintento por `DbUpdateException` — se invoca el dispatcher **dentro del mismo scope-por-mensaje**, envuelto en try/catch propio: un fallo de difusión (o de la query) se registra con `LogWarning` y **nunca** afecta la proyección ni el ack (ADR-0012 best-effort; el estado HTTP sigue siendo la fuente recuperable). La difusión ocurre como máximo una vez por mensaje: solo tras el despacho que tuvo éxito.
- Caso borde `PartidaFinalizada`: al ejecutarse tras la proyección, la partida ya está `Terminada`, así que `ObtenerRankingConsolidadoQuery` no lanza `PartidaNoTerminadaException`. Scoring tardío (evento de puntaje proyectado después de `PartidaFinalizada`) produce un nuevo push de ranking nativo del juego — coherente con el cálculo-al-leer de SP-4b; no se re-difunde el consolidado (la relectura HTTP lo incorpora).

## 5. Hardening `[Authorize]` (deuda SP-4a/4b saldada)

- `[Authorize]` a nivel de clase en `RankingsController` y `EquiposController`; `HealthController` queda anónimo (probe de infraestructura).
- `RankingHub` con `[Authorize]` (sección 2).
- En `Program.cs`, dentro del bloque de Keycloak configurado: `JwtBearerEvents.OnMessageReceived` toma `access_token` del query string cuando el path empieza por `/puntuaciones/hubs/ranking` (copia del patrón de Operaciones de Sesión — SignalR no puede enviar el header `Authorization` en WebSocket).
- Cuando Keycloak **no** está configurado, `AddAuthentication()` queda sin scheme por defecto y `[Authorize]` fallaría con 500: los tests de integración y contract adoptan el patrón **`TestAuthHandler`** de Operaciones de Sesión (scheme de prueba con claims controladas por header). Los contract tests fijan el **401 sin token** en los 4 endpoints y en la negociación del hub.
- El contrato HTTP reemplaza la nota "sin `[Authorize]` (hardening → SP-4c)" por la postura final: autenticación exigida también en el servicio (defensa en profundidad), lectura para cualquier rol autenticado, sin permiso funcional específico.

## 6. Estructura graduada

Sin carpetas nuevas fuera de doctrina: `Application/Interfaces/` (port), `Api/Realtime/` (hub, mensajes, publisher — espejo de Operaciones de Sesión, que ya fija el precedente de `Realtime/` dentro de `Api/`), `Api/Workers/` (dispatcher). Sin comandos, queries, DTOs, entidades ni migraciones nuevas. Sin cambios de esquema.

## 7. Testing (TDD por tarea)

- **Unit — hub:** suscripción con partida proyectada → une al grupo; partida desconocida → `HubException`; desuscripción remueve del grupo (patrón `SesionHubTests`).
- **Unit — publisher:** cada método envía al grupo `puntuaciones-partida-{id}` con el nombre de mensaje y payload correctos (mock de `IHubContext`).
- **Unit — dispatcher:** comando de scoring Trivia → resuelve `ObtenerRankingJuegoQuery` y publica `RankingTriviaActualizado`; ídem BDT; `ProyectarPartidaFinalizadaCommand` → consolidado; comandos no-ranking → no publica; excepción de la query o del port → no propaga (warn).
- **Unit — worker (rama nueva):** difusión solo tras despacho exitoso; fallo de difusión no impide el ack (se salda parcialmente la deuda "ramas warn+ack del worker sin unit tests" en lo que toca a la rama nueva; las ramas SP-4a preexistentes mantienen su anotación).
- **Unit — controllers:** los existentes siguen; se añade verificación del atributo `[Authorize]` en ambos controllers.
- **Integration:** proyectar `PuntajeTriviaIncrementado` / `EtapaBDTGanada` / `PartidaFinalizada` dispara la difusión con el ranking recalculado (host de test con `TestAuthHandler` + cliente SignalR real conectado al TestServer vía su `HttpMessageHandler`); regresión completa SP-4a/4b en verde.
- **Contract:** 401 sin token en los 4 endpoints HTTP y en `POST /puntuaciones/hubs/ranking/negotiate`; con token de prueba → 200 (los shapes HTTP no cambian).

## 8. Contratos y documentación

- `contracts/http/puntuaciones-api.md`: nueva sección **SignalR** (ruta del hub vía gateway, métodos `SuscribirAPartida`/`DesuscribirDePartida`, los 3 mensajes con payload por referencia a los shapes HTTP ya documentados, `access_token` por query string); sección **Autorización** reescrita (sección 5); Status actualizado (SP-4c registrado; pendiente solo auditoría/historial SP-4d).
- `services/puntuaciones/service-context.md`: estado SP-4c; deuda `[Authorize]`/hardening **retirada**; deuda de unit tests del worker acotada a las ramas SP-4a preexistentes; pending queda **solo** auditoría/historial (SP-4d).
- `docs/04-sdd/traceability-matrix.md`: fila SP-4c (fuentes verificadas en `docs/01-project-source/srs.md`: RF-13 y HU-42 — actualización en tiempo real incluyendo ranking; RF-22 — ranking Trivia en tiempo real; RF-38 — ranking BDT visible para operadores y participantes; RF-37 — eventos para actualización de ranking; HU-26 — operador ve el ranking durante Trivia; RNF-03/RNF-17/RNF-21 — WebSockets, canal de tiempo real y paso por el gateway; eventos `RankingTriviaActualizado`/`RankingBDTActualizado`/`RankingConsolidadoCalculado`).
- **Sin cambios** en `contracts/events/operaciones-sesion-events.md`; solo se actualizan (si existen) las notas "deferred to Puntuaciones (SP-4)" para apuntar a la sección SignalR del contrato HTTP de Puntuaciones.

## 9. Riesgos y mitigaciones

- **Doble difusión en el reintento del worker** → la difusión se invoca únicamente tras el despacho que tuvo éxito (el intento fallido por `DbUpdateException` no difunde); como máximo un push por mensaje.
- **Fallo del hub/broadcast bloqueando proyecciones** → try/catch propio con `LogWarning`; el ack nunca depende de la difusión.
- **Costo de recomputar el ranking por push** (anticipado en SP-4b §9) → una query por evento de scoring, misma que sirve el GET; trivial a escala académica; si midiera mal, el punto de optimización es el dispatcher (caché puntual), sin tocar contrato.
- **`[Authorize]` rompiendo el arranque sin Keycloak** → el bloque condicional de `Program.cs` se mantiene; tests con `TestAuthHandler`; en despliegue real el gateway ya exige JWT, el servicio añade defensa en profundidad.
- **Suscriptores a partidas aún no proyectadas** (carrera entre publicar partida y suscribirse) → `HubException` clara; el cliente reintenta al recibir `PartidaEnLobby` de Operaciones de Sesión (documentado en el contrato; cableado real en SP-5).
- **Grupos residuales** → SignalR remueve conexiones de grupos al desconectar; `DesuscribirDePartida` cubre el abandono explícito.

## 10. Cierre del slice

- Ledger por tarea en `.superpowers/sdd/progress.md`; review final whole-branch del rango de commits SP-4c.
- Traceability + contratos actualizados (sección 8).
- Post-slice: queda únicamente SP-4d (auditoría/historial) para cerrar SP-4; el cableado de clientes al hub es SP-5.
