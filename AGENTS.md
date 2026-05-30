# UMBRAL - OpenCode Project Instructions

## Project identity

UMBRAL is an academic software engineering project for operating real-time interactive experiences under two game modes:

- Trivia
- Búsqueda del Tesoro / BDT

Do not create, infer, implement or document additional game modes.

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
- Trivia form creation;
- Trivia game creation and supervision;
- BDT game creation and supervision;
- lobby supervision;
- rankings visible to operator;
- BDT geolocation map for operator;
- history and traceability views.

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

### Client routing rule

- Stories with actor `Administrador` or `Operador` belong to React web unless the approved SDD explicitly says otherwise.
- Stories with actor `Participante` belong to React Native mobile unless the approved SDD explicitly says otherwise.
- Stories with actor `Participante líder` or `Líder de equipo` belong to React Native mobile when the user acts as a participant.
- Stories with actor `Sistema` belong to backend/internal processing.

Do not implement participant gameplay screens in the React web frontend unless an SDD explicitly says so.

Do not implement administrator/operator screens in the React Native mobile app unless an SDD explicitly says so.

## Mandatory service topology

The backend must be implemented as physical microservices. Do not collapse the backend into a modular monolith.

Target services:

1. Identity Service
2. Team Service
3. Trivia Game Service
4. BDT Game Service

Do not create additional backend microservices unless an explicit future ADR approves the change.

## Source hierarchy

Use documents in this order of authority:

1. `docs/01-project-source/` for raw project artifacts created by the team.
2. `docs/02-project-context/` for operational summaries used by OpenCode.
3. `docs/03-microservices/` for service ownership and communication.
4. `docs/04-sdd/` for feature-by-feature implementation.
5. `docs/05-decisions/` for accepted architecture decisions.
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

- Identity Service owns users, roles, Keycloak mapping and local user references.
- Team Service owns teams, team codes, team members, leadership, team status and team membership rules.
- Trivia Game Service owns trivia forms, questions, options, trivia sessions, trivia participants, trivia answers, trivia scoring, trivia ranking, trivia lobby, trivia history and trivia real-time updates.
- BDT Game Service owns BDT sessions, areas, stages, clues, expected QR codes, treasure uploads, QR validation, BDT progress, BDT ranking, BDT history, BDT geolocation updates and BDT real-time updates.

A service must never read or write another service's database directly.

## BDT ranking rule

BDT ranking is not based on numeric accumulated score.

BDT ranking is based on:

1. highest number of stages won;
2. if tied, lowest accumulated time only across stages won.

Use these concepts for BDT:

- `EtapasGanadas`
- `TiempoAcumuladoEtapasGanadas`
- `RankingBDTActualizado`

Do not use these as active BDT ranking concepts:

- `PuntajeEtapa`
- `PuntajeAcumulado` for BDT ranking
- `PuntajeBDTIncrementado`

Trivia may still use `PuntajeAsignado`, `PuntajeAcumulado` and `PuntajeTriviaIncrementado`.

## Explicit non-services

Do not create or reference these as active physical services:

- Audit Service
- Scoring Service
- Trivia Service
- Treasure Hunt Service
- Notification Service

Audit, ranking, history and notifications are responsibilities inside the owning service of the corresponding business flow unless a future ADR changes the topology.

## Academic brief adaptation

The original academic brief describes missions, live sessions and evidence submissions. UMBRAL adapts that vocabulary into:

| Academic brief | UMBRAL adaptation |
|---|---|
| Mission | Trivia form / BDT configuration |
| Mission nodes or stages | Trivia questions / BDT stages |
| LiveSession | PartidaTrivia / PartidaBDT |
| EvidenceSubmission | RespuestaTrivia / TesoroQR |
| Team | Equipo |
| SessionEvent | EventoHistorial |
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
