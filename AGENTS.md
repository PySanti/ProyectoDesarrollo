# UMBRAL - OpenCode Project Instructions

## Project identity

UMBRAL is an academic software engineering project for operating real-time interactive experiences under exactly two game modes:

- Trivia
- Búsqueda del Tesoro / BDT

Do not create, infer, implement or document additional game modes, generic mission/session/evidence modules, or generic workflows unless an approved SDD explicitly maps them to current UMBRAL vocabulary.

A `Partida` is the published, joined and ranked unit. It contains one or more sequential `Juego`s, and each `Juego` is exactly `JuegoTrivia` or `JuegoBDT`. Lobby, inscription, modality, start mode, lifecycle and consolidated ranking are partida-level concerns.

## Migration status

The current doctrine is the four-service target architecture defined by the source documents and ADR-0008. The repository is being migrated from a previous implementation layout.

The repository may still contain folders from the previous implementation layout. Those folders are migration debt, not active doctrine.

Do not treat existing old folder names, ports, database names or run scripts as proof of active service boundaries. Do not claim code migration is complete unless the related SDD/ADR explicitly covers that migration slice.

## Mandatory stack

- Web frontend: React
- Mobile frontend: React Native
- Backend: .NET Core
- Persistence: PostgreSQL
- ORM: Entity Framework Core
- Application style: CQRS + MediatR
- Real-time communication: WebSockets / SignalR
- Async messaging: RabbitMQ
- Local execution: Docker Compose
- Architecture: Clean Architecture / Hexagonal Architecture
- Testing: unit, integration and E2E where applicable

## Client topology

UMBRAL has two frontend clients.

### Web React client

Used by:

- Administrador
- Operador

Owns:

- administration flows;
- operator flows;
- user administration;
- team administration by administrator;
- Trivia game creation and supervision;
- BDT game creation and supervision;
- lobby supervision;
- rankings visible to operator;
- BDT geolocation map for operator;
- history and traceability views.

The web client calls backend capabilities only through the mandatory YARP gateway. It must not call microservices directly.

### Mobile React Native client

Used by:

- Participante
- Líder de equipo when acting as a participant

Owns participant gameplay flows:

- listing published Trivia and BDT games;
- filtering games by modality;
- team membership actions as participant;
- joining individual games;
- team preinscription by leader;
- accepting or rejecting convocatorias;
- answering Trivia;
- viewing Trivia question results;
- viewing BDT active stage;
- uploading QR treasure images;
- receiving BDT clues;
- sharing BDT geolocation when required;
- receiving participant-facing in-app notifications.

The mobile client calls backend capabilities only through the mandatory YARP gateway. It must not call microservices directly.

### Client routing rule

- Stories with actor `Administrador` or `Operador` belong to React web unless the approved SDD explicitly says otherwise.
- Stories with actor `Participante` belong to React Native mobile unless the approved SDD explicitly says otherwise.
- Stories with actor `Participante líder` or `Líder de equipo` belong to React Native mobile when the user acts as a participant.
- Stories with actor `Sistema` belong to backend/internal processing.

Do not implement participant gameplay screens in the React web frontend unless an SDD explicitly says so.

Do not implement administrator/operator screens in the React Native mobile app unless an SDD explicitly says so.

## Mandatory service topology

The backend must be implemented as physical microservices behind the mandatory YARP gateway. Do not collapse the backend into a modular monolith.

Target services:

1. Identity
2. Partidas
3. Operaciones de Sesion
4. Puntuaciones

Do not create additional backend microservices unless an explicit future ADR approves the change.

The previous physical services `Team Service`, `Trivia Game Service` and `BDT Game Service` are legacy implementation boundaries only. Their responsibilities are redistributed as follows:

- Identity owns users, roles, Keycloak mapping, permissions/governance, teams, memberships, leadership, `InvitacionEquipo`, team history and temporary-credential email responsibility.
- Partidas owns partida and game configuration, including sequential `Juego`s, Trivia questions and BDT stages.
- Operaciones de Sesion owns live runtime, publishing to lobby, inscriptions, convocatorias, synchronized Trivia/BDT execution, validation, clues, geolocation and session real-time updates.
- Puntuaciones owns scoring, native rankings, consolidated ranking, team-performance queries and audit/history projections.

## Mandatory gateway

YARP is the single backend entry point for web and mobile clients, including SignalR/WebSocket traffic. The gateway validates the Keycloak JWT and applies coarse route-level authorization by base role (`Administrador`, `Operador`, `Participante`) without querying Identity on every request.

Fine-grained functional-permission checks remain inside the owning microservices. The gateway owns no domain logic, scores, rankings, events or database access.

## Source hierarchy

Use documents in this order of authority:

1. `docs/01-project-source/` for raw project artifacts created by the team.
2. `docs/02-project-context/` for operational summaries used by OpenCode.
3. `docs/03-microservices/` for service ownership and communication.
4. `docs/04-sdd/` for feature-by-feature implementation.
5. `docs/05-decisions/` for accepted architecture decisions, including ADR-0008 when resolving doctrine replacement.
6. `contracts/` for HTTP and event contracts.

Do not treat `.opencode/` as a place for domain documentation. `.opencode/` is only for agents, commands and skills.

## Required reading before planning or coding

