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
- [x] Operator can upload a QR image per stage and have BDT Game Service decode the expected textual QR content.

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
10. Upload a readable QR image in a stage and confirm the expected QR text field is filled from the backend decode response.

Manual runtime smoke with a real Keycloak token was not executed in this tool session because no real browser/Keycloak operator token was provided. The equivalent automated smoke coverage was executed through API integration tests using the test `Operador` authentication handler plus React web tests for the HU-34 form and payload.

## Automated Test Evidence

Current evidence:

| Test type | Command / evidence | Status |
|---|---|---|
| Domain unit | `dotnet test "tests/Umbral.BdtGameService.UnitTests/Umbral.BdtGameService.UnitTests.csproj"` in `services/bdt-game-service` | Passed: 74/74 |
| Application | `CrearPartidaBdtCommandHandlerTests` included in BDT unit suite | Passed |
| QR decode application | `DecodificarQrEsperadoBdtCommandHandlerTests` included in BDT unit suite | Passed: readable, unreadable and validator coverage |
| API integration | `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.IntegrationTests/Umbral.BdtGameService.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~Hu34CreateBdtGameIntegrationTests"` | Passed: 11/11 |
| Full integration | `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.IntegrationTests/Umbral.BdtGameService.IntegrationTests.csproj --no-restore` | Passed: 28/28 |
| Contract | `dotnet test "tests/Umbral.BdtGameService.ContractTests/Umbral.BdtGameService.ContractTests.csproj"` in `services/bdt-game-service` | Passed: 43/43 |
| QR decode contract | `Hu34ExpectedQrDecodeContractTests` included in BDT contract suite | Passed: 5/5 |
| PostgreSQL | `$env:BDT_POSTGRES_TEST_CONNECTION='Host=localhost;Port=5432;Database=umbral_bdt_game;Username=postgres;Password=postgres;'; dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.IntegrationTests/Umbral.BdtGameService.IntegrationTests.csproj --no-restore` | Passed: 28/28 with isolated schema factory |
| React web | `npm test -- --run` in `frontend/` | Passed: 48/48 |
| React web focused | `npm test -- --run CreateBdtGamePage bdtApi` in `frontend/` | Passed: 14/14 |
| React web TypeScript | `npx tsc --noEmit` in `frontend/` | Passed; `npm run typecheck` is not defined in `frontend/package.json` |
| React web build | `npm run build` in `frontend/` | Passed |

## Traceability Status

| Field | Value |
|---|---|
| HU | HU-34 |
| Requirement | RF-25, RF-26, RF-27, RF-35, RF-36, RNF-01, RNF-02, RNF-04, RNF-06, RNF-13 |
| Owning service | BDT Game Service |
| Client | React web |
| Contract | `contracts/http/bdt-game-api.md` (`POST /api/bdt/games`, `POST /api/bdt/stages/expected-qr/decode`), `contracts/events/bdt-game-events.md` (no integration event required) |
| Status | 10/10 / hardening completed / tested / PostgreSQL-verified / frontend multi-stage and QR-image decode tested / acceptance updated |
