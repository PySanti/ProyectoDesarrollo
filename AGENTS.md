# UMBRAL - OpenCode Instructions

## Project

UMBRAL is an academic software engineering project for operating real-time immersive investigation experiences.

The system supports only two game modes:

- Trivia
- Búsqueda del Tesoro

Do not create, infer or implement additional game modes.

## Mandatory architecture

This project must be implemented using physical microservices.

Mandatory services:

1. Identity Service
2. Team Service
3. Trivia Service
4. Treasure Hunt Service
5. Scoring Service
6. Audit Service

Do not collapse these services into a modular monolith.

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

## Professor material

The professor provided these official files:

- Agents:
  - `docs/00-professor-source/agents/architect-agent.md`
  - `docs/00-professor-source/agents/backend-agent.md`
  - `docs/00-professor-source/agents/devops-agent.md`
  - `docs/00-professor-source/agents/frontend-agent.md`
  - `docs/00-professor-source/agents/qa-agent.md`

- Rules:
  - `docs/00-professor-source/rules/coding-standards.md`
  - `docs/00-professor-source/rules/project-rules.md`

- Skills:
  - `docs/00-professor-source/skills/cqrs-mediatr-skill.md`
  - `docs/00-professor-source/skills/ddd-modeling-skill.md`
  - `docs/00-professor-source/skills/efcore-postgres-skill.md`
  - `docs/00-professor-source/skills/rabbitmq-events-skill.md`
  - `docs/00-professor-source/skills/testing-skill.md`
  - `docs/00-professor-source/skills/websocket-signalr-skill.md`

- Specs:
  - `docs/00-professor-source/specs/umbral-architecture-spec.md`
  - `docs/00-professor-source/specs/umbral-backend-spec.md`
  - `docs/00-professor-source/specs/umbral-frontend-spec.md`
  - `docs/00-professor-source/specs/umbral-product-spec.md`
  - `docs/00-professor-source/specs/umbral-quality-spec.md`

Use the operational OpenCode versions under `.opencode/agents/` and `.opencode/skills/`.

## Required reading before coding

Before planning or implementing any feature, read:

- `docs/02-project-context/project-brief.md`
- `docs/02-project-context/srs-summary.md`
- `docs/02-project-context/business-rules.md`
- `docs/02-project-context/first-delivery-scope.md`
- `docs/03-microservices/microservices-map.md`
- `docs/03-microservices/service-ownership.md`
- `docs/03-microservices/communication-map.md`
- `docs/04-sdd/sdd-workflow.md`
- The related SDD folder under `docs/04-sdd/specs/`

## SDD is mandatory

Never implement directly from a vague prompt.

For every feature:

1. Identify the user story.
2. Identify the owning microservice.
3. Read the SDD files.
4. Update `spec.md` if needed.
5. Update `design.md` if needed.
6. Update `tasks.md`.
7. Implement one task at a time.
8. Add or update tests.
9. Update `acceptance.md`.
10. Update `docs/04-sdd/traceability-matrix.md`.

## Service boundary rules

- Identity Service owns users, roles and Keycloak mapping.
- Team Service owns teams and team members.
- Trivia Service owns quizzes, questions, options, trivia sessions and trivia answers.
- Treasure Hunt Service owns missions, stages, clues, evidence and treasure hunt sessions.
- Scoring Service owns scores, score movements and rankings.
- Audit Service owns immutable event history.

A service must not directly access another service's database.

## Quality rules

- Domain must not depend on infrastructure.
- Controllers must not contain business rules.
- Commands modify state.
- Queries do not modify state.
- Cross-service asynchronous workflows use RabbitMQ.
- Real-time visible updates use SignalR/WebSockets.
- Business rules must have tests.
- Any completed feature must update traceability.

## SDD generation rule

If the related `spec.md`, `design.md`, `tasks.md` or `acceptance.md` contains TODO sections, OpenCode must complete those SDD files first.

Do not implement code while the related SDD files are incomplete.

The correct order is:

1. Complete or refine `spec.md`.
2. Ask for user review or approval.
3. Complete or refine `design.md`.
4. Complete or refine `tasks.md`.
5. Implement one task at a time.
6. Update tests, `acceptance.md` and `traceability-matrix.md`.

## Design context rule

Before completing a feature spec or design, OpenCode must read:

- `docs/02-project-context/design/domain-business-rules.md`
- `docs/02-project-context/design/domain-entities-by-context.md`
- `docs/02-project-context/design/class-design-by-layer.md`
- `docs/02-project-context/design/service-model-impact.md`

Use the PlantUML source files only when extra detail is needed.