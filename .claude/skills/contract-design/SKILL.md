---
name: contract-design
description: Maintain HTTP and event contracts between UMBRAL frontend, gateway and the four approved microservices.
compatibility: opencode
---

# Contract Design

Use this skill whenever a feature creates or changes an endpoint, event, DTO, or cross-service interaction.

## Read

- `contracts/http/`
- `contracts/events/`
- `docs/03-microservices/api-contracts.md`
- `docs/03-microservices/events-catalog.md`
- `docs/03-microservices/communication-map.md`

## Valid HTTP contract files

- `contracts/http/identity-api.md`
- `contracts/http/team-api.md`
- `contracts/http/trivia-game-api.md`
- `contracts/http/bdt-game-api.md`

## Valid event contract files

- `contracts/events/identity-events.md`
- `contracts/events/team-events.md`
- `contracts/events/trivia-game-events.md`
- `contracts/events/bdt-game-events.md`

## Rules

- Do not invent endpoints in frontend code.
- Contract changes must be documented before implementation.
- HTTP contracts belong in `contracts/http/<service>-api.md`.
- Event contracts belong in `contracts/events/<service>-events.md`.
- Cross-service workflows must identify publisher and consumer.
- Event payloads must include versioning.
- Breaking changes must be called out in the SDD design.
- Contracts must be traceable to the user story and owning service.
- Do not create contracts for Audit Service, Scoring Service, Trivia Service or Treasure Hunt Service.

## Required contract fields

For HTTP endpoints:

- Method
- Path
- Auth / role requirements
- Request body
- Response body
- Error cases
- Owning service

For events:

- Event name
- Version
- Publisher
- Consumers
- Trigger
- Payload
- Idempotency key or deduplication strategy when applicable
