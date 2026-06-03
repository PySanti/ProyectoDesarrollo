# HU-39 - Unirse a BDT individual

## User Story

Como **Participante**, quiero **unirme a una BDT individual publicada**, para **jugar por mi cuenta**.

## Source References

- HU: `HU-39` in `docs/01-project-source/srs.md` and `docs/01-project-source/historias-de-usuario.md`.
- RF: `RF-05`, `RF-06`, `RF-11`, `RF-13`, `RF-27`, `RF-35`, `RF-36`.
- RB: `RB-18`, `RB-20`, `RB-23`, `RB-B12`, `RB-B13`, `RB-B14`.
- RNF: `RNF-01`, `RNF-02`, `RNF-04`, `RNF-06`, `RNF-13`, `RNF-20`.
- Ownership: `docs/04-sdd/SPECS-LIST.md`, `docs/03-microservices/service-ownership.md`.
- Service context: `services/bdt-game-service/service-context.md`, `docs/03-microservices/services/bdt-game-service.md`.
- Mobile context: `mobile/mobile-context.md`, `docs/02-project-context/mobile-participant-context.md`.
- Contract base: `contracts/http/bdt-game-api.md`, `contracts/events/bdt-game-events.md`.

## Actor

- `Participante` using React Native mobile.

## User Goal

Allow an authenticated participant to register individually in a published individual BDT game while it is in lobby and has available capacity, then show a waiting/lobby screen.

## Scope

Included:

- React Native action to join an individual BDT game from the published BDT list.
- Backend command in BDT Game Service to register the participant as an individual `ExploradorBDT` lobby entry.
- Validate game exists, is BDT, is in `Lobby`, has modality `Individual`, and has available player capacity.
- Prevent duplicate individual registration by the same participant in the same game.
- Return waiting screen data to the mobile client.
- Return mobile waiting screen data through the synchronous HTTP response.

Out of scope:

- Joining BDT by team; covered by HU-40.
- Warning for non-leader in team BDT; covered by HU-14.
- Accepting/rejecting convocatorias; not part of individual BDT join.
- Starting BDT; covered by HU-43.
- Active stage, QR upload, geolocation and treasure validation; covered by HU-44, HU-45 and HU-46.
- Operator participant lobby view; covered by HU-42.
- SignalR lobby updates; deferred to HU-42 or HU-55.

## Preconditions

- User is authenticated with base role `Participante`.
- Target BDT game exists in BDT Game Service.
- Target BDT game is published as state `Lobby`.
- Target BDT game has modality `Individual`.
- Target BDT game has available individual capacity.

## Postconditions

- The participant has one individual `ExploradorBDT` active lobby registration for the selected game.
- The participant sees a waiting screen in the mobile app.
- The BDT game remains in `Lobby` until HU-43 starts it.
- No team registration, team validation or convocation is created.

## Business Rules

- `RB-B13`: any player can attempt to enter a published BDT.
- `RB-B14`: in individual BDT, any player can register while the game is in lobby and has capacity.
- `RB-18`: participants can play individual games even if they belong to a team.
- `RB-20`: in individual games, the operator-defined maximum is players.
- `RB-23`: a game cannot start unless minimums are met; HU-39 only registers and does not start the game.
- `RF-36`: backend validates business rules before accepting registration.
- Mobile must not decide authoritative capacity or state; it consumes backend result.

## Related Requirements

- `RF-05`
- `RF-06`
- `RF-11`
- `RF-13`
- `RF-27`
- `RF-35`
- `RF-36`
- `RNF-01`
- `RNF-02`
- `RNF-04`
- `RNF-06`
- `RNF-13`
- `RNF-20`

## Acceptance Criteria

1. An authenticated participant can select a published individual BDT game and join it.
2. The backend rejects joining a non-existing game with `404`.
3. The backend rejects joining a BDT game that is not in `Lobby` with `409`.
4. The backend rejects joining a team-modality BDT through the individual endpoint with `409`.
5. The backend rejects joining when the individual player capacity is full with `409`.
6. The backend rejects duplicate registration by the same participant with `409`.
7. Successful registration persists the participant inscription/lobby entry.
8. Successful registration returns waiting screen data to mobile.
9. The mobile app navigates to or renders the BDT waiting screen after success.
10. The endpoint rejects unauthenticated users with `401`.
11. The endpoint rejects authenticated non-participant users with `403`.
12. The operation does not consult or mutate Team Service data.

## Assumptions

- `Publicada` maps to BDT state `Lobby` for first delivery.
- BDT Game Service owns BDT lobby registrations for BDT execution through `ExploradorBDT`.
- Individual BDT join does not require Team Service validation, even if the participant belongs to a team.
- RF-13 lobby real-time updates are deferred to HU-42 or HU-55 and do not block HU-39 implementation readiness.

## Open Questions

- None blocking for SDD review.
