# HU-44 - Ver etapa activa y opcion de subir tesoro

## User Story

Como **Participante**, quiero **ver la etapa activa y la opcion de subir tesoro**, para **participar en una BDT iniciada desde la aplicacion movil**.

## Source References

- HU: `HU-44` in `docs/01-project-source/srs.md` and `docs/01-project-source/historias-de-usuario.md`.
- RF: `RF-13`, `RF-14`, `RF-27`, `RF-28`, `RF-31`, `RF-32`, `RF-34`, `RF-35`, `RF-36`.
- RB: `RB-09`, `RB-16`, `RB-25`, `RB-33`, `RB-B19`, `RB-B20`, `RB-B30`, `RB-B31`, `RB-B32`, `RB-B33`, `RB-B49`.
- RNF: `RNF-01`, `RNF-03`, `RNF-04`, `RNF-06`, `RNF-13`, `RNF-17`, `RNF-19`, `RNF-20`.
- Ownership: `docs/04-sdd/SPECS-LIST.md`, `docs/03-microservices/service-ownership.md`.
- Service context: `services/bdt-game-service/service-context.md`, `docs/03-microservices/services/bdt-game-service.md`.
- Mobile context: `mobile/mobile-context.md`, `docs/02-project-context/mobile-participant-context.md`.
- Contracts: `contracts/http/bdt-game-api.md`, `contracts/events/bdt-game-events.md`.

## Actor

- `Participante` using React Native mobile.

## User Goal

Show the active BDT stage, countdown/timer information and upload-treasure action to an authenticated participant who is registered in an initiated BDT game.

## Scope

Included:

- React Native active-stage screen for BDT participants.
- Backend read model in BDT Game Service for the participant active-stage view.
- Validation that the participant is registered/active in the selected BDT game.
- Validation that the game is `Iniciada` and has an active stage.
- Return active-stage data, timer timestamps and `puedeSubirTesoro` flag.
- Mobile timer display based on backend timestamps.
- Mobile action/button to navigate to HU-45 upload flow.
- Mobile geolocation permission gate before enabling active BDT participation, because geolocation is mandatory for active BDT participation.
- Real-time refresh path using the documented `PartidaBDTIniciada` message from HU-43. Stage close/advance notifications are prepared as an extension point but remain owned by HU-47 until documented.

Out of scope:

- Starting the BDT game; covered by HU-43.
- Uploading or capturing the QR treasure image; covered by HU-45.
- QR decoding and validation; covered by HU-45/HU-46.
- Closing stages and advancing to the next stage; covered by HU-47.
- Sending clues; covered by HU-49.
- Operator geolocation map; covered by HU-52 when active.
- Historical route tracking or advanced geospatial analytics.

## Preconditions

- User is authenticated with base role `Participante`.
- Target BDT game exists and is `Iniciada`.
- Participant is registered/active for that BDT game.
- At least one BDT stage is active.
- Mobile app can request geolocation permission before enabling BDT active participation.

## Postconditions

- No BDT state is changed by the active-stage query.
- Participant sees active stage, timer and upload action if allowed.
- If geolocation permission is denied, mobile blocks active participation and shows a clear message.
- Upload action routes to HU-45 and does not validate QR itself.
- If the game has no active stage, the backend returns a business conflict (`409`) instead of a synthetic `200` response.

## Business Rules

- `RB-09`: a game in `Iniciada` allows game actions such as uploading treasures.
- `RB-B19`: after BDT starts, the first stage is active.
- `RB-B20`: during an initiated BDT, mobile must expose the upload treasure option.
- `RB-B49`: geolocation is mandatory for active BDT participation.
- Backend remains authoritative for game state, active stage and upload availability.
- Mobile may validate permissions for usability but must not duplicate domain rules.
- The active-stage endpoint returns `200 OK` only when an active stage is available to the participant. Missing active stage is a business conflict (`409`).

## Related Requirements

- `RF-13`
- `RF-14`
- `RF-27`
- `RF-28`
- `RF-31`
- `RF-32`
- `RF-34`
- `RF-35`
- `RF-36`
- `RNF-01`
- `RNF-03`
- `RNF-04`
- `RNF-06`
- `RNF-13`
- `RNF-17`
- `RNF-19`
- `RNF-20`

## Acceptance Criteria

1. An authenticated participant registered in an initiated BDT can view the active stage from mobile.
2. The backend rejects missing games with `404`.
3. The backend rejects non-registered participants with `403` because they are not authorized to view that participant game state.
4. The backend rejects games that are not `Iniciada` with `409`.
5. The backend rejects initiated games without active stage with `409`.
6. The response includes active stage order, time limit and backend timer timestamps.
7. The mobile screen renders stage data and countdown based on backend timestamps.
8. The mobile screen shows a “subir tesoro” action only when `puedeSubirTesoro` is true.
9. The mobile app requests geolocation permission before enabling active BDT participation.
10. If geolocation permission is denied, the mobile app blocks active participation with a clear message.
11. The endpoint rejects unauthenticated users with `401` and non-participants with `403`.
12. The mobile screen refreshes active-stage state when it receives the documented `PartidaBDTIniciada` real-time message.
13. The mobile app does not decode or validate QR content in HU-44.

## Assumptions

- BDT Game Service can determine participant registration from its own `ExploradorBDT`/BDT registration state.
- Location streaming and operator map are outside HU-44; HU-44 only gates active participation on permission as required by the SRS.
- The upload button navigates to HU-45 and remains disabled when backend says uploads are not allowed.
- Stage close/advance real-time messages are owned by HU-47. HU-44 prepares the mobile refresh/invalidation path and uses the already documented HU-43 `PartidaBDTIniciada` message for start-to-active-stage refresh.

## Open Questions

- None blocking for SDD review.
