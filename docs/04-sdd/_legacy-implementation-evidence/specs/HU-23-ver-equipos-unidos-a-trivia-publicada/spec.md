# HU-23 — Spec: Ver equipos unidos a Trivia publicada

## User story

Como Operador, quiero observar los equipos que solicitaron unirse a la partida de Trivia publicada, para supervisar el lobby de una Trivia por equipos.

## Source

- HU: HU-23
- Requirement: RF-13, RF-18, RF-35
- Service: Trivia Game Service
- Client: React web
- Actor: Operador

## Scope

Included:

- Query backend endpoint `GET /api/trivia-games/{id}/teams`.
- Return unique team identifiers registered in the Trivia lobby.
- Keep query read-only.
- Keep backend implementation inside Trivia Game Service.

Excluded:

- Team master data lookup or team member details from Team Service.
- Manual accept/reject workflow.
- Manual team approval or rejection from the operator UI.

## Business rules

- Only authenticated operators can use the operator teams query.
- Query must not mutate game, inscription or team state.
- The endpoint must return `404` when the Trivia game does not exist.
- Trivia Game Service must not read Team Service database directly.

## API / Events

HTTP endpoints:

- `GET /api/trivia-games/{id}/teams` documented in `contracts/http/trivia-game-api.md`.

Events published:

- None through this query.

Events consumed:

- None.

## Acceptance criteria

- Operator receives `200 OK` with unique team IDs for a Trivia game.
- Operator can view the team lobby list from the React web operator panel.
- If no teams are registered, response is an empty list.
- Nonexistent game returns `404`.
- Backend handler, API and frontend tests exist.

## Tests

- Application: `GetTriviaGameTeamsQueryHandlerTests`.
- API: `TriviaGamesControllerTests` team endpoint coverage.
- Contract: `contracts/http/trivia-game-api.md` endpoint section.
