# HU-35 — Design: Ver lista de partidas de Trivia publicadas

## Context

- Bounded context: Trivia.
- Owning microservice: Trivia Game Service.
- Client target: React web operator UI.
- Supporting services: Identity Service only through JWT/operator authorization claims.

## Backend design

Add a read-only CQRS query:

- `GetOperatorSupervisableTriviaGamesQuery`.
- `GetOperatorSupervisableTriviaGamesQueryHandler`.
- Repository port method `GetSupervisableForOperatorAsync`.
- EF Core implementation filtering `PartidaEstado.Lobby` and `PartidaEstado.Iniciada`.

The endpoint lives in the operator controller protected by `PolicyNames.Operador`:

```txt
GET /api/trivia-games/operator/supervision
```

The response uses `TriviaGameListItemDto` to avoid introducing a parallel read model for the same summary fields.

## Frontend design

`TriviaOperationsPage` keeps the existing create-form section and replaces the manual UUID field in `Supervisar partida` with an operator list loaded from the new endpoint.

State additions:

- `supervisableGames`.
- `selectedGameId`.
- `selectedGame` derived from the selected id.
- `loading` values for list, selection and start.

Selection behavior:

- On mount, load operator supervision list.
- On game selection, call participants, teams and ranking endpoints using the selected id.
- Render all detail sections together.
- `Iniciar Trivia` calls the existing start endpoint and then refreshes the supervision list and selected detail.

## Contract design

The existing participant endpoint `GET /api/trivia-games` remains unchanged because it is used by mobile participant listing stories HU-09/HU-11 and should expose published `Lobby` games.

HU-35 introduces an operator-specific endpoint so `Iniciada` games can be supervised without changing mobile semantics.

## Design Patterns Applied

| Pattern | Location | Problem solved | Justification |
|---|---|---|---|
| CQRS | `GetOperatorSupervisableTriviaGamesQuery` | Separate read-only operator listing from commands | The query does not mutate state. |
| Mediator | `TriviaGamesController` | Decouple controller from application logic | Controllers invoke MediatR only. |
| Repository | `IPartidaTriviaRepository` | Abstract EF Core persistence | Application layer remains independent from infrastructure. |
| Adapter | `triviaApi.ts` | Encapsulate HTTP access in frontend | UI consumes documented contract through an API function. |

## Tests

- Application handler verifies only `Lobby` and `Iniciada` are returned from repository output.
- API verifies operator authorization and response shape.
- Frontend verifies list selection, detail rendering and no manual UUID input.

## Risks

- Existing API tests use shared InMemory fixtures in some classes; new tests should avoid depending on global empty database state.
- Existing team detail endpoint may return empty list until team registration HU-19 is implemented; the UI must still render an empty teams state.
