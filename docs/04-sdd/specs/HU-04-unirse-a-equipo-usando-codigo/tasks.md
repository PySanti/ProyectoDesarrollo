# HU-04 — Tasks

## Task status key

- [ ] Pending
- [x] Done

## Domain

- [x] Add/extend `Equipo` behavior to allow joining by access code while enforcing:
  - no duplicate member inside the same team
  - joined participant is not leader
  - cardinality invariant `1..5`
  - rejection when team already has 5 members

## Application

- [x] Create `UnirseAEquipoPorCodigoCommand` and response DTO/read model.
- [x] Create `UnirseAEquipoPorCodigoCommandValidator` for payload rules.
- [x] Create `UnirseAEquipoPorCodigoCommandHandler` with business flow:
  - verify actor not in active team
  - normalize access code
  - load active team by code
  - add participant to aggregate
  - persist changes
- [x] Add business exceptions for conflict/not-found scenarios:
  - already belongs to active team
  - team not found by access code
  - team full

## Infrastructure

- [x] Extend Team repository contracts for HU-04 checks:
  - `GetActiveByAccessCodeAsync`
  - persistence/update support for aggregate join flow
- [x] Implement repository methods in EF Core persistence.
- [x] Ensure access-code lookup is normalized consistently (`trim + uppercase`).
- [x] Map persistence failures to consistent application exception handling.

## API

- [x] Add `POST /api/teams/join-by-code` endpoint in Team Service API.
- [x] Require authenticated participant access policy.
- [x] Map errors to HTTP statuses (`400`, `401`, `403`, `404`, `409`).
- [x] Return `200` with updated team payload.

## Contracts

- [x] Add HU-04 concrete endpoint section to `contracts/http/team-api.md`.
- [x] Document that HU-04 publishes no required integration event in `contracts/events/team-events.md` if kept that way.
- [x] Re-validate implemented payload/status against HU-04 contract definitions.

## Tests

- [x] Domain unit tests for join-team invariants (member added, non-leader, max 5, duplicate rejection).
- [x] Application unit tests for `UnirseAEquipoPorCodigoCommandHandler` success and conflicts.
- [x] Validator unit tests for join-by-code payload.
- [x] Integration tests for `POST /api/teams/join-by-code` (`200`, `400`, `401`, `403`, `404`, `409`).
- [x] Contract tests for HU-04 endpoint response shape and status codes.

## Frontend (React Native mobile)

- [x] Add join-team screen/form in participant mobile flow.
- [x] Add mobile API client method for `POST /api/teams/join-by-code`.
- [x] Handle `404` invalid code message.
- [x] Handle `409` conflict message when participant already belongs to an active team or target team is full.
- [x] Add mobile tests for happy path and main API errors.

## Acceptance and traceability

- [x] Update `docs/04-sdd/specs/HU-04-unirse-a-equipo-usando-codigo/acceptance.md` with executed evidence.
- [x] Update HU-04 row in `docs/04-sdd/traceability-matrix.md` (requirements/supporting services/contracts/status).
- [x] Align HU-04 status in `docs/04-sdd/SPECS-LIST.md` after implementation/testing.

## Hardening to reach 10/10

- [x] Add reproducible mobile TypeScript validation by ensuring the project uses the local `typescript` dependency and documenting/running `npx tsc --noEmit` or an equivalent `npm run typecheck` script.
- [x] Add or document manual runtime evidence for the React Native join-team flow on device/emulator, including valid code, invalid code (`404`) and business conflict (`409`).
- [x] Add a mobile UI/component-level test or E2E-style test for `JoinTeamScreen` covering submit success, loading state and error rendering.
- [x] Add backend concurrency protection for simultaneous joins when a team has 4 members, preventing two requests from persisting a sixth member.
- [x] Add an automated concurrency/integration test that proves the team cardinality invariant `1..5` is preserved under concurrent join attempts.
- [x] Review the global unique index on `equipos_participantes.usuarioid` against future HU-07/HU-05 behavior so historical or inactive memberships do not block valid future team joins.
- [x] Map persistence unique-membership conflicts during HU-04 join to a business `409` instead of generic `500` when the participant concurrently joins another team.
- [x] Update `acceptance.md`, `traceability-matrix.md` and `SPECS-LIST.md` with `hardening-10-10` only after all hardening evidence is executed.
