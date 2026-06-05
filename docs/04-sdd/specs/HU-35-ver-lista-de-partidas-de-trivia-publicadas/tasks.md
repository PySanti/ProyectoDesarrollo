# HU-35 — Tasks

## Backend

- [x] Add `GetOperatorSupervisableTriviaGamesQuery`.
- [x] Add query handler returning `TriviaGameListItemDto` for `Lobby` and `Iniciada` games.
- [x] Extend `IPartidaTriviaRepository` and EF implementation with operator supervision query.
- [x] Add `GET /api/trivia-games/operator/supervision` to the operator controller.
- [x] Add backend application/API tests.

## Contracts

- [x] Document `GET /api/trivia-games/operator/supervision` in `contracts/http/trivia-game-api.md`.

## Frontend

- [x] Add `getOperatorTriviaGamesForSupervision` API function.
- [x] Replace manual UUID input in `TriviaOperationsPage` with selectable list.
- [x] Load participants, teams and ranking after selecting a game.
- [x] Keep `Iniciar Trivia` wired to selected game and disabled when state is `Iniciada`.
- [x] Add frontend tests for the new supervision flow.

## Documentation

- [x] Update `acceptance.md` with evidence.
- [x] Update `docs/04-sdd/traceability-matrix.md`.
