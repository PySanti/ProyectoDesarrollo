# HU-45 - Tasks

## Domain

- [x] Add or verify `TesoroQR` entity for upload attempts.
- [x] Add or verify `PartidaBDT.RegistrarTesoroQr(...)` for active-stage upload attempts.
- [x] Validate game state `Iniciada`.
- [x] Validate stage is active and still accepts attempts.
- [x] Validate participant/explorer is active in the BDT game.
- [x] Allow multiple attempts while stage remains active.
- [x] Ensure upload does not calculate numeric BDT score/ranking.

## Application

- [x] Add `SubirTesoroQrCommand`.
- [x] Add command validator for ids and uploaded image metadata.
- [x] Add application ports for image storage and QR decoding.
- [x] Add command handler that stores image, decodes QR content and persists `TesoroQR` attempt.
- [x] Persist unreadable QR attempts with `qrDecodificado = null` and processing status `NoLegible`.
- [x] Return upload response DTO matching the approved HTTP contract.

## Infrastructure

- [x] Add EF Core mapping for `TesoroQR` attempts.
- [x] Implement image storage adapter or service-local file/reference strategy baseline.
- [x] Implement QR decoder adapter baseline.
- [x] Add repository load/update path including active stage, explorer and treasure attempts.
- [x] Add PostgreSQL persistence coverage using isolated schema.

## API

- [x] Add `POST /api/bdt/games/{partidaId}/stages/{etapaId}/treasures` endpoint.
- [x] Enforce authenticated `Participante` authorization.
- [x] Accept `multipart/form-data` with `image` field.
- [x] Derive `ParticipanteUserId` from authenticated token claims only.
- [x] Return documented error status codes.
- [x] Keep endpoint free of business rules.

## Contracts

- [x] Update `contracts/http/bdt-game-api.md` with HU-45 multipart upload endpoint before implementation.
- [x] Update `contracts/events/bdt-game-events.md` with HU-45 no-RabbitMQ/no-new-SignalR decision before implementation.
- [x] Document accepted image MIME types (`image/jpeg`, `image/png`) and maximum file size (`5 MB`) before implementation.
- [x] Document `201 Created` response for recorded upload attempts before implementation.

## Tests

- [x] Add domain unit tests for active-stage upload rules and multiple attempts.
- [x] Add application handler tests with fake storage and fake QR decoder.
- [x] Add API integration tests for multipart success and documented errors.
- [x] Add HTTP contract tests.
- [x] Add PostgreSQL/Npgsql persistence tests.
- [x] Add mobile permission tests.
- [x] Add mobile upload flow tests.

## Mobile

- [x] Add camera/image-picker permission request with explanation baseline.
- [x] Add upload screen or modal reachable from HU-44 active-stage screen.
- [x] Add mobile API client for multipart upload endpoint.
- [x] Render selected/captured image summary.
- [x] Render loading, success and error states.
- [x] Block upload when camera/image permission or geolocation permission is denied.
- [x] Keep QR validation out of mobile.

## Hardening to 10/10

- [x] Add a real React Native/Expo camera/media-library image picker adapter for HU-45 runtime usage.
- [x] Add a real React Native/Expo geolocation permission adapter for HU-45 runtime usage.
- [x] Wire `BdtTreasureUploadScreen` to the runtime image-picker and geolocation adapters by default.
- [x] Keep injectable adapter boundaries for tests without relying on `globalThis.__umbralPickTreasureImage` in production flow.
- [x] Add mobile tests proving runtime adapters are wired and permission granted/denied paths work through the adapter boundary.
- [x] Add a production QR decoder adapter capable of decoding real JPEG/PNG QR images.
- [x] Keep deterministic/fake QR decoder only for tests or explicitly configured non-production environments.
- [x] Add QR decoder test with a real generated QR image fixture and unreadable-image fallback coverage.
- [x] Persist uploaded image bytes or retrievable artifact metadata in BDT-owned storage instead of only returning a synthetic reference.
- [x] Add integration/PostgreSQL evidence that `TesoroQR.ImagenReferencia` points to a retrievable stored artifact or metadata record.
- [x] Re-run BDT unit, integration, contract and HU-45 PostgreSQL tests after hardening.
- [x] Re-run mobile tests and mobile typecheck after hardening.
- [x] Update `acceptance.md` with 10/10 evidence after hardening.
- [x] Update `docs/04-sdd/traceability-matrix.md` and `docs/04-sdd/SPECS-LIST.md` after hardening.

## Acceptance and Traceability

- [x] Update `acceptance.md` with executed evidence after implementation.
- [x] Update `docs/04-sdd/traceability-matrix.md` after implementation status changes.
- [x] Update `docs/04-sdd/SPECS-LIST.md` after implementation status changes.
