# HU-07 — Acceptance

## Acceptance checklist

- [x] HU-07 is assigned to `Team Service`.
- [x] HU-07 is implemented for `React Native mobile` participant flow.
- [x] A non-leader participant can leave their active team directly.
- [x] A leader with other members cannot leave directly and must transfer leadership first.
- [x] A leader who is the only member can leave and the team is marked as `Eliminado`.
- [x] Leaving a team does not delete or modify historical game participation records.
- [x] Required tests exist (domain, application, integration, contract, mobile frontend).
- [x] HU-07 contract section is documented in `contracts/http/team-api.md`.
- [x] Traceability row for HU-07 is updated.

## Manual verification steps

1. Authenticate as participant in the mobile app.
2. Join or create a team where the participant is not leader.
3. Trigger leave-team action.
4. Verify the participant no longer belongs to the team and the app shows no-team state.
5. Authenticate as participant who is leader with at least one other member.
6. Trigger leave-team action.
7. Verify the operation is rejected with a message that leadership must be transferred first.
8. Authenticate as participant who is leader and the only team member.
9. Trigger leave-team action.
10. Verify the team is marked as eliminated and the participant no longer has an active team.
11. Attempt leave-team action without an active team.
12. Verify the operation is rejected without modifying state.

## Automated test evidence

Commands to execute after implementation:

- `dotnet test services/team-service/tests/Umbral.TeamService.UnitTests/Umbral.TeamService.UnitTests.csproj`
- `dotnet test services/team-service/tests/Umbral.TeamService.IntegrationTests/Umbral.TeamService.IntegrationTests.csproj`
- `dotnet test services/team-service/tests/Umbral.TeamService.ContractTests/Umbral.TeamService.ContractTests.csproj`
- `npm run typecheck` from `mobile/`
- `npm test` from `mobile/`

Latest execution summary:

- `dotnet test services/team-service/tests/Umbral.TeamService.UnitTests/Umbral.TeamService.UnitTests.csproj` passed: 30/30.
- `dotnet test services/team-service/tests/Umbral.TeamService.IntegrationTests/Umbral.TeamService.IntegrationTests.csproj --filter Hu07` passed: 7/7.
- `dotnet test services/team-service/tests/Umbral.TeamService.ContractTests/Umbral.TeamService.ContractTests.csproj` passed: 8/8.
- `npm test` from `mobile/` passed: 23/23, including render-level `LeaveTeamScreenController` coverage used by `LeaveTeamScreen` for the successful leave path.
- `npm run typecheck` from `mobile/` passed.
- Full Team Service integration suite passed 18/20 and still has unrelated HU-04 failures: PostgreSQL authentication failed for `umbral_user` in PostgreSQL-backed concurrency tests, and HU-04 InMemory concurrency expects PostgreSQL advisory-lock behavior.
- HU-07 hardening completed: mobile success now explicitly shows `Sin equipo activo`, the post-success button label is short, the leave action is disabled after success, HU-07 API maps application-level conflicts instead of domain exceptions, and `Equipo.Salir(...)` rejects non-active teams.

## Readiness evidence

- SDD files exist: `spec.md`, `design.md`, `tasks.md`, `acceptance.md`.
- HTTP contract is documented in `contracts/http/team-api.md` as `DELETE /api/teams/membership`.
- Event contract states no cross-service integration event is required for HU-07 closure.
- Traceability row is updated to reflect HU-07 completion, hardening and test evidence.

## Traceability status

- HU-07 row is completed in `docs/04-sdd/traceability-matrix.md`.

## Assumptions

- Team Service is the source of truth for active team membership and leadership.
- HU-07 authorization relies on authenticated token claims and participant policy in Team Service.
- Deleting the team when the only leader leaves is logical deletion via `EstadoEquipo.Eliminado`, not physical deletion.
- Successful exit removes the `ParticipanteEquipo` row because the current persistence model uses a unique index on `usuarioid` and has no inactive-membership flag.
