# HU-06 — Tasks

## Task status key

- [ ] Pending
- [x] Done

## Domain

- [x] Add/extend `Equipo` behavior to transfer leadership while enforcing:
  - only the current leader can transfer leadership;
  - target new leader must be another current member of the same active team;
  - exactly one leader remains after transfer;
  - team cardinality remains unchanged and valid (`1..5`).
- [x] Add domain exceptions or result values for:
  - actor is not current leader;
  - target new leader is not a team member;
  - target new leader is already current leader;
  - team is not active.

## Application

- [x] Create `TransferirLiderazgoCommand` and response DTO/read model.
- [x] Create `TransferirLiderazgoCommandHandler` with business flow:
  - load active team by actor user id;
  - reject when no active team exists;
  - execute aggregate leadership transfer behavior;
  - persist changes;
  - return result for mobile UI.
- [x] Add validator for `NuevoLiderUserId`.
- [x] Add business exceptions for not-found/conflict scenarios:
  - no active team;
  - actor is not leader;
  - target is not member;
  - target is current leader;
  - team is not active.

## Infrastructure

- [x] Reuse or extend Team repository contracts for HU-06 checks:
  - `GetActiveByMemberUserIdAsync` or equivalent.
  - persistence/update support for leadership flag changes.
- [x] Implement repository persistence for leadership transfer in EF Core.
- [x] Ensure successful transfer updates `EsLider` flags without adding/removing membership rows.
- [x] Map persistence failures to consistent application exception handling.

## API

- [x] Add `PATCH /api/teams/leadership` endpoint in Team Service API.
- [x] Require authenticated participant access policy.
- [x] Map errors to HTTP statuses (`400`, `401`, `403`, `404`, `409`, `500`).
- [x] Return `200` with leadership transfer result payload.

## Contracts

- [x] Add HU-06 concrete endpoint section to `contracts/http/team-api.md`.
- [x] Document in `contracts/events/team-events.md` that no cross-service integration event is required for HU-06 closure, unless implementation introduces one.
- [x] Re-validate implemented payload/status against HU-06 contract definitions.

## Tests

- [x] Domain unit tests for leadership-transfer invariants.
- [x] Application unit tests for `TransferirLiderazgoCommandHandler` success and conflicts.
- [x] Integration tests for `PATCH /api/teams/leadership` (`200`, `400`, `401`, `403`, `404`, `409`).
- [x] Persistence regression test proving the former leader can leave through HU-07 after transferring leadership.
- [x] Contract tests for HU-06 endpoint response shape and status codes.

## Frontend (React Native mobile)

- [x] Add transfer-leadership action/screen state in participant mobile flow.
- [x] Add mobile API client method for `PATCH /api/teams/leadership`.
- [x] Allow selecting a new leader from existing team members excluding the current leader.
- [x] Handle success by showing the new leader and guidance to leave through HU-07 if desired.
- [x] Handle `404` no active team message.
- [x] Handle `409` actor not leader message.
- [x] Handle `409` invalid target member message.
- [x] Add mobile tests for happy path and main API errors.

## Acceptance and traceability

- [x] Update `docs/04-sdd/specs/HU-06-transferir-liderazgo-antes-de-salir-del-equipo/acceptance.md` with executed evidence after implementation.
- [x] Update HU-06 row in `docs/04-sdd/traceability-matrix.md` for SDD/contract readiness.
- [x] Align HU-06 status in `docs/04-sdd/SPECS-LIST.md` after implementation/testing.
- [x] Update HU-06 row in `docs/04-sdd/traceability-matrix.md` after implementation/testing.

## Hardening 10/10

- [x] Restore a reproducible backend test environment with the SDK required by `global.json` (`8.0.407`) or update the project SDK decision through an explicit repo-wide decision before verification.
- [x] Re-run and record fresh HU-06 backend evidence in `acceptance.md`:
  - `dotnet test services/team-service/tests/Umbral.TeamService.UnitTests/Umbral.TeamService.UnitTests.csproj --filter "FullyQualifiedName~TransferirLiderazgo"`;
  - `dotnet test services/team-service/tests/Umbral.TeamService.IntegrationTests/Umbral.TeamService.IntegrationTests.csproj --filter "FullyQualifiedName~Hu06EndpointsIntegrationTests"`;
  - `dotnet test services/team-service/tests/Umbral.TeamService.ContractTests/Umbral.TeamService.ContractTests.csproj --filter "FullyQualifiedName~Hu06ContractTests"`.
- [x] Restore mobile dependencies from `mobile/` and re-run HU-06 mobile evidence:
  - `npm run typecheck`;
  - `node --test tests/transferLeadershipFlow.test.js tests/TransferLeadershipScreenController.test.js`.
- [x] Add or record PostgreSQL/Npgsql runtime evidence for Team Service leadership transfer, proving the `EsLider` flag update persists outside EF Core InMemory tests.
- [x] Update `acceptance.md`, `docs/04-sdd/SPECS-LIST.md` and `docs/04-sdd/traceability-matrix.md` to mark HU-06 as `10/10` only after the fresh backend, mobile and PostgreSQL evidence is recorded.
