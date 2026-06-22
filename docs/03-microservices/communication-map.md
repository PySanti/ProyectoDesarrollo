# Communication Map

> **Authority:** the communication rules below come from `CLAUDE.md` and `docs/01-project-source/microservicios.md`. Concrete HTTP routes, queue/exchange/routing-key names and SignalR shapes are not invented here; they are defined per HU during SDD and recorded under `contracts/`.

## Communication rules

- **Clients call the gateway, never the services directly.** All frontend/mobile ↔ backend traffic passes through the YARP gateway, including real time. There is no direct client → service contact.
- **The gateway validates the Keycloak JWT and applies coarse route-level authorization by base role** (`Administrador`/`Operador`/`Participante`) using token claims, without querying Identity on every request.
- **Services enforce functional permissions and domain rules locally.** Fine-grained authorization (`GestionarPartidas`, `GestionarEquipos`, `ParticiparEnPartidas`) and all business rules live inside the owning service; services also validate JWT audience/issuer (`KEYCLOAK_VALID_AUDIENCES` / `KEYCLOAK_VALID_ISSUERS`) as defense in depth.
- **RabbitMQ carries domain events between services.** Cross-service async workflows — scoring/ranking, audit, history consolidation, internal notifications, temporary-credential email — flow over RabbitMQ so they do not block the main flow.
- **SignalR/WebSockets are user-visible and routed through the gateway.** Real-time updates (lobby, partida states, timers, ranking, stages, clues, geolocation, results, device synchronization) go over SignalR/WebSockets through the gateway.
- **No service reads or writes another service's database.** Each service owns its own PostgreSQL database (`umbral_identity`, `umbral_partidas`, `umbral_operaciones_sesion`, `umbral_puntuaciones`); cross-context data flows only via events or gateway-routed HTTP.

## Authentication and token lifecycle

- Keycloak realm `UMBRAL-UCAB` with base realm roles `Administrador`, `Operador`, `Participante`. Web client `umbral-web`, mobile client `umbral-mobile` (PKCE S256). UMBRAL stores no passwords; only a local reference keyed by the Keycloak identifier.
- Web and mobile authenticate directly with Keycloak, not with the backend. The token carries user data, base role and permissions; the gateway validates it and authorizes by role at the route level.
- Token refresh happens only between the client and Keycloak — neither the gateway nor the backend participate.

## Async event flow (conceptual)

| Producer | Typical consumers | Purpose |
|---|---|---|
| Identity | (async notification pipeline) | Temporary-credential email on user creation / email change. |
| Operaciones de Sesion | Puntuaciones (and own audit materialization) | Runtime domain events: publication, start, game/stage activation, answer/QR validation, won stages, finish. |
| Puntuaciones | (SignalR broadcast to clients via gateway) | Ranking updates derived from runtime events. |

Concrete exchange/queue/routing-key names, payload schemas, idempotency and outbox policy are defined per HU in SDD and recorded in `contracts/events/`.

## Real-time flow (conceptual)

The user-visible real-time channel covers: partida publication, lobby changes, partida states, Trivia questions, BDT stages, timers, ranking, clues, geolocation, results and synchronization across authorized devices of the same team. Session real time is served by **Operaciones de Sesion**; ranking real time is broadcast by **Puntuaciones**. Both reach clients only through the gateway. Concrete hub names and message shapes are defined per HU in SDD.

## Rule for SDD

Each HU `design.md` that requires communication must specify:

| Question | Required answer |
|---|---|
| Is the communication user-visible in real time? | If yes, SignalR/WebSocket through the gateway. |
| Is the communication a non-blocking side effect across services? | If yes, RabbitMQ domain event. |
| Does it require data owned by another context? | Define an explicit gateway-routed contract; never read another service's DB. |
| Does it mutate state? | It must be a command/use case of the owning service. |
