# HU-39 - Acceptance

## Acceptance Checklist

- [x] Participant can join a published individual BDT from mobile.
- [x] Successful join persists the individual registration.
- [x] Successful join returns waiting screen data.
- [x] Mobile shows the waiting screen after success.
- [x] Backend rejects non-existing game with `404`.
- [x] Backend rejects non-lobby game with `409`.
- [x] Backend rejects team-modality game through the individual endpoint with `409`.
- [x] Backend rejects duplicate registration with `409`.
- [x] Backend rejects full capacity with `409`.
- [x] Backend preserves full-capacity invariant under concurrent PostgreSQL joins by different participants.
- [x] Endpoint requires authenticated `Participante`.
- [x] Team Service is not consulted or mutated for individual join.
- [x] Contracts are updated after SDD review.
- [x] Traceability matrix is updated with final 10/10 concurrency-hardening evidence.
- [x] SignalR lobby updates are explicitly deferred to HU-42 or HU-55.
- [x] Individual registration model is fixed as `ExploradorBDT` with competitor type `Usuario`.

## Manual Verification Steps

1. Login in mobile as `Participante`.
2. Open BDT panel and select a published individual BDT.
3. Tap join.
4. Confirm the app shows a waiting screen after success.
5. Try joining the same BDT again and confirm duplicate registration is rejected.
6. Try joining a team BDT through this action and confirm it is rejected.
7. Try joining with a non-participant token and confirm access is rejected.

## Automated Test Evidence

| Test type | Command / evidence | Status |
|---|---|---|
| Domain unit | `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.UnitTests/Umbral.BdtGameService.UnitTests.csproj --no-restore` | Passed: 40/40 |
| Application | `UnirseABdtIndividualCommandHandlerTests` included in BDT unit suite | Passed |
| API integration | `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.IntegrationTests/Umbral.BdtGameService.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~Hu39JoinIndividualBdtIntegrationTests"` | Passed: 12/12 |
| Contract | `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.ContractTests/Umbral.BdtGameService.ContractTests.csproj --no-restore --filter "FullyQualifiedName~Hu39ContractTests"` | Passed: 6/6 |
| PostgreSQL | `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.IntegrationTests/Umbral.BdtGameService.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~Hu39PostgresJoinIndividualBdtTests"` | Passed: 4/4 |
| PostgreSQL concurrent capacity | `Hu39PostgresJoinIndividualBdtTests.PostIndividualInscription_Should_Not_Exceed_Capacity_With_Concurrent_Npgsql_Requests` | Passed: exactly one `200`, one `409`, one persisted `ExploradorBDT` |
| Backend full unit suite | `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.UnitTests/Umbral.BdtGameService.UnitTests.csproj --no-restore` | Passed: 40/40 |
| Backend full integration suite | `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.IntegrationTests/Umbral.BdtGameService.IntegrationTests.csproj --no-restore` | Passed: 53/53 |
| Backend full contract suite | `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.ContractTests/Umbral.BdtGameService.ContractTests.csproj --no-restore` | Passed: 19/19 |
| Mobile | `npm test` in `mobile/` | Passed: 47/47 |
| Mobile typecheck | `npm run typecheck` in `mobile/` | Passed |

## Traceability Status

| Field | Value |
|---|---|
| HU | HU-39 |
| Requirement | RF-05, RF-06, RF-11, RF-13, RF-27, RF-35, RF-36, RNF-01, RNF-02, RNF-04, RNF-06, RNF-13, RNF-20 |
| Owning service | BDT Game Service |
| Client | React Native mobile |
| Contract | `contracts/http/bdt-game-api.md` (`POST /api/bdt/games/{partidaId}/individual-inscriptions`), `contracts/events/bdt-game-events.md` (no integration event required) |
| Status | 10/10 / implemented / tested / backend-verified / PostgreSQL-concurrency-verified / mobile-tested / acceptance updated |
