---
name: rabbitmq-events
description: Design and implement RabbitMQ integration events for UMBRAL cross-service asynchronous workflows using the four-service topology.
compatibility: opencode
---

# RabbitMQ Events

Base yourself on:

- `docs/05-decisions/ADR-0006-four-service-topology.md`
- `docs/03-microservices/events-catalog.md`
- `contracts/events/`

## Valid event contract files

Use only:

- `contracts/events/identity-events.md`
- `contracts/events/team-events.md`
- `contracts/events/trivia-game-events.md`
- `contracts/events/bdt-game-events.md`

Do not use:

- `contracts/events/audit-events.md`
- `contracts/events/scoring-events.md`
- `contracts/events/session-events.md`
- `contracts/events/trivia-events.md`
- `contracts/events/treasure-hunt-events.md`

## Rules

- Events must be versioned.
- Events must be defined under the event file of the owning service.
- Services publish only events they own.
- Consumers must be idempotent where possible.
- Events represent facts that already happened.
- Do not use RabbitMQ for direct user-facing queries.
- Do not put business decisions in consumers that belong to another service's domain.
- Do not create Audit Service or Scoring Service events as standalone service contracts.

## Event naming style

Use past-tense names tied to the owning service context:

- `TeamCreated`
- `TeamMemberJoined`
- `TriviaAnswerSubmitted`
- `RespuestaTriviaValidada`
- `PuntajeTriviaIncrementado`
- `TreasureQrSubmitted`
- `HitoBDTEncontrado`
- `PuntajeBDTIncrementado`
