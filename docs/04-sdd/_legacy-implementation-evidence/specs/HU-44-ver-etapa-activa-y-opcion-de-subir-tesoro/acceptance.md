# HU-44 - Acceptance

## Acceptance Checklist

- [x] Participant can view active BDT stage from React Native mobile.
- [x] Backend returns active-stage data only for registered/active participants.
- [x] Backend rejects non-initiated games.
- [x] Backend rejects games without active stage with `409`.
- [x] Mobile renders stage order, timer and upload action.
- [x] Upload action is shown only when backend allows treasure upload.
- [x] Mobile requests/checks geolocation permission before enabling active participation.
- [x] Mobile blocks active participation with a clear message when geolocation permission is denied.
- [x] Mobile geolocation uses a React Native-compatible permission adapter and works in the target mobile runtime.
- [x] Mobile countdown updates over time while the active-stage screen remains mounted.
- [x] The integrated mobile screen wires `Subir tesoro` to HU-45 navigation or an approved HU-45 handoff placeholder.
- [x] Endpoint requires authenticated `Participante`.
- [x] Query does not mutate BDT state.
- [x] Documented `PartidaBDTIniciada` real-time message refreshes the active-stage screen.
- [x] Mobile does not subscribe to undocumented real-time event names.
- [x] Contracts are updated before implementation.
- [x] Traceability matrix is updated after hardening implementation.

## Manual Verification Steps

1. Login in mobile as `Participante`.
2. Join a BDT and wait until it is started.
3. Open the active-stage screen.
4. Confirm stage order, timer and “subir tesoro” action are visible.
5. Deny geolocation permission and confirm participation is blocked with a clear message.
6. Allow geolocation permission and confirm the upload action is enabled when backend permits it.
7. Simulate receiving `PartidaBDTIniciada` for the current partida and confirm the screen refreshes active-stage data.

## Automated Test Evidence

| Test type | Command / evidence | Status |
|---|---|---|
| Application | `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.UnitTests/Umbral.BdtGameService.UnitTests.csproj` | Passed: 71/71 |
| API integration | `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.IntegrationTests/Umbral.BdtGameService.IntegrationTests.csproj --filter "FullyQualifiedName~Hu44"` | Passed: 11/11 |
| Contract | `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.ContractTests/Umbral.BdtGameService.ContractTests.csproj --filter "FullyQualifiedName~Hu44"` | Passed: 6/6 |
| PostgreSQL | `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.IntegrationTests/Umbral.BdtGameService.IntegrationTests.csproj --filter "FullyQualifiedName~Hu44"` | Passed: `Hu44PostgresActiveStageTests` with isolated schema |
| Mobile | `npm test` in `mobile/` | Passed: 69/69 |
| Mobile typecheck | `npm run typecheck` in `mobile/` | Passed |
| Real-time | `npm test` in `mobile/` | Passed: refresh on documented `PartidaBDTIniciada` only |

## Hardening Evidence for 10/10

| Area | Evidence |
|---|---|
| React Native geolocation runtime | `requestBdtGeolocationPermission` uses Expo Location and has granted, denied and unavailable adapter tests; no `navigator.geolocation` fallback remains. |
| Live timer | `BdtActiveStageScreenController updates countdown while mounted` proves countdown decreases and disables upload when time reaches zero. |
| Upload action integration | `BdtActiveStageScreenController calls HU-45 navigation callback from upload action` and `buildBdtTreasureUploadParams maps HU-44 active stage to HU-45 route params` prove the HU-45 handoff data. |
| Team modality boundary | `design.md` explicitly defers team-modality active-stage access to HU-40 until team registration/membership mapping exists. |

## Traceability Status

| Field | Value |
|---|---|
| HU | HU-44 |
| Requirement | RF-13, RF-14, RF-27, RF-28, RF-31, RF-32, RF-34, RF-35, RF-36, RNF-01, RNF-03, RNF-04, RNF-06, RNF-13, RNF-17, RNF-19, RNF-20 |
| Owning service | BDT Game Service |
| Client | React Native mobile |
| Contract | Implemented and contract-tested: `contracts/http/bdt-game-api.md`, `contracts/events/bdt-game-events.md` |
| Status | 10/10 / hardening completed / tested / mobile geolocation adapter verified / live countdown verified / HU-45 navigation handoff verified / acceptance updated |
