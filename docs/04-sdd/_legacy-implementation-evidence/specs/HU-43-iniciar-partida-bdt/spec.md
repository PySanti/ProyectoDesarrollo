# HU-43 - Iniciar partida BDT

## User Story

Como **Operador**, quiero **iniciar una partida BDT**, para **comenzar la busqueda cuando existan participantes suficientes**.

## Source References

- HU: `HU-43` in `docs/01-project-source/srs.md` and `docs/01-project-source/historias-de-usuario.md`.
- RF: `RF-04`, `RF-11`, `RF-13`, `RF-27`, `RF-31`, `RF-32`, `RF-35`, `RF-36`.
- RB: `RB-07`, `RB-08`, `RB-09`, `RB-12`, `RB-23`, `RB-26`, `RB-28`, `RB-B19`.
- RNF: `RNF-01`, `RNF-02`, `RNF-03`, `RNF-04`, `RNF-06`, `RNF-13`, `RNF-17`.
- Ownership: `docs/04-sdd/SPECS-LIST.md`, `docs/03-microservices/service-ownership.md`.
- Service context: `services/bdt-game-service/service-context.md`, `docs/03-microservices/services/bdt-game-service.md`.
- Web context: `frontend/frontend-context.md`.
- Contracts: `contracts/http/bdt-game-api.md`, `contracts/events/bdt-game-events.md`.

## Actor

- `Operador` using React web.

## User Goal

Allow an authenticated operator to start a published BDT game from lobby when configured participation minimums are met, changing the game to `Iniciada` and activating the first stage.

## Scope

Included:

- React web operator action to start a BDT game from an operator-visible lobby or list.
- Backend command in BDT Game Service to start a BDT game.
- Validation that the target game exists, is in `Lobby`, has at least one valid stage and satisfies configured minimum participation.
- Validation of start mode for a manual operator start: allowed for `Manual` and `ManualYAutomatico`; rejected for strictly `Automatico` when triggered by an operator HTTP action.
- State transition from `Lobby` to `Iniciada`.
- Activation of the first BDT stage with server timestamps for timer calculation.
- Synchronous HTTP response with started game and active-stage summary.
- User-visible real-time update for game state and active stage after backend state is persisted.
- Authorization of SignalR partida subscriptions so only an operator or a participant registered/active in the BDT game can receive `PartidaBDTIniciada` for that game.

Out of scope:

- Creating BDT games and stages; covered by HU-34.
- Listing operator-published BDT games; covered by HU-37.
- Joining individual BDT games; covered by HU-39.
- Joining team BDT games and team convocatoria rules; covered by HU-40/HU-41 when implemented.
- Operator lobby participant monitoring; covered by HU-42.
- Participant active-stage screen; covered by HU-44.
- Uploading QR treasure images; covered by HU-45.
- QR validation and stage closing; covered by HU-46 and HU-47.
- Implementing a background scheduler for strictly automatic start. This SDD defines the manual operator start path and the domain rule that strictly automatic games are not manually started.

## Preconditions

- User is authenticated with base role `Operador`.
- Target BDT game exists in BDT Game Service.
- Target BDT game is in `Lobby`.
- Target BDT game has at least one configured stage.
- BDT Game Service has persisted lobby registrations from HU-39/HU-40 flows.

## Postconditions

- The BDT game state becomes `Iniciada`.
- The first stage becomes active.
- Active-stage timer timestamps are based on backend/server time.
- No new registration is created by this command.
- No QR treasure is uploaded or validated by this command.
- A real-time game-started/stage-activated update is published only after persistence succeeds.
- If the post-commit SignalR/WebSocket dispatch fails, the persisted `Iniciada` state remains authoritative, the HTTP response remains successful, the failure is logged, and clients can recover the current state through the active-stage HTTP query owned by HU-44.
- Unregistered participants cannot subscribe to another BDT game's real-time group and therefore cannot receive `PartidaBDTIniciada` for a game they do not participate in.

## Business Rules

- `RB-07`: BDT games use approved game states only.
- `RB-12`: every state transition must be validated before applying it.
- `RB-23`: a game cannot start unless configured minimum participation is satisfied.
- `RB-26`: the operator cannot manually start a game if minimum participation is not satisfied.
- `RB-28`: BDT start mode can be manual, automatic or both.
- `RB-B19`: when BDT starts, state changes to `Iniciada` and first stage is activated.
- Backend remains authoritative for state, minimums and start-mode validation.
- Backend remains authoritative for SignalR group subscription authorization. The hub may coordinate subscription, but it must delegate participant/operator eligibility to BDT Game Service application/infrastructure code and must not contain BDT business rules.
- BDT ranking must not be calculated from numeric accumulated score during start.

## Related Requirements

- `RF-04`
- `RF-11`
- `RF-13`
- `RF-27`
- `RF-31`
- `RF-32`
- `RF-35`
- `RF-36`
- `RNF-01`
- `RNF-02`
- `RNF-03`
- `RNF-04`
- `RNF-06`
- `RNF-13`
- `RNF-17`

## Acceptance Criteria

1. An authenticated operator can start a BDT game configured for manual start while it is in `Lobby` and minimum participation is satisfied.
2. The backend rejects missing games with `404`.
3. The backend rejects games that are not in `Lobby` with `409`.
4. The backend rejects games that do not meet minimum participation with `409`.
5. The backend rejects a manual operator start for a game configured as strictly `Automatico` with `409`.
6. Successful start changes game state to `Iniciada`.
7. Successful start activates the first stage and returns stage order, time limit and timer timestamps.
8. The endpoint rejects unauthenticated users with `401`.
9. The endpoint rejects authenticated non-operator users with `403`.
10. React web renders loading, success and error states for the start action.
11. A SignalR/WebSocket update is emitted after persistence so mobile clients can refresh active-stage state.
12. No business rule is implemented in the API endpoint or SignalR hub.
13. A post-commit SignalR/WebSocket dispatch failure does not roll back the start transition and does not turn the already-persisted command into an HTTP `500`.
14. A participant authenticated with a valid role but not registered/active in the requested BDT game cannot subscribe to that game's SignalR group.
15. A participant registered/active in the requested BDT game can subscribe to that game's SignalR group and receive `PartidaBDTIniciada`.
16. An authenticated operator can subscribe to BDT game updates for operator supervision.

## Assumptions

- Minimum participation is evaluated from BDT Game Service registrations/explorers persisted by HU-39 and later HU-40; BDT Game Service must not query another service database.
- Manual start is exposed through HTTP for the operator. Strictly automatic start requires a future scheduler/background trigger if the team decides to implement it.
- For first delivery, stage timer synchronization can use server timestamps returned by HTTP and real-time payloads; clients must not be authoritative for timers.
- SignalR/WebSocket is a notification adapter, not the source of truth. Once BDT Game Service commits the start transition, HTTP read models are the recovery path if a real-time notification is missed.
- The subscription authorization check can be implemented through a BDT-owned read/application port. It must use BDT Game Service state and token claims only; it must not call Team Service or read another service database for HU-43.

## Open Questions

- None blocking for SDD review.
