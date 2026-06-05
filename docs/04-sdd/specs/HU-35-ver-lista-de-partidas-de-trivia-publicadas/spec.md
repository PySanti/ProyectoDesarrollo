# HU-35 — Spec: Ver lista de partidas de Trivia publicadas

## User story

Como Operador, quiero ver la lista de partidas de Trivia publicadas o iniciadas, para seleccionar una partida y supervisar su lobby, equipos, ranking e inicio sin introducir manualmente un UUID.

## Source

- HU: HU-35
- Requirements: RF-17, RF-18, RF-23, RF-35, RF-36
- Service: Trivia Game Service
- Client: React web
- Actor: Operador
- Related stories: HU-22, HU-23, HU-24, HU-30

## Scope

Included:

- Operator query endpoint `GET /api/trivia-games/operator/supervision`.
- Return Trivia games in `Lobby` or `Iniciada` state.
- Keep participant listing endpoint `GET /api/trivia-games` unchanged for mobile published-game listing.
- Replace the manual UUID input in React web `Supervisar partida` with a selectable operator list.
- When a game is selected, show participants, teams, ranking and the manual start action in the same supervision section.

Excluded:

- Real-time lobby/ranking hardening beyond documented existing endpoints.
- Game cancellation.
- Team registration for Trivia team games.
- Participant gameplay screens.
- Changing Trivia scoring rules.

## Business rules

- Only authenticated operators can access the operator supervision list.
- The supervision list is read-only and must not mutate game state.
- The supervision list returns only `Lobby` and `Iniciada` Trivia games.
- Participant mobile listing must continue to expose only published `Lobby` games.
- The React web frontend must not require operators to type a raw UUID for supervision.
- Starting Trivia remains backend-authoritative through HU-24 and is available only for selected games in `Lobby`; backend still validates minimum participation and allowed start mode.
- Ranking remains ordered by accumulated score descending and tie-breaker by accumulated response time according to HU-30.

## API / Events

HTTP endpoints:

- `GET /api/trivia-games/operator/supervision` documented in `contracts/http/trivia-game-api.md`.
- Existing detail endpoints reused after selection:
  - `GET /api/trivia-games/{id}/participants`.
  - `GET /api/trivia-games/{id}/teams`.
  - `GET /api/trivia-games/{id}/ranking`.
  - `POST /api/trivia-games/{id}/start`.

Events published:

- None for the read-only operator list.

Events consumed:

- None for this feature.

## Acceptance criteria

- Operator sees a list of Trivia games in `Lobby` or `Iniciada` without typing a UUID.
- Operator can select a game from the list.
- Selecting a game displays game summary, participants, teams and ranking.
- The start action uses the selected game id.
- The start action is disabled for games already in `Iniciada` state.
- Empty list and error states are visible.
- Backend, contract and frontend tests cover the new flow.

## Tests

- Application: `GetOperatorSupervisableTriviaGamesQueryHandlerTests`.
- API: `TriviaGamesControllerTests` or dedicated operator supervision endpoint tests.
- Frontend: `TriviaOperationsPage.test.tsx` selection, detail loading, start and empty/error states.
