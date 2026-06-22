# HU-45 - Subir foto del tesoro QR

## User Story

Como **Participante**, quiero **tomar o subir una foto del tesoro QR**, para **intentar validar la etapa activa**.

## Source References

- HU: `HU-45` in `docs/01-project-source/srs.md` and `docs/01-project-source/historias-de-usuario.md`.
- RF: `RF-13`, `RF-28`, `RF-29`, `RF-30`, `RF-31`, `RF-35`, `RF-36`.
- RB: `RB-09`, `RB-10`, `RB-11`, `RB-15`, `RB-16`, `RB-B20`, `RB-B21`, `RB-B22`, `RB-B26`, `RB-B44`, `RB-B49`.
- RNF: `RNF-01`, `RNF-02`, `RNF-03`, `RNF-04`, `RNF-06`, `RNF-13`, `RNF-16`, `RNF-17`, `RNF-18`, `RNF-19`, `RNF-20`.
- Ownership: `docs/04-sdd/SPECS-LIST.md`, `docs/03-microservices/service-ownership.md`.
- Service context: `services/bdt-game-service/service-context.md`, `docs/03-microservices/services/bdt-game-service.md`.
- Mobile context: `mobile/mobile-context.md`, `docs/02-project-context/mobile-participant-context.md`.
- Contracts: `contracts/http/bdt-game-api.md`, `contracts/events/bdt-game-events.md`.

## Actor

- `Participante` using React Native mobile.

## User Goal

Allow a participant to capture or select a QR treasure image during an active BDT stage and submit it to BDT Game Service for backend processing.

## Scope

Included:

- React Native camera/image-picker flow for BDT treasure upload.
- Mobile permission explanation and handling for camera/image library.
- Real React Native/Expo image picker and camera permission adapter wired into the screen, not only test injection hooks.
- Real React Native/Expo geolocation permission adapter wired into the screen so active BDT participation can be enabled when permission is granted.
- Backend upload endpoint in BDT Game Service accepting the selected image and active-stage context.
- Backend accepts only `image/jpeg` and `image/png` uploads, with a maximum file size of `5 MB`.
- Backend validation that the participant is registered/active, the game is `Iniciada`, the stage is active and treasure attempts are still allowed.
- Persist a `TesoroQR` attempt with participant/explorer, game, stage, retrievable submitted image reference, timestamp and processing status.
- Backend invokes a QR decoder adapter capable of decoding real JPEG/PNG QR images to attempt extracting textual QR content from the image.
- Return upload/processing result to mobile.
- Allow multiple attempts while the stage remains active and no correct QR has closed the stage.

Out of scope:

- Final comparison of decoded QR content against expected QR and marking valid/invalid; covered by HU-46.
- Closing the stage after a valid QR or timeout; covered by HU-47.
- Operator view of uploaded treasures; covered by HU-51 when active.
- Sending clues; covered by HU-49.
- Mobile active-stage screen before upload; covered by HU-44.
- Mobile-side authoritative QR decoding or validation.
- Push notifications outside the app.

## Preconditions

- User is authenticated with base role `Participante`.
- Target BDT game exists and is `Iniciada`.
- Participant is registered/active for the BDT game.
- Target stage is currently active.
- Mobile camera or image-picker permission is granted.
- Geolocation permission is granted for active BDT participation.
- Mobile runtime has a native image picker/camera adapter and native geolocation permission adapter configured.

## Postconditions

- A `TesoroQR` upload attempt is recorded by BDT Game Service.
- The attempt is associated with the active BDT game, active stage and participant/explorer.
- The backend has attempted QR decoding or has recorded that the image could not be processed/read.
- The backend has stored or otherwise made retrievable the uploaded image reference for later BDT supervision/traceability.
- The mobile app receives a clear result for the upload attempt.
- The stage is not closed by HU-45 unless HU-46/HU-47 are implemented as part of a later integrated workflow.

## Business Rules

- `RB-B20`: active BDT participants can access “subir tesoro”.
- `RB-B21`: uploading treasure means taking or selecting a photo that contains the supposed QR.
- `RB-B22`: backend processes the uploaded image and decodes QR content.
- `RB-B26`: every uploaded treasure is registered with player/team, game, stage, date/time and result/processing data.
- `RB-B44`: multiple attempts are allowed during the same active stage until correct validation or stage close.
- `RB-B49`: geolocation is mandatory for active BDT participation.
- Mobile must not be authoritative for QR decoding or validation.
- BDT Game Service owns QR upload and processing; no service may access another service database.

## Related Requirements

- `RF-13`
- `RF-28`
- `RF-29`
- `RF-30`
- `RF-31`
- `RF-35`
- `RF-36`
- `RNF-01`
- `RNF-02`
- `RNF-03`
- `RNF-04`
- `RNF-06`
- `RNF-13`
- `RNF-16`
- `RNF-17`
- `RNF-18`
- `RNF-19`
- `RNF-20`

## Acceptance Criteria

1. Participant can take or select a QR treasure image from mobile.
2. Mobile explains and requests required camera/image-picker permission.
3. Mobile blocks upload with a clear message when permission is denied.
4. Mobile submits the image to a documented BDT Game Service endpoint.
5. Backend rejects unsupported image types with `415` and images larger than `5 MB` with `413`.
6. Backend rejects missing/non-existing games with `404`.
7. Backend rejects games not in `Iniciada` with `409`.
8. Backend rejects inactive/closed stages with `409`.
9. Backend rejects unregistered participants with `403` because they are not authorized to upload treasures for that BDT game.
10. Backend records each upload attempt while attempts are allowed.
11. Backend attempts QR decoding through an infrastructure adapter.
12. Backend records accepted upload attempts even when no QR can be read; unreadable QR content is stored as `qrDecodificado = null` for later validation/reporting.
13. Multiple attempts are accepted while the stage remains active and not solved.
14. Mobile shows loading, success and error states for upload.
15. No QR validation result is treated as authoritative by the mobile app.
16. Mobile runtime can request real camera/image-picker permission and select or capture an image without relying on test globals.
17. Mobile runtime can request real geolocation permission and enable upload when permission is granted.
18. Backend QR decoder can process real JPEG/PNG QR images, not only deterministic text fixtures.
19. Backend image storage adapter persists a retrievable image artifact/reference for future operator supervision and traceability.

## Assumptions

- HU-45 stores and decodes the uploaded image, but final QR comparison and stage resolution are assigned to HU-46/HU-47.
- The final 10/10 implementation may store the image in local service storage, object storage or database-backed metadata, but the domain must persist a stable retrievable image reference and never rely on mobile-only state.
- The approved upload contract accepts `image/jpeg` and `image/png` only, with a maximum file size of `5 MB`.
- A deterministic/test QR decoder may remain for tests, but production DI must use a real QR decoder adapter for JPEG/PNG images.

## Open Questions

- None blocking for SDD review.
