# HU-03 — Tasks

## Task status key

- [ ] Pending
- [x] Done

## Domain

- [x] Add/create `Equipo` creation behavior that enforces:
  - team starts with exactly one member (creator)
  - creator is leader
  - active team status on creation
  - cardinality invariant `1..5`
- [x] Ensure `CodigoAcceso` is part of aggregate creation state.

## Application

- [x] Create `CrearEquipoCommand` and response DTO/read model.
- [x] Create `CrearEquipoCommandValidator` for payload rules.
- [x] Create `CrearEquipoCommandHandler` with business flow:
  - verify actor not in active team
  - generate unique access code
  - create/persist team
- [x] Add business exceptions for conflict scenarios (`already belongs to active team`).

## Infrastructure

- [x] Extend Team repository contracts for HU-03 checks:
  - `ExistsActiveTeamByUserIdAsync`
  - `ExistsByAccessCodeAsync`
  - `AddAsync`
- [x] Implement repository methods in EF Core persistence.
- [x] Add access-code generator strategy/service with uniqueness retry.
- [x] Map persistence failures to consistent application exception handling.

## API

- [x] Add `POST /api/teams` endpoint in Team Service API.
- [x] Require authenticated participant access policy.
- [x] Map errors to HTTP statuses (`400`, `401`, `403`, `409`).
- [x] Return `201` with created team payload.

## Contracts

- [x] Add HU-03 concrete endpoint section to `contracts/http/team-api.md`.
- [x] If event is published, add `EquipoCreado v1` section to `contracts/events/team-events.md`.
- [x] Re-validate implemented payload/status against HU-03 contract definitions.

## Tests

- [x] Domain unit tests for team creation invariants (`1..5`, creator as leader/member).
- [x] Application unit tests for `CrearEquipoCommandHandler` success and conflicts.
- [x] Validator unit tests for create-team payload.
- [x] Integration tests for `POST /api/teams` (`201`, `400`, `401`, `403`, `409`).
- [x] Contract tests for HU-03 endpoint response shape and status codes.

## Frontend (React Native mobile)

- [x] Add create-team screen/form in participant mobile flow.
- [x] Add mobile API client method for `POST /api/teams`.
- [x] Handle `409` conflict message when participant already belongs to an active team.
- [x] Add mobile tests for happy path and main API errors.

## Acceptance and traceability

- [x] Update `docs/04-sdd/specs/HU-03-crear-equipo/acceptance.md` with executed evidence.
- [x] Update HU-03 row in `docs/04-sdd/traceability-matrix.md` (requirements/supporting services/contracts/status).
- [x] Align HU-03 status in `docs/04-sdd/SPECS-LIST.md` after implementation/testing.

## Review follow-up tasks required to close HU-03

- [x] Align Team Service runtime connection string with Docker Compose:
  - either change Docker Compose to provide `ConnectionStrings__TeamDatabase`
  - or change Team Service infrastructure to read the configured connection name consistently.
- [x] Add a Dockerfile for `services/team-service` or adjust Docker Compose so the Team Service container can build successfully.
- [x] Resolve active-user validation for HU-03:
  - either implement the contract-based Identity Service validation for an active participant
  - or update SDD/contracts to explicitly state that HU-03 relies only on token claims and does not query Identity Service.
- [x] Resolve `EquipoCreado` event behavior:
  - either implement real event publication/outbox/RabbitMQ adapter
  - or update SDD/contracts/traceability to document that HU-03 uses no real event publication.
- [x] Add EF Core migration or documented schema-creation strategy for Team Service PostgreSQL persistence.
- [x] Add or execute PostgreSQL-backed verification for `POST /api/teams` to prove persistence does not fall back to InMemory in runtime configuration.
- [x] Fix local .NET SDK reproducibility for tests:
  - install SDK `10.0.300`
  - or align `global.json` with the SDK required by the repository.
- [x] Re-run and record fresh backend test evidence:
  - unit tests
  - application/handler tests
  - integration tests
  - contract tests.
- [x] Re-run and record fresh mobile test evidence for HU-03 create-team flow.
- [x] Strengthen HU-03 contract tests to assert that the create-team response contains exactly one initial member.
- [x] Run and document a runtime smoke test with Team Service using PostgreSQL-backed configuration.
- [x] Update `acceptance.md` with the new verification evidence.
- [x] Update `docs/04-sdd/traceability-matrix.md` after resolving persistence, event and validation decisions.
- [x] Update HU-03 status in `docs/04-sdd/SPECS-LIST.md` only after the follow-up closure tasks are complete.

## Hardening tasks required to bring HU-03 to 10/10

- [x] Enforce the `one active team per participant` rule under concurrency:
  - add a persistence-level guarantee so two parallel create-team requests for the same `UsuarioId` cannot both succeed
  - avoid relying only on read-then-write validation in `CrearEquipoCommandHandler`
  - map the resulting persistence conflict to the documented business conflict response (`409`)
- [x] Handle access-code collisions correctly at persistence time:
  - detect unique-index conflicts for `CodigoAcceso` during save
  - retry code generation when the collision happens after the optimistic pre-check
  - avoid returning `500` for transient access-code race conditions
- [x] Add backend tests for concurrent-create scenarios:
  - prove that the same participant cannot end up in two active teams under parallel requests
  - prove that access-code uniqueness conflicts are retried or mapped to the expected contract result
- [x] Harden the mobile create-team flow for network/runtime failures:
  - catch rejected `fetch` calls in the API/flow layer
  - ensure the screen clears loading state on thrown errors
  - show a user-friendly retryable error message instead of leaving the promise unhandled
- [x] Add mobile tests for offline / thrown-network-error behavior.
- [x] Re-run acceptance evidence after hardening tasks and only then consider HU-03 as `10/10` quality-complete.
