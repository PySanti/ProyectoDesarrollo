# HU-45 - Design

## Owning Service

- BDT Game Service.

## Supporting Services

- Identity Service / Keycloak token claims for authenticated role `Participante`.
- PostgreSQL through EF Core inside BDT Game Service.
- React Native mobile as participant client.
- React Native/Expo camera or image-picker permission APIs wired through a mobile permission adapter.
- React Native/Expo geolocation permission API wired through a mobile permission adapter.
- Production QR decoder adapter inside BDT Game Service infrastructure that can decode real JPEG/PNG QR images.
- File/object storage adapter inside BDT Game Service infrastructure that stores a retrievable uploaded image artifact/reference.

No Team Service call is required in HU-45. Participant/team eligibility must be derived from BDT Game Service registration state.

## Domain Model

Entities and value objects involved:

- `PartidaBDT` aggregate root.
- `EtapaBDT` active child entity.
- `ExploradorBDT` participant/team competitor.
- `TesoroQR` upload attempt entity.
- `CodigoQREsperado` remains protected by backend and is not sent to mobile.
- `ResultadoValidacionQR` is finalized by HU-46; HU-45 stores upload/decoding status and may persist `qrDecodificado = null` when no QR can be read.
- `EstadoPartida.Iniciada` and `EstadoEtapa.Activa`.

Domain invariants:

- Treasure upload is accepted only for initiated BDT games.
- Treasure upload is accepted only for the active stage.
- Uploading is rejected if the stage is closed or already resolved.
- Participant/explorer must be registered/active in the BDT game.
- Multiple attempts are allowed while the stage remains active.
- Mobile-decoded QR content is never authoritative.

## Commands

### `SubirTesoroQrCommand`

Input:

- `PartidaId`.
- `EtapaId` from active-stage screen.
- `ParticipanteUserId` from authenticated token claims.
- Uploaded image stream/file metadata.

Output:

- Treasure upload attempt DTO.

Handler responsibilities:

- Validate game, active stage and participant registration.
- Validate stage still accepts attempts.
- Store image/reference through an infrastructure port.
- Invoke QR decoder adapter to attempt decoding.
- Persist `TesoroQR` attempt with decoded QR content if available.
- Return upload result to mobile.
- Do not close stage or update BDT ranking in HU-45.

## Queries

- None required for upload.
- Active-stage data is provided by HU-44.

## HTTP Contract

Endpoint documented in `contracts/http/bdt-game-api.md`:

```txt
POST /api/bdt/games/{partidaId}/stages/{etapaId}/treasures
```

Authorization:

- Authenticated `Participante`.

Request:

- `multipart/form-data`.
- Field `image`: QR treasure image captured or selected from mobile.
- Accepted media types: `image/jpeg`, `image/png`.
- Maximum image size: `5 MB`.

Response `201 Created` when the upload attempt is recorded:

```json
{
  "tesoroId": "uuid",
  "partidaId": "uuid",
  "etapaId": "uuid",
  "exploradorId": "uuid",
  "fechaEnvioUtc": "2026-01-01T00:03:00Z",
  "estadoProcesamiento": "Decodificado | NoLegible",
  "qrDecodificado": "QR-ETAPA-1",
  "mensaje": "Tesoro recibido para validacion."
}
```

Errors:

| Status | Reason |
|---|---|
| 400 | Invalid `partidaId`, invalid `etapaId`, missing image or invalid file metadata |
| 401 | Unauthenticated |
| 403 | Authenticated user is not participant or has invalid `sub` claim |
| 404 | BDT game or stage not found |
| 409 | Game is not `Iniciada`, stage is not active, stage is closed or attempts are no longer allowed |
| 413 | Image exceeds documented size limit |
| 415 | Unsupported image media type |
| 500 | Persistence, storage or decoder infrastructure failure |

## Events

RabbitMQ integration events:

- None required for HU-45 closure unless a future asynchronous QR-validation pipeline is approved.

Domain/history event candidate inside BDT Game Service:

- `TesoroQRSubido` with `tesoroId`, `partidaId`, `etapaId`, `exploradorId`, `imagenReferencia`, `occurredOnUtc`.

User-visible real-time update:

- Optional `TesoroQRSubido` or upload-received update to the participant/operator if HU-51 or HU-55 later approves the real-time contract.
- HU-45 mobile can rely on synchronous HTTP response for closure.

## Real-Time Updates

- Not required for HU-45 closure unless implementation chooses to notify upload processing asynchronously.
- QR validation result real-time updates belong to HU-46/HU-47 if introduced.
- No new SignalR/WebSocket message is required for HU-45 readiness.

