# HU-22 — Spec: Ver participantes unidos a Trivia publicada

## User story

Como Operador, quiero observar los participantes que solicitaron unirse a la partida de Trivia publicada, para supervisar el lobby antes de iniciar la partida.

## Source

- HU: HU-22
- Requirement: RF-13, RF-18, RF-35
- Service: Trivia Game Service
- Client: React web
- Actor: Operador

## Scope

Included:

- Query backend endpoint `GET /api/trivia-games/{id}/participants`.
- Return current lobby/game summary and individual participant inscriptions.
- Keep query read-only.
- Keep backend implementation inside Trivia Game Service.

Excluded:

- Accept/reject participants manually.
- Team lobby view; covered by HU-23.
- New SignalR behavior beyond the existing Trivia lobby hub contract.
- Manual accept/reject workflow in the operator UI.

## Business rules

- Only authenticated operators can use the operator lobby query.
- Query must not mutate game or inscription state.
- The endpoint must return `404` when the Trivia game does not exist.
- The endpoint must not access Team Service or BDT Game Service databases.

## API / Events

HTTP endpoints:

- `GET /api/trivia-games/{id}/participants` documented in `contracts/http/trivia-game-api.md`.

Events published:

- None through this query.

Events consumed:

- None.

## Acceptance criteria

- Operator receives `200 OK` with game lobby metadata and participant list.
- Operator can view the participant lobby list from the React web operator panel.
- Nonexistent game returns `404`.
- Query is read-only.
- Backend handler, API and frontend tests exist.

## Tests

- Application: `GetTriviaGameParticipantsQueryHandlerTests`.
- API: `TriviaGamesControllerTests` participant endpoint coverage.
- Contract: `contracts/http/trivia-game-api.md` endpoint section.
