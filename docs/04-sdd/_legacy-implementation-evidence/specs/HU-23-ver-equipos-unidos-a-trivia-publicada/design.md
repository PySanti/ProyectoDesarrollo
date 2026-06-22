# HU-23 — Design

## Context

- Bounded context: Trivia.
- Owning microservice: Trivia Game Service.
- Client target: React web operator panel.

## Backend Design

- Controller endpoint: `GET /api/trivia-games/{id}/teams`.
- Query: `GetTriviaGameTeamsQuery`.
- Handler: `GetTriviaGameTeamsQueryHandler`.
- Persistence port: `ITriviaInscripcionRepository.ListByPartidaIdAsync` and `IPartidaTriviaRepository.GetByIdAsync`.
- DTO: list of team lobby items documented in `contracts/http/trivia-game-api.md`.

The handler loads the game for existence validation and derives unique team IDs from Trivia inscriptions. Team master data stays owned by Team Service and is not queried in this implemented backend slice.

## Frontend Design

React web uses `TriviaOperationsPage` in the operator shell. The page calls `GET /api/trivia-games/{id}/teams` through the Trivia API client and renders loading, empty, error and table states using the unified blue/white card style.

## Authorization

- Endpoint requires `Operador` authorization according to the contract.
- No participant/mobile access is intended for this operator view.

## Communication

- HTTP query only for current backend closure.
- No RabbitMQ integration event is required.

## Design Patterns Applied

| Pattern | Location | Problem solved | Justification |
|---|---|---|---|
| CQRS | `GetTriviaGameTeamsQuery` | Read-only lobby team query | Keeps operator read model separate from commands |
| Mediator | Query handler | Decouples controller from application logic | Matches service convention |
| Repository | Application ports | Abstracts persistence | Keeps API/domain independent from EF Core |

## Test Plan

- Handler returns unique team IDs for registered team inscriptions.
- Handler returns an empty list when no teams are registered.
- Handler throws not found for missing game.
- API returns documented status codes and response shape.
- Frontend renders team list and success/error states.
