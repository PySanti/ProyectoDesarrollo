# HU-45 - Acceptance

## Acceptance Checklist

- [x] Participant can take or select a QR treasure image from mobile using a real React Native/Expo runtime adapter.
- [x] Mobile explains and requests camera/image-picker permission in the baseline controller flow.
- [x] Mobile explains and requests camera/image-picker permission through the real runtime adapter.
- [x] Mobile blocks upload with clear message when permission is denied in tests.
- [x] Mobile blocks upload with clear message when permission is denied by the real runtime adapter.
- [x] Mobile blocks active upload when geolocation permission is denied in tests.
- [x] Mobile enables upload when geolocation permission is granted through the real runtime adapter.
- [x] Mobile sends multipart upload to documented BDT Game Service endpoint.
- [x] Backend accepts only `image/jpeg` and `image/png` images up to `5 MB`.
- [x] Backend returns `413` for images larger than `5 MB`.
- [x] Backend returns `415` for unsupported image media types.
- [x] Backend records every allowed treasure upload attempt.
- [x] Backend attempts QR decoding through an infrastructure adapter baseline.
- [x] Backend decodes QR content from a real JPEG/PNG QR image through a production adapter.
- [x] Backend persists a retrievable uploaded image artifact/reference for later BDT supervision and traceability.
- [x] Backend rejects uploads outside initiated BDT active stage.
- [x] Backend rejects unregistered/inactive participants.
- [x] Multiple attempts are allowed while stage remains active.
- [x] Mobile shows loading, success and error states.
- [x] Mobile does not perform authoritative QR validation.
- [x] Contracts are documented before implementation.
- [x] Traceability matrix is updated after implementation.

## Planning Readiness Evidence

| Item | Status |
|---|---|
| HTTP contract documented | Completed: `POST /api/bdt/games/{partidaId}/stages/{etapaId}/treasures` |
| Event contract decision documented | Completed: no RabbitMQ and no new SignalR/WebSocket message required for HU-45 closure |
| Accepted MIME types | Completed: `image/jpeg`, `image/png` |
| Maximum image size | Completed: `5 MB` |
| Implementation blocker status | No planning blocker remains |

## Manual Verification Steps

1. Login in mobile as `Participante`.
2. Open an active BDT stage from HU-44.
3. Tap “subir tesoro”.
4. Grant camera/image permission and capture or select an image.
5. Submit the image.
6. Confirm the app shows upload success or a clear processing error.
7. Submit another image while the stage remains active and confirm another attempt is accepted.
8. Deny permission and confirm upload is blocked with a clear message.
9. Try upload after stage close and confirm backend rejects it.

## Automated Test Evidence

| Test type | Command / evidence | Status |
|---|---|---|
| Domain unit | `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.UnitTests/Umbral.BdtGameService.UnitTests.csproj` | Passed: 71/71 |
| Application | `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.UnitTests/Umbral.BdtGameService.UnitTests.csproj` | Passed: handler storage, decoder, decoded/unreadable attempts, conflicts |
| API integration | `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.IntegrationTests/Umbral.BdtGameService.IntegrationTests.csproj --filter "FullyQualifiedName~Hu45"` | Passed: 13/13 |
| Contract | `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.ContractTests/Umbral.BdtGameService.ContractTests.csproj --filter "FullyQualifiedName~Hu45"` | Passed: 7/7 |
| PostgreSQL | `dotnet test services/bdt-game-service/tests/Umbral.BdtGameService.IntegrationTests/Umbral.BdtGameService.IntegrationTests.csproj --filter "FullyQualifiedName~Hu45"` | Passed: `Hu45PostgresUploadTreasureTests` with retrievable stored artifact evidence |
| Mobile | `npm test` in `mobile/` | Passed: 74/74 |
| Mobile typecheck | `npm run typecheck` in `mobile/` | Passed |

## Hardening Evidence for 10/10

| Area | Evidence | Status |
|---|---|---|
| Real mobile image picker | `requestBdtTreasureImagePermission` and `pickBdtTreasureImage` use Expo ImagePicker; tests cover granted, denied, unavailable, library selection, camera source and cancellation. | Completed |
| Real mobile geolocation permission | `BdtTreasureUploadScreen` injects the Expo geolocation adapter already verified by HU-44/HU-45 mobile tests. | Completed |
| Real QR image decoding | `ZxingQrImageDecoder` decodes a QRCoder-generated PNG QR fixture and returns `null` for unreadable image bytes. | Completed |
| Retrievable image storage | `LocalTesoroQrImageStorage` writes uploaded bytes to service-local storage and PostgreSQL evidence confirms `ImagenReferencia` points to an existing artifact. | Completed |
| Final verification | BDT unit 71/71, HU-45 integration 13/13, HU-45 contract 7/7, mobile 74/74 and mobile typecheck passed. | Completed |

## Traceability Status

| Field | Value |
|---|---|
| HU | HU-45 |
| Requirement | RF-13, RF-28, RF-29, RF-30, RF-31, RF-35, RF-36, RNF-01, RNF-02, RNF-03, RNF-04, RNF-06, RNF-13, RNF-16, RNF-17, RNF-18, RNF-19, RNF-20 |
| Owning service | BDT Game Service |
| Client | React Native mobile |
| Contract | `contracts/http/bdt-game-api.md`, `contracts/events/bdt-game-events.md` documented for HU-45 |
| Status | 10/10 / hardening completed / tested / native mobile adapters verified / real QR decoder verified / retrievable image storage verified / acceptance updated |