Before any feature work, read:

- `docs/02-project-context/project-brief.md`
- `docs/02-project-context/adaptation-to-academic-brief.md`
- `docs/02-project-context/srs-summary.md`
- `docs/02-project-context/business-rules.md`
- `docs/02-project-context/first-delivery-scope.md`
- `docs/02-project-context/mobile-participant-context.md`
- `docs/02-project-context/bdt-ranking-clarification.md`
- `docs/02-project-context/design/design-index.md`
- `docs/02-project-context/design/domain-business-rules.md`
- `docs/02-project-context/design/domain-entities-by-context.md`
- `docs/02-project-context/design/class-design-by-layer.md`
- `docs/02-project-context/design/service-model-impact.md`
- `docs/03-microservices/microservices-map.md`
- `docs/03-microservices/service-ownership.md`
- `docs/03-microservices/communication-map.md`
- `docs/04-sdd/SPECS-LIST.md`
- `docs/04-sdd/sdd-workflow.md`
- `docs/05-decisions/ADR-0007-adapted-academic-scope-mobile-and-bdt.md`
- `docs/05-decisions/ADR-0008-documentation-doctrine-replacement.md`
- the related folder under `docs/04-sdd/specs/`.

For mobile features, also read:

- `mobile/mobile-context.md`
- `.opencode/agents/mobile.md`
- `.opencode/skills/react-native-mobile/SKILL.md`

## SDD is mandatory

Never implement directly from a vague prompt.

For every feature:

1. Identify the user story.
2. Confirm the spec appears in `docs/04-sdd/SPECS-LIST.md`.
3. Identify the owning microservice.
4. Identify the client target: React web, React Native mobile, backend, or mixed.
5. Read source context and SDD files.
6. Complete or refine `spec.md`.
7. Complete or refine `design.md`.
8. Complete or refine `tasks.md`.
9. Implement one task at a time.
10. Add or update tests.
11. Update `acceptance.md`.
12. Update `docs/04-sdd/traceability-matrix.md`.

If `spec.md`, `design.md`, `tasks.md`, or `acceptance.md` contains TODO sections, do not code. Complete the SDD first.

## Service ownership rules

- Identity owns users, roles, Keycloak mapping, local user references, permissions/governance, teams, memberships, leadership, `InvitacionEquipo`, team status and team history.
- Partidas owns partida configuration, sequential `Juego`s, Trivia questions/options and BDT stage configuration.
- Operaciones de Sesion owns live runtime, lobbies, inscriptions, convocatorias, answer/QR validation, clues, geolocation and session real-time updates.
- Puntuaciones owns scoring, native rankings, consolidated ranking, team-performance queries and audit/history projections.

A service must never read or write another service's database directly.

## BDT ranking rule

BDT native ranking is point-based under the current doctrine:

1. highest accumulated points from won BDT stages, where each won `EtapaBDT` grants its configured `Puntaje`;
2. if tied, lowest accumulated time only across won stages.

`EtapasGanadas` is informative data only, not the primary sort key. Active BDT ranking uses `RankingBDTActualizado`; the event `EtapaBDTGanada` carries the stage `Puntaje` consumed by Puntuaciones.

Trivia may still use `PuntajeAsignado`, `PuntajeAcumulado` and `PuntajeTriviaIncrementado`.

## Explicit non-services

Do not create or reference these as active physical services:

- Team Service
- Trivia Game Service
- BDT Game Service
- Audit Service
- Scoring Service
- Trivia Service
- Treasure Hunt Service
- Notification Service

Teams live inside Identity. Trivia/BDT configuration lives inside Partidas. Trivia/BDT runtime lives inside Operaciones de Sesion. Audit/history and scoring/ranking are materialized by Puntuaciones, with Operaciones de Sesion materializing its own runtime history. Temporary-credential email responsibility lives inside Identity unless a future ADR changes the topology.

## Legacy evidence rule

Legacy SDDs and contracts may remain only under `docs/04-sdd/_legacy-implementation-evidence/` and `contracts/**/_legacy/`. They are historical implementation evidence, not active planning input.

## Academic brief adaptation

The original academic brief describes missions, live sessions and evidence submissions. UMBRAL adapts that vocabulary into:

| Academic brief | UMBRAL adaptation |
|---|---|
| Mission | Partida |
| Mission nodes or stages | Juego (JuegoTrivia / JuegoBDT), with Trivia Pregunta and BDT EtapaBDT as inner steps |
| LiveSession | the live session managed by Operaciones de Sesion |
| EvidenceSubmission | RespuestaTrivia / TesoroQR |
| Team | Equipo |
| SessionEvent | EventoHistorial / RegistroAuditoria |
| ScoreEntry | Trivia score event / BDT ranking event |

Do not implement generic mission/session/evidence modules unless an SDD explicitly maps them to current UMBRAL vocabulary.

## Quality rules

- Domain must not depend on infrastructure.
- Controllers must not contain business rules.
- Commands modify state.
- Queries do not modify state.
- Cross-service asynchronous workflows use RabbitMQ.
- User-visible real-time updates use SignalR/WebSockets.
- Business rules require tests.
- Completed features require updated acceptance evidence and traceability.
- Backend remains authoritative for business rules.
- Frontend and mobile may validate for usability only, not as source of truth.
