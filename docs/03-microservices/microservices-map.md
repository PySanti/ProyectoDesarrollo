# UMBRAL Microservices Map

UMBRAL is implemented as four independent .NET 8 backend microservices behind a mandatory YARP gateway. Each service follows Clean Architecture + CQRS (MediatR) and owns its own PostgreSQL database.

| Service | Responsibility | Persistence |
|---|---|---|
| Identity | Users, Keycloak mapping, roles, permissions, governance, teams, team membership, leadership, invitations, temporary credential notification | `umbral_identity` |
| Partidas | Partida configuration, sequential Juegos, Trivia questions/options, BDT stages and expected QR text, modality, participation limits, start configuration | `umbral_partidas` |
| Operaciones de Sesion | Runtime lobby, inscriptions, convocatorias, live start, synchronization, answer/QR validation, clues, geolocation, reconnection, session SignalR | `umbral_operaciones_sesion` |
| Puntuaciones | Scoring, native rankings, consolidated ranking, audit/history projections, ranking SignalR | `umbral_puntuaciones` |

## Gateway

The YARP gateway is mandatory but does not own domain state.

It is the single entry point for all client traffic — including real-time (WebSockets/SignalR). It validates the Keycloak JWT and applies coarse, route-level authorization by base role (`Administrador`/`Operador`/`Participante`) without querying Identity on every request. Fine-grained functional-permission authorization stays inside each service. The gateway owns no domain logic, scores, rankings or database.

## DDD contexts per service

| Service | DDD contexts materialized |
|---|---|
| Identity | Identidad (Generic) + Equipos (Support) + permission/role governance |
| Partidas | Partidas (Core) + Trivia/BDT configuration |
| Operaciones de Sesion | Trivia/BDT runtime + Participación (Support) |
| Puntuaciones | Scoring + ranking + Auditoría/Historial (cross-cutting) |

## Hard boundaries

- A service must never read or write another service's database.
- Cross-service async workflows use RabbitMQ; user-visible real-time updates use SignalR/WebSockets, routed through the gateway.
- Domain must not depend on infrastructure; controllers contain no business rules; commands mutate state, queries do not.
- The backend is authoritative for business rules; frontend/mobile validate for usability only.

## Ranking doctrine (current)

- **Trivia native ranking** (per `JuegoTrivia`): order by `PuntajeAcumulado` descending (time never modifies points); tie-break by lowest accumulated answer time.
- **BDT native ranking** (per `JuegoBDT`): order by accumulated points = sum of the `Puntaje` of the **won stages**; tie-break by lowest accumulated time of the **won stages only**. The count of stages won is kept as **informative data only**, not as the sort key.
- **Consolidated partida ranking** (`RankingConsolidado`, computed on finish): order by (1) number of games won, then (2) total accumulated points across all games, then (3) lowest total time.

> The old "BDT ranks by number of stages won, not points" rule is **superseded**. BDT now uses per-stage `Puntaje` and ranks by accumulated points.

## Obsolete decomposition

The previous physical services — `Team Service`, `Trivia Game Service`, `BDT Game Service` (a.k.a. Treasure Hunt Service) — are **obsolete** and must not be reintroduced as active services. Their responsibilities are now distributed: teams → Identity; Trivia/BDT configuration → Partidas; Trivia/BDT runtime → Operaciones de Sesion; scoring/ranking/audit → Puntuaciones. Likewise, do not create `Scoring Service`, `Audit Service` or `Notification Service` as separate services: scoring/ranking is Puntuaciones, audit/history is materialized in Puntuaciones and Operaciones de Sesion, and email notification lives inside Identity.

## Client topology

| Client | Actors | Main responsibility |
|---|---|---|
| React web | Administrador, Operador | User and team administration, role/permission governance, partida creation (Trivia questions / BDT stages), publishing, lobby, live operation, rankings, clue delivery, geolocation map, history (admin views operations read-only) |
| React Native mobile | Participante, Líder de equipo as participant | Joining partidas, team actions and invitations, accepting/rejecting convocatorias, answering Trivia, BDT QR upload, receiving clues, BDT geolocation sharing |
| Backend services | Sistema | Authoritative business rules and internal processing |

All client ↔ backend traffic passes through the gateway; there is no direct client → service contact.
