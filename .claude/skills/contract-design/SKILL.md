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

- `contracts/http/gateway-api.md`
- `contracts/http/identity-api.md`
- `contracts/http/partidas-api.md`
- `contracts/http/operaciones-sesion-api.md`
- `contracts/http/puntuaciones-api.md`

## Valid event contract files

- `contracts/events/identity-events.md`
- `contracts/events/partidas-events.md`
- `contracts/events/operaciones-sesion-events.md`
- `contracts/events/puntuaciones-events.md`

## Rules

- Do not invent endpoints in frontend code.
- Contract changes must be documented before implementation.
- HTTP contracts belong in `contracts/http/<service>-api.md`.
- Event contracts belong in `contracts/events/<service>-events.md`.
- Cross-service workflows must identify publisher and consumer.
- Event payloads must include versioning.
- Breaking changes must be called out in the SDD design.
- Contracts must be traceable to the user story and owning service.
- Do not create contracts for the obsolete / superseded services: Team Service, Trivia Game Service, BDT Game Service, Treasure Hunt Service, Audit Service, Scoring Service or Notification Service. Active contracts cover only Identity, Partidas, Operaciones de Sesion, Puntuaciones and the gateway.

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
