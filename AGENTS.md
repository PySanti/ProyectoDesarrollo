# UMBRAL - OpenCode Project Instructions

## Project identity

UMBRAL is an academic software engineering project for operating real-time interactive experiences under two game modes:

- Trivia
- Búsqueda del Tesoro / BDT

Do not create, infer, implement or document additional game modes.

## Mandatory stack

- Frontend: React
- Backend: .NET Core
- Persistence: PostgreSQL
- ORM: Entity Framework Core
- Application style: CQRS + MediatR
- Real-time communication: WebSockets / SignalR
- Async messaging: RabbitMQ
- Local execution: Docker Compose
- Architecture: Clean Architecture / Hexagonal Architecture
- Testing: unit, integration and E2E where applicable

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
- `docs/02-project-context/srs-summary.md`
- `docs/02-project-context/business-rules.md`
- `docs/02-project-context/first-delivery-scope.md`
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
- the related folder under `docs/04-sdd/specs/`.

## SDD is mandatory

Never implement directly from a vague prompt.

For every feature:

1. Identify the user story.
2. Confirm the spec appears in `docs/04-sdd/SPECS-LIST.md`.
3. Identify the owning microservice.
4. Read source context and SDD files.
5. Complete or refine `spec.md`.
6. Complete or refine `design.md`.
7. Complete or refine `tasks.md`.
8. Implement one task at a time.
9. Add or update tests.
10. Update `acceptance.md`.
11. Update `docs/04-sdd/traceability-matrix.md`.

If `spec.md`, `design.md`, `tasks.md`, or `acceptance.md` contains TODO sections, do not code. Complete the SDD first.

## Service ownership rules

- Identity Service owns users, roles, Keycloak mapping and local user references.
- Team Service owns teams, team codes, team members, leadership, team status and team membership rules.
- Trivia Game Service owns trivia forms, questions, options, trivia sessions, trivia participants, trivia answers, trivia scoring, trivia ranking, trivia lobby, trivia history and trivia real-time updates.
- BDT Game Service owns BDT sessions, areas, stages, clues, expected QR codes, evidence uploads, QR validation, BDT progress, BDT scoring, BDT ranking, BDT history, BDT geolocation updates and BDT real-time updates.

A service must never read or write another service's database directly.

## Explicit non-services

Do not create or reference these as active physical services:

- Audit Service
- Scoring Service
- Trivia Service
- Treasure Hunt Service
- Notification Service

## Quality rules

- Domain must not depend on infrastructure.
- Controllers must not contain business rules.
- Commands modify state.
- Queries do not modify state.
- Cross-service asynchronous workflows use RabbitMQ.
- User-visible real-time updates use SignalR/WebSockets.
- Business rules require tests.
- Completed features require updated acceptance evidence and traceability.
