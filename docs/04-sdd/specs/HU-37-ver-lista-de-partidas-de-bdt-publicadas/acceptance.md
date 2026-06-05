# HU-37 - Acceptance

## Acceptance Checklist

- [x] Operator can view published BDT games from React web.
- [x] Each listed game shows name and state.
- [x] Response includes modality, textual search area and stage count when available.
- [x] Operator can open a read-only summary modal from a listed row without requiring a HU-38 full-detail endpoint.
- [x] Query returns an empty list without error when no BDT games are published.
- [x] Endpoint requires authenticated `Operador`.
- [x] Query does not mutate state.
- [x] Contracts are updated after SDD review.
- [x] Traceability matrix is updated for implementation readiness.
- [x] No SignalR behavior is required for HU-37 closure.

## Manual Verification Steps

1. Login in React web as `Operador`.
2. Open the BDT published games list.
3. Confirm existing published BDT games are shown.
4. Confirm each row includes at least name and state.
5. Confirm an empty state appears when there are no published BDT games.
6. Login as a non-operator and confirm access is rejected.

## Automated Test Evidence

Evidence will be recorded after implementation:

| Test type | Command / evidence | Status |
|---|---|---|
| Application | `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.UnitTests/Umbral.BdtGameService.UnitTests.csproj --no-restore --filter "FullyQualifiedName~ListarPartidasBdtPublicadasOperadorHandlerTests"` | Passed: 2/2 |
| API integration | `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.IntegrationTests/Umbral.BdtGameService.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~Hu37OperatorPublishedGamesIntegrationTests"` | Passed: 8/8 |
| Contract | `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.ContractTests/Umbral.BdtGameService.ContractTests.csproj --no-restore --filter "FullyQualifiedName~Hu37ContractTests"` | Passed: 3/3 |
| PostgreSQL | `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.IntegrationTests/Umbral.BdtGameService.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~Hu37PostgresOperatorPublishedGamesTests"` after creating local `umbral_bdt_game_test` database with `PGPASSWORD=postgres createdb -h localhost -p 5432 -U postgres umbral_bdt_game_test` | Passed: 1/1 with isolated schema factory |
| Full BDT unit suite | `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.UnitTests/Umbral.BdtGameService.UnitTests.csproj --no-restore` | Passed: 31/31 |
| Full BDT integration suite | `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.IntegrationTests/Umbral.BdtGameService.IntegrationTests.csproj --no-restore` | Passed: 37/37 |
| Full BDT contract suite | `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.ContractTests/Umbral.BdtGameService.ContractTests.csproj --no-restore` | Passed: 13/13 |
| React web focused | `npm test --prefix frontend -- PublishedBdtGamesPage` | Passed: 9/9 including row summary modal |
| Full React web | `npm test --prefix frontend` | Passed: 45/45 |
| React web typecheck | `tsc -p tsconfig.json --noEmit` in `frontend/` | Passed |
| React web build | `npm run build --prefix frontend` | Passed |
| React web build | `npm run build` in `frontend/` | Passed |

## Runtime Smoke Evidence

- Automated React web tests cover the HU-37 operator route, API client token forwarding, loading, empty, error, `401`, `403` and accessible table states.
- Automated React web tests cover the row summary modal using existing list data only; no HU-38 endpoint is introduced.
- Manual smoke with real Keycloak `Operador` token against a running BDT Game Service remains environment-dependent because this workspace does not provide a running Keycloak realm/client and BDT service deployment configured with real tokens.
- Backend authorization is verified through integration/contract tests using the test authentication scheme, including unauthenticated, non-operator and malformed/missing `sub` claim cases.

## Traceability Status

| Field | Value |
|---|---|
| HU | HU-37 |
| Requirement | RF-25, RF-27, RF-35, RNF-01, RNF-02, RNF-04, RNF-06, RNF-13 |
| Owning service | BDT Game Service |
| Client | React web |
| Contract | `contracts/http/bdt-game-api.md` (`GET /api/bdt/operator/games/published`), `contracts/events/bdt-game-events.md` (no integration event required) |
| Status | 10/10 / hardening completed / tested / PostgreSQL-verified / frontend-tested / acceptance updated |