## Design Patterns Applied

| Pattern | Location | Problem solved | Justification |
|---|---|---|---|
| CQRS + Mediator | `SubirTesoroQrCommand` and handler | Encapsulates state-changing upload use case | Required project architecture |
| Aggregate Method | `PartidaBDT.RegistrarTesoroQr(...)` | Protects active-stage and participant eligibility rules | BDT upload rules belong to the aggregate/domain |
| Adapter / Port | QR decoder and image storage ports | Isolates device/image infrastructure from domain/application | Clean/hexagonal architecture requirement |
| Repository | BDT application port and EF Core implementation | Persists `TesoroQR` attempt | Required for traceability and history |
| Template Method / Pipeline candidate | Upload -> store -> decode -> persist | Keeps upload processing steps explicit and testable | Useful if decoder/storage variants grow |

## Hardening Plan for 10/10

The current baseline proves the HTTP, domain, persistence and mobile controller flow, but the feature is not 10/10 until the following runtime gaps are closed:

| Area | Current gap | Required 10/10 change | Verification |
|---|---|---|---|
| Mobile image selection | The screen relies on injected/test picker behavior and the default permission path denies runtime image access. | Add a real React Native/Expo image picker adapter that requests camera/media-library permission, allows capture or selection and maps assets to the HU-45 upload DTO. | Mobile controller tests with adapter fakes, plus a render/navigation test proving `BdtTreasureUploadScreen` wires the adapter by default. |
| Mobile geolocation | The default implementation uses web-style `navigator.geolocation`, which is not a reliable React Native permission adapter. | Add a real React Native/Expo geolocation permission adapter and inject it into HU-45 upload screen. | Mobile tests for granted/denied paths using the adapter boundary and typecheck/build evidence. |
| Backend QR decoding | The deterministic decoder only reads UTF-8 fixtures starting with `QR:`. | Add a production QR decoder adapter capable of decoding QR content from real JPEG/PNG images while keeping deterministic fakes for tests. | Infrastructure/unit or integration test using a generated QR image fixture and preserving unreadable-image behavior. |
| Image storage | The current service-local storage returns a reference without persisting bytes. | Persist the uploaded image artifact in service-local storage/object storage/database-backed file storage and keep `TesoroQR.ImagenReferencia` retrievable. | Integration/PostgreSQL test asserting the persisted reference points to a retrievable stored artifact or stored metadata. |
| Acceptance evidence | Acceptance currently records baseline automated tests only. | Update acceptance after hardening with native adapter tests, real-image QR decoder test and retrievable-storage test. | `acceptance.md`, `SPECS-LIST.md` and traceability matrix updated after verification. |

## Tests Required

Unit tests:

- Upload attempt succeeds for registered participant and active stage.
- Upload attempt fails for non-initiated game.
- Upload attempt fails for inactive/closed stage.
- Upload attempt fails for unregistered participant.
- Multiple attempts are allowed while stage remains active.

Application tests:

- Handler calls storage and decoder ports.
- Handler persists `TesoroQR` attempt with decoded content when decoder succeeds.
- Handler records unreadable QR attempts with `qrDecodificado = null` and `estadoProcesamiento = NoLegible`.
- Handler maps business conflicts and infrastructure failures correctly.

Integration/API tests:

- Multipart upload returns documented success response.
- Returns documented `400`, `401`, `403`, `404`, `409`, `413` and `415` cases where applicable.
- Rejects image files larger than `5 MB` with `413`.
- Rejects media types other than `image/jpeg` and `image/png` with `415`.
- Database stores upload attempt with participant, game, stage and timestamp.

Contract tests:

- Multipart request and response shape match `contracts/http/bdt-game-api.md` once updated.

Mobile tests:

- Camera/image-picker permission accepted path.
- Permission denied path.
- Real mobile image picker adapter maps selected/captured assets to multipart upload input.
- Real mobile geolocation permission adapter enables upload on granted permission and blocks on denied permission.
- `BdtTreasureUploadScreen` wires runtime adapters by default, not only injectable test hooks.
- Upload sends image to documented endpoint with bearer token.
- Loading, success and error states render clearly.
- Mobile does not decode or validate QR authoritatively.

PostgreSQL tests:

- `TesoroQR` attempt persists with Npgsql using isolated schema.
- Stored image reference is stable and points to a retrievable artifact/metadata.

Infrastructure tests:

- Production QR decoder decodes a real JPEG/PNG QR fixture.
- Production QR decoder preserves `NoLegible` behavior for images without readable QR content.
