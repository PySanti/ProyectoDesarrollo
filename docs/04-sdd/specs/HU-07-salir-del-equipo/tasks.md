# HU-07 — Tasks

## Task status key

- [ ] Pending
- [x] Done

## Domain

- [x] Add/extend `Equipo` behavior to allow member exit while enforcing:
  - non-leader can leave directly;
  - leader with other members cannot leave directly;
  - leader who is the only member eliminates the team;
  - active team does not remain without leader.
- [x] Add domain exceptions or result values for:
  - participant is not a team member;
  - leader must transfer leadership first.

## Application

- [x] Create `SalirDeEquipoCommand` and response DTO/read model.
- [x] Create `SalirDeEquipoCommandHandler` with business flow:
  - load active team by actor user id;
  - reject when no active team exists;
  - execute aggregate exit behavior;
  - persist changes;
  - return result for mobile UI.
- [x] Add business exceptions for not-found/conflict scenarios:
  - no active team;
  - leader must transfer leadership before leaving.

## Infrastructure

- [x] Extend Team repository contracts for HU-07 checks:
  - `GetActiveByMemberUserIdAsync` or equivalent.
  - persistence/update support for member-row removal and team elimination.
- [x] Implement repository methods in EF Core persistence.
- [x] Ensure successful exit removes the `ParticipanteEquipo` row so the unique `usuarioid` index does not block future HU-04 joins.
- [x] Map persistence failures to consistent application exception handling.

## API

- [x] Add `DELETE /api/teams/membership` endpoint in Team Service API.
- [x] Require authenticated participant access policy.
- [x] Map errors to HTTP statuses (`401`, `403`, `404`, `409`, `500`).
- [x] Return `200` with exit result payload.

## Contracts

- [x] Add HU-07 concrete endpoint section to `contracts/http/team-api.md`.
- [x] Document in `contracts/events/team-events.md` that no cross-service integration event is required for HU-07 closure, unless implementation introduces one.
- [x] Re-validate implemented payload/status against HU-07 contract definitions.

## Tests

- [x] Domain unit tests for exit-team invariants.
- [x] Application unit tests for `SalirDeEquipoCommandHandler` success and conflicts.
- [x] Integration tests for `DELETE /api/teams/membership` (`200`, `401`, `403`, `404`, `409`).
- [x] Persistence regression test proving a user can join/create another team after leaving.
- [x] Contract tests for HU-07 endpoint response shape and status codes.

## Frontend (React Native mobile)

- [x] Add leave-team action/screen state in participant mobile flow.
- [x] Add mobile API client method for `DELETE /api/teams/membership`.
- [x] Handle success by showing no-team state or navigating back to team management.
- [x] Handle `404` no active team message.
- [x] Handle `409` leader must transfer leadership message.
- [x] Add mobile tests for happy path and main API errors.

## Hardening to 10/10

- [x] Make HU-07 mobile success explicitly show the participant has no active team or navigate back to team management in a no-team state.
- [x] Add mobile render-level test coverage for the `LeaveTeamScreenController` success path used by `LeaveTeamScreen`, not only the flow/model helpers.
- [x] Move direct domain-exception-to-HTTP mapping out of the API endpoint where practical, so the API layer maps application-level errors instead of domain exceptions directly.
- [x] Add an explicit active-team guard in `Equipo.Salir(...)` to reject exit behavior when the aggregate is not `Activo`.
- [x] Add domain/application tests for attempting to leave a non-active team.
- [x] Re-run HU-07 unit, integration, contract, mobile and typecheck checks after hardening.
- [x] Re-run or document the full Team Service integration suite status after resolving unrelated HU-04 PostgreSQL/concurrency failures.
- [x] Update `acceptance.md`, `SPECS-LIST.md` and `traceability-matrix.md` after all hardening tasks pass.

## Final review plan to 10/10

- [x] Correct the overdeclared mobile test task by adding render-level coverage for the `LeaveTeamScreenController` used by `LeaveTeamScreen`.
- [x] Add minimal render-level coverage with `react-test-renderer`, press `Salir de mi equipo`, mock a successful leave response and assert that `Sin equipo activo` is visible.
- [x] Verify the rendered success path also disables the leave action after the participant no longer has an active team.
- [x] Adjust the post-success button label to a short state label such as `Sin equipo activo` instead of reusing the long success message.
- [x] Update `design.md` to document the implemented hardening: non-active team guard, `EquipoNoActivoException`, `LeaveTeamConflictException`, application-level conflict mapping and mobile no-team success state.
- [x] Update `design.md` test plan to include non-active team domain/application tests and render-level mobile success coverage.
- [x] Re-run HU-07 focused verification after final review fixes: Team Service unit tests, HU-07 integration tests, Team Service contract tests, mobile tests and mobile typecheck.
- [x] Re-run or re-document the full Team Service integration suite status, keeping HU-04 failures clearly marked as unrelated if they still fail.
- [x] Update `acceptance.md` with the final review evidence, including whether mobile coverage is real render-level coverage or intentionally limited helper/model coverage.
- [x] Update `SPECS-LIST.md` and `traceability-matrix.md` only after the final review tasks pass.

## Acceptance and traceability

- [x] Update `docs/04-sdd/specs/HU-07-salir-del-equipo/acceptance.md` with executed evidence.
- [x] Update HU-07 row in `docs/04-sdd/traceability-matrix.md` for SDD/contract readiness.
- [x] Align HU-07 status in `docs/04-sdd/SPECS-LIST.md` for SDD/contract readiness.
- [x] Update HU-07 row in `docs/04-sdd/traceability-matrix.md` after implementation/testing.
- [x] Align HU-07 status in `docs/04-sdd/SPECS-LIST.md` after implementation/testing.
