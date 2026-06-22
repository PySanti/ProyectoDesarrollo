# HU-06 — Acceptance

## Acceptance checklist

- [x] HU-06 is assigned to `Team Service`.
- [x] HU-06 is implemented for `React Native mobile` participant leader flow.
- [x] Current leader can transfer leadership to another member of the same active team.
- [x] Former leader becomes a regular member.
- [x] New leader becomes the only leader.
- [x] Team remains active and membership cardinality remains unchanged.
- [x] Non-leader cannot transfer leadership.
- [x] Transfer to a non-member is rejected.
- [x] Transfer to the same current leader is rejected.
- [x] Required tests exist (domain, application, integration, contract, mobile frontend).
- [x] HU-06 contract section is documented in `contracts/http/team-api.md`.
- [x] Traceability row for HU-06 is updated.

## Manual verification steps

1. Authenticate as participant leader in the mobile app.
2. Ensure the team has at least one other member.
3. Open transfer leadership flow.
4. Select another member as new leader.
5. Confirm the transfer.
6. Verify the selected member is shown as the new leader.
7. Verify the previous leader remains a member but is no longer leader.
8. Attempt transfer as a non-leader member.
9. Verify the operation is rejected with a leadership/authorization business message.
10. Attempt transfer to a user who is not a member when possible through API test setup.
11. Verify the operation is rejected without modifying team state.
12. After successful transfer, execute HU-07 as the former leader and verify they can leave as non-leader.

## Automated test evidence

Commands to execute after implementation:

- `dotnet test services/team-service/tests/Umbral.TeamService.UnitTests/Umbral.TeamService.UnitTests.csproj`
- `dotnet test services/team-service/tests/Umbral.TeamService.IntegrationTests/Umbral.TeamService.IntegrationTests.csproj --filter Hu06`
- `dotnet test services/team-service/tests/Umbral.TeamService.ContractTests/Umbral.TeamService.ContractTests.csproj --filter Hu06`
- `npm run typecheck` from `mobile/`
- `npm test` from `mobile/`

Latest execution summary:

- `dotnet test services/team-service/tests/Umbral.TeamService.UnitTests/Umbral.TeamService.UnitTests.csproj --filter "FullyQualifiedName~TransferirLiderazgo"` — Passed: 11/11.
- `dotnet test services/team-service/tests/Umbral.TeamService.IntegrationTests/Umbral.TeamService.IntegrationTests.csproj --filter "FullyQualifiedName~Hu06EndpointsIntegrationTests"` — Passed: 9/9.
- `dotnet test services/team-service/tests/Umbral.TeamService.ContractTests/Umbral.TeamService.ContractTests.csproj --filter "FullyQualifiedName~Hu06ContractTests"` — Passed: 3/3.
- `TEAM_POSTGRES_TEST_CONNECTION='Host=localhost;Port=5432;Database=umbral_team;Username=postgres;Password=postgres;' dotnet test services/team-service/tests/Umbral.TeamService.IntegrationTests/Umbral.TeamService.IntegrationTests.csproj --filter "FullyQualifiedName~Hu06PostgresLeadershipTransferTests" --no-restore` — Passed: 1/1; verified leadership transfer persists with Npgsql/PostgreSQL and exactly one `EsLider` remains.
- Merge verification on 2026-06-04: `dotnet test services/team-service/tests/Umbral.TeamService.IntegrationTests/Umbral.TeamService.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~Postgres"` passed 2/2 using compose PostgreSQL on port `55432` and an isolated schema; the default factory no longer drops `public`.
- `npm run typecheck` from `mobile/` — Passed.
- `node --test tests/transferLeadershipFlow.test.js tests/TransferLeadershipScreenController.test.js` from `mobile/` — Passed: 6/6.
- `global.json` uses SDK `8.0.407` with `rollForward: latestMajor`, allowing reproducible verification on the installed SDK while preserving the project target framework.

## Readiness evidence

- SDD files exist: `spec.md`, `design.md`, `tasks.md`, `acceptance.md`.
- HTTP contract documented: `PATCH /api/teams/leadership` in `contracts/http/team-api.md`.
- Event-contract note documented: `contracts/events/team-events.md` states no cross-service integration event is required for HU-06 closure unless implementation introduces one.
- Traceability row documents SDD and contract readiness.
- Mobile API client, screen/controller state, route and tests are implemented against `PATCH /api/teams/leadership`.
- PostgreSQL/Npgsql runtime evidence exists for the leadership transfer persistence path.

## Traceability status

- HU-06 appears in `docs/04-sdd/SPECS-LIST.md` as `10/10` with backend, mobile and PostgreSQL verification.
- HU-06 appears in `docs/04-sdd/traceability-matrix.md` with backend, contract, mobile and PostgreSQL/Npgsql evidence complete.

## Assumptions

- Team Service is the source of truth for active team membership and leadership.
- HU-06 authorization relies on authenticated token claims and participant policy in Team Service.
- Leadership transfer is a domain operation inside the existing `Equipo` aggregate.
- HU-06 does not remove the former leader from the team; leaving is performed by HU-07.
