# HU-34 - Acceptance

## Acceptance Checklist

- [x] Operator can create a BDT game from React web.
- [x] Backend persists the BDT game in state `Lobby`.
- [x] Backend persists one or more stages.
- [x] Backend rejects creation without stages.
- [x] Backend rejects stages without expected textual QR content.
- [x] Backend rejects stages without positive time limit.
- [x] Individual modality validates player limits.
- [x] Team modality validates team limits and minimum players per team.
- [x] Endpoint requires authenticated `Operador`.
- [x] Controller contains no business rules.
- [x] Contracts are updated after SDD review.
- [x] Traceability matrix is updated for implementation readiness.
- [x] SignalR publication/lobby updates are explicitly deferred to HU-42 or HU-55.
- [x] Duplicate stage order is rejected before persistence.
- [x] Null stage list returns a clean validation error.
- [x] React web supports adding/removing one or more stages and submits all stages through `etapas[]`.
- [x] HU-34 closure evidence is traced to `POST /api/bdt/games`; operator listing remains HU-37 scope.
- [x] Frontend documents runtime requirement `VITE_BDT_API_BASE_URL` for real API calls.

## Manual Verification Steps

1. Login in React web as `Operador`.
2. Open BDT creation screen.
3. Enter name, textual search area, modality, limits, start mode and at least one stage.
4. Submit the form.
5. Confirm success message and created game summary.
6. Confirm the success summary shows the created game and the number of configured stages.
7. Repeat with missing stages and confirm a clear validation error.
8. Repeat with a non-operator user and confirm access is rejected.
9. Configure `VITE_BDT_API_BASE_URL` with the BDT Game Service base URL before running the React web client against a real backend.

Manual runtime smoke with a real Keycloak token was not executed in this tool session because no real browser/Keycloak operator token was provided. The equivalent automated smoke coverage was executed through API integration tests using the test `Operador` authentication handler plus React web tests for the HU-34 form and payload.

## Automated Test Evidence

Evidence will be recorded after implementation:

| Test type | Command / evidence | Status |
|---|---|---|
| Domain unit | `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.UnitTests/Umbral.BdtGameService.UnitTests.csproj --no-restore` | Passed: 29/29 |
| Application | `CrearPartidaBdtCommandHandlerTests` included in BDT unit suite | Passed |
| API integration | `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.IntegrationTests/Umbral.BdtGameService.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~Hu34CreateBdtGameIntegrationTests"` | Passed: 11/11 |
| Full integration | `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.IntegrationTests/Umbral.BdtGameService.IntegrationTests.csproj --no-restore` | Passed: 28/28 |
| Contract | `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.ContractTests/Umbral.BdtGameService.ContractTests.csproj --no-restore` | Passed: 10/10 |
| PostgreSQL | `$env:BDT_POSTGRES_TEST_CONNECTION='Host=localhost;Port=5432;Database=umbral_bdt_game;Username=postgres;Password=postgres;'; dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.IntegrationTests/Umbral.BdtGameService.IntegrationTests.csproj --no-restore` | Passed: 28/28 with isolated schema factory |
| React web | `npm test -- --run` in `frontend/` | Passed: 19/19 |
| React web build | `npm run build` in `frontend/` | Passed |

## Traceability Status

| Field | Value |
|---|---|
| HU | HU-34 |
| Requirement | RF-25, RF-26, RF-27, RF-35, RF-36, RNF-01, RNF-02, RNF-04, RNF-06, RNF-13 |
| Owning service | BDT Game Service |
| Client | React web |
| Contract | `contracts/http/bdt-game-api.md` (`POST /api/bdt/games`), `contracts/events/bdt-game-events.md` (no integration event required) |
| Status | 10/10 / hardening completed / tested / PostgreSQL-verified / frontend multi-stage tested / acceptance updated |
