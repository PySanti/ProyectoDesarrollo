# HU-03 — Acceptance

## Acceptance checklist

- [x] HU-03 is assigned to `Team Service`.
- [x] HU-03 is implemented for `React Native mobile` participant flow.
- [x] Team creation is allowed only when participant does not belong to another active team.
- [x] Created team includes exactly one initial member (the creator).
- [x] Creator is marked as team leader.
- [x] Team access code is generated and unique.
- [x] Team starts in active status.
- [x] Team cardinality decision `1..5` is respected.
- [x] Duplicate membership attempt is rejected with business conflict (`409`).
- [x] Required tests exist (domain, application, integration, contract, mobile frontend).
- [x] HU-03 contract section is documented in `contracts/http/team-api.md`.
- [x] Traceability row for HU-03 is updated.
- [x] Mobile app shell exists with real Keycloak login and reusable navigation base.

## Manual verification steps

1. Authenticate as participant in mobile app.
2. Open create-team screen.
3. Submit valid team name while participant has no active team.
4. Verify success response shows team info with generated access code.
5. Verify creator appears as first member and leader.
6. Attempt team creation again with same participant.
7. Verify operation is rejected with business conflict (`409`).
8. Confirm no team exceeds cardinality rules during creation.

## Automated test evidence

- Domain unit tests:
  - Paths:
    - `services/team-service/tests/Umbral.TeamService.UnitTests/CrearEquipoDomainTests.cs`
  - Executed command:
    - `dotnet test services/team-service/tests/Umbral.TeamService.UnitTests/Umbral.TeamService.UnitTests.csproj`
  - Result summary:
    - Passed.

- Application/handler tests:
  - Paths:
    - `services/team-service/tests/Umbral.TeamService.UnitTests/CrearEquipoHandlerTests.cs`
    - `services/team-service/tests/Umbral.TeamService.UnitTests/CrearEquipoValidatorTests.cs`
  - Executed command:
    - `dotnet test services/team-service/tests/Umbral.TeamService.UnitTests/Umbral.TeamService.UnitTests.csproj`
  - Result summary:
    - Passed.

- Integration tests:
  - Paths:
    - `services/team-service/tests/Umbral.TeamService.IntegrationTests/Hu03EndpointsIntegrationTests.cs`
  - Executed command:
    - `dotnet test services/team-service/tests/Umbral.TeamService.IntegrationTests/Umbral.TeamService.IntegrationTests.csproj`
  - Result summary:
    - Passed (`201`, `400`, `401`, `403`, `409`).

- Contract tests:
  - Paths:
    - `services/team-service/tests/Umbral.TeamService.ContractTests/Hu03ContractTests.cs`
  - Executed command:
    - `dotnet test services/team-service/tests/Umbral.TeamService.ContractTests/Umbral.TeamService.ContractTests.csproj`
  - Result summary:
    - Passed.

- Mobile frontend tests:
  - Paths:
    - `mobile/tests/createTeamFlow.test.js`
    - `mobile/tests/tokenClaims.test.js`
  - Executed command:
    - `node --test mobile/tests/*.test.js`
  - Result summary:
    - Passed.

- Mobile reusable shell + auth implementation:
  - Paths:
    - `mobile/App.tsx`
    - `mobile/src/navigation/RootNavigator.tsx`
    - `mobile/src/auth/AuthProvider.tsx`
    - `mobile/src/auth/keycloakMobileAuth.ts`
    - `mobile/src/features/teams/CreateTeamScreenContainer.tsx`
    - `mobile/src/screens/LoginScreen.tsx`
    - `mobile/src/screens/HomeScreen.tsx`
  - Result summary:
    - Implemented and integrated for authenticated HU-03 flow.

- Runtime API smoke verification:
  - Executed command:
    - `dotnet run --project services/team-service/src/Umbral.TeamService.Api/Umbral.TeamService.Api.csproj --urls http://127.0.0.1:5099` + `POST /api/teams` without token
  - Result summary:
    - Service starts and endpoint enforces auth in runtime (`401` unauthenticated without bearer token).

- PostgreSQL-backed runtime verification:
  - Executed commands:
    - `docker compose up -d postgres` (from `infra/`)
    - `ASPNETCORE_URLS=http://127.0.0.1:5099 ConnectionStrings__TeamDatabase='Host=127.0.0.1;Port=55432;Database=umbral_team;Username=umbral;Password=16102005' dotnet run --project services/team-service/src/Umbral.TeamService.Api/Umbral.TeamService.Api.csproj`
    - `POST /api/teams` without token against runtime API
    - `docker compose stop postgres`
  - Result summary:
    - Team Service starts with Npgsql connection.
    - Schema-creation strategy executes at startup (`EnsureCreated`).
    - Runtime request returns `401` (no auth token), without falling back to InMemory persistence.

- Fresh backend test evidence after HU-03 follow-up closure:
  - Executed command:
    - `dotnet test services/team-service/tests/Umbral.TeamService.UnitTests/Umbral.TeamService.UnitTests.csproj && dotnet test services/team-service/tests/Umbral.TeamService.IntegrationTests/Umbral.TeamService.IntegrationTests.csproj && dotnet test services/team-service/tests/Umbral.TeamService.ContractTests/Umbral.TeamService.ContractTests.csproj`
  - Result summary:
    - Passed (Unit: 9, Integration: 5, Contract: 2).

- Fresh mobile test evidence after HU-03 follow-up closure:
  - Executed command:
    - `node --test mobile/tests/createTeamFlow.test.js`
  - Result summary:
    - Passed (4/4).

- Hardening evidence for concurrency and retry behavior:
  - Paths:
    - `services/team-service/src/Umbral.TeamService.Infrastructure/Persistence/TeamDbContext.cs`
    - `services/team-service/src/Umbral.TeamService.Infrastructure/Persistence/EquipoRepository.cs`
    - `services/team-service/src/Umbral.TeamService.Application/Teams/CreateTeam/CrearEquipoCommandHandler.cs`
    - `services/team-service/tests/Umbral.TeamService.UnitTests/CrearEquipoHandlerTests.cs`
  - Result summary:
    - Unique constraints now protect `usuarioid` and `codigoacceso` at persistence level.
    - `23505` unique violations are mapped to business conflicts/retry flow.
    - Handler retries access-code collisions and maps concurrent duplicate membership to `409` conflict behavior.

- Contract-strengthening evidence:
  - Path:
    - `services/team-service/tests/Umbral.TeamService.ContractTests/Hu03ContractTests.cs`
  - Result summary:
    - Contract test now asserts `integrantes` length is exactly `1` on successful team creation.

## Traceability status

- HU-03 row exists in `docs/04-sdd/traceability-matrix.md` with implemented contracts and tests.
- HU-03 status reflects completion and testing with PostgreSQL-backed runtime verification executed.

## Assumptions

- Team creation uses Team Service as source of truth for membership and leadership.
- HU-03 authorization relies on authenticated token claims and participant policy in Team Service (no mandatory Identity Service runtime query for this HU).
- `EquipoCreado` for HU-03 is emitted through the Team Service application event port with a no-op infrastructure publisher; RabbitMQ publication is not required for HU-03 closure.
