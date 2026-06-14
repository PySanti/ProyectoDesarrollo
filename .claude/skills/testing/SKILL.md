---
name: testing
description: Apply UMBRAL testing strategy for unit, application, integration, contract and E2E tests.
compatibility: opencode
---

# Testing

Base yourself on:

- `docs/04-sdd/sdd-definition-of-done.md`
- `docs/04-sdd/sdd-definition-of-ready.md`
- related SDD feature folder

## Rules

- Every business rule needs at least one test.
- Domain invariants should be unit tested.
- Application handlers should be tested with mocks, fakes or stubs.
- EF Core persistence should be covered by integration tests where relevant.
- Cross-service flows should have contract or integration tests.
- Frontend critical flows should have E2E tests where applicable.
- Completed features must update `acceptance.md` with evidence.
- Completed features must update `traceability-matrix.md`.

## Test mapping

- Domain rule → unit test
- Command handler → application test
- Repository / DB mapping → integration test
- RabbitMQ event contract → contract or integration test
- SignalR update → integration or E2E test
- Critical frontend flow → E2E test
