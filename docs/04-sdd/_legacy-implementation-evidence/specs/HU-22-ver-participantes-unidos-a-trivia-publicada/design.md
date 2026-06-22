# HU-22 — Design

## Context

- Bounded context: Trivia.
- Owning microservice: Trivia Game Service.
- Client target: React web operator panel.

## Backend Design

- Controller endpoint: `GET /api/trivia-games/{id}/participants`.
- Query: `GetTriviaGameParticipantsQuery`.
- Handler: `GetTriviaGameParticipantsQueryHandler`.
- Persistence port: `ITriviaInscripcionRepository.ListByPartidaIdAsync` and `IPartidaTriviaRepository.GetByIdAsync`.
- DTO: lobby/participants read model documented in `contracts/http/trivia-game-api.md`.

The query loads the `PartidaTrivia` to confirm existence and combines it with current inscriptions for that game. It does not change domain state.

## Frontend Design

React web uses `TriviaOperationsPage` in the operator shell. The page calls `GET /api/trivia-games/{id}/participants` through the Trivia API client and renders loading, empty, error and table states using the unified blue/white card style.

## Authorization

- Endpoint requires `Operador` authorization according to the contract.
- No participant/mobile access is intended for this operator view.

## Communication

- HTTP query only for current closure.
- Existing Trivia lobby SignalR hub may later refresh the operator view, but this SDD does not introduce new event names.

## Design Patterns Applied

| Pattern | Location | Problem solved | Justification |
|---|---|---|---|
| CQRS | `GetTriviaGameParticipantsQuery` | Read-only lobby participant query | Keeps operator read model separate from commands |
| Mediator | Query handler | Decouples controller from application logic | Matches service convention |
| Repository | Application ports | Abstracts persistence | Keeps API/domain independent from EF Core |

## Test Plan

- Handler returns participant list for existing game.
- Handler throws not found for missing game.
- API returns documented status codes and response shape.
- Frontend renders participant list and success/error states.
