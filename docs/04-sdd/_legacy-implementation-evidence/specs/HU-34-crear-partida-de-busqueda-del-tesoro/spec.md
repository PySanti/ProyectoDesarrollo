# HU-34 - Crear partida de Busqueda del Tesoro

## User Story

Como **Operador**, quiero **crear una partida de Busqueda del Tesoro, anadir etapas, tesoro por etapa y temporizador de cada etapa**, para **preparar la dinamica de busqueda**.

## Source References

- HU: `HU-34` in `docs/01-project-source/srs.md` and `docs/01-project-source/historias-de-usuario.md`.
- RF: `RF-25`, `RF-26`, `RF-27`, `RF-35`, `RF-36`.
- RB: `RB-B01`, `RB-B02`, `RB-B03`, `RB-B04`, `RB-B05`, `RB-B06`, `RB-B07`, `RB-B09`, `RB-B10`, `RB-B11`, `RB-B12`, `RB-B48`.
- RNF: `RNF-01`, `RNF-02`, `RNF-04`, `RNF-06`, `RNF-13`.
- Ownership: `docs/04-sdd/SPECS-LIST.md`, `docs/03-microservices/service-ownership.md`.
- Service context: `services/bdt-game-service/service-context.md`, `docs/03-microservices/services/bdt-game-service.md`.
- Web context: `frontend/frontend-context.md`.
- Contracts: `contracts/http/bdt-game-api.md`, `contracts/events/bdt-game-events.md`.

## Actor

- `Operador` using React web.

## User Goal

Allow an operator to create a valid BDT game with textual search area, modality, participation limits, start mode and one or more valid stages so the game can be exposed as a lobby for later participant registration.

## Scope

Included:

- React web operator form for BDT creation.
- Backend command in BDT Game Service to create `PartidaBDT`.
- Required fields: name, textual search area, modality, minimum participants, maximum participants or teams according to modality, minimum players per team when modality is `Equipo`, start mode and stages.
- Stage fields: order, QR image uploaded by the operator and decoded by BDT Game Service into expected textual QR content, plus time limit in seconds.
- Validation that at least one valid stage exists.
- Persist the game and stages in BDT Game Service PostgreSQL storage.
- Create the game in state `Lobby` for first-delivery publication semantics.
- Return created game summary to the web client.

Out of scope:

- Participant listing of BDT games; covered by HU-10 and HU-12.
- Operator list of published BDT games; covered by HU-37.
- Joining individual BDT games; covered by HU-39.
- Team BDT joining and leadership validation; covered by HU-40 and HU-14.
- Starting the BDT game; covered by HU-43.
- Participant QR image upload and participant QR validation; covered by HU-45 and HU-46.
- Geolocation, clues, ranking, active-stage gameplay and SignalR lobby/publication updates.

## Preconditions

- The user is authenticated with base role `Operador`.
- BDT Game Service is available.
- The web client submits data using the documented HTTP contract.
- Only Trivia and BDT are valid game modes; this SDD creates only BDT.

## Postconditions

- A new `PartidaBDT` exists in BDT Game Service persistence.
- The game has state `Lobby` and can be listed as published by HU-37 and participant listing flows.
- Each persisted stage has expected textual QR content decoded from the operator-uploaded QR image and a positive time limit.
- No participant is registered by this command.
- No BDT ranking, geolocation, clue or QR validation state is created.

## Business Rules

- `RB-B01`: only an operator can create BDT games.
- `RB-B02`: a BDT game can be individual or team-based.
- `RB-B03`: the operator defines game name, textual search area, modality and participation limits.
- `RB-B04`: in individual modality, the maximum limit represents players.
- `RB-B05`: in team modality, the maximum limit represents teams.
- `RB-B06`: in team modality, the operator defines minimum players per team.
- `RB-B07`: stages are defined during BDT creation for this HU.
- `RB-B09`: every stage has a time limit.
- `RB-B10`: a BDT cannot be published without at least one valid stage.
- `RB-B11`: a BDT stage cannot be published without expected QR and time limit.
- `RB-B48`: search area is textual; no coordinate, polygon or geofence validation is performed.

## Related Requirements

- `RF-25`
- `RF-26`
- `RF-27`
- `RF-35`
- `RF-36`
- `RNF-01`
- `RNF-02`
- `RNF-04`
- `RNF-06`
- `RNF-13`

## Acceptance Criteria

1. An authenticated operator can create a BDT game from React web.
2. The request requires name, textual search area, modality, participation limits, start mode and at least one stage.
3. The backend rejects creation without stages.
4. The web form rejects any stage without a QR image decoded by BDT Game Service into expected textual QR content.
5. The backend rejects any stage without a positive time limit.
6. In individual modality, the maximum participant limit is interpreted as maximum players.
7. In team modality, the maximum participant limit is interpreted as maximum teams and minimum players per team is required.
8. The created game is persisted in BDT Game Service storage with state `Lobby`.
9. The response returns game id, name, modality, state, search area, start mode and stage count.
10. The endpoint rejects unauthenticated users with `401`.
11. The endpoint rejects authenticated non-operator users with `403`.
12. The controller does not contain business rules; validation is handled in application/domain layers.

## Assumptions

- For first delivery, successful HU-34 creation publishes the game as `Lobby` because no separate BDT draft/publish HU is active.
- `Publicada` is represented by BDT state `Lobby`, consistent with HU-10 and HU-12.
- Expected QR is stored as textual content, but the operator obtains it by uploading a QR image for backend decoding instead of typing the string manually.
- RF-13 publication/lobby real-time updates are deferred to HU-42 or HU-55 and do not block HU-34 implementation readiness.

## Open Questions

- None blocking for SDD review.
