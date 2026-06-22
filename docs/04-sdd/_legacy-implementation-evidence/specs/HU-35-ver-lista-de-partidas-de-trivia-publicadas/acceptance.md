# HU-35 — Acceptance

## Criteria

| ID | Criterion | Status | Evidence |
|---|---|---|---|
| CA-01 | Operator sees Trivia games in `Lobby` or `Iniciada` without typing a UUID. | Passed | `TriviaOperationsPage.test.tsx`; API `GetOperatorSupervision_ReturnsLobbyAndStartedGames`. |
| CA-02 | Selecting a game displays participants, teams and ranking. | Passed | `TriviaOperationsPage.test.tsx` selection tests load all detail endpoints. |
| CA-03 | `Iniciar Trivia` uses the selected game id. | Passed | `TriviaOperationsPage.test.tsx` asserts `startTriviaGame("game-1", "operator-token")`. |
| CA-04 | `Iniciar Trivia` is disabled for `Iniciada` games. | Passed | `TriviaOperationsPage.test.tsx` started-game test. |
| CA-05 | Backend endpoint is operator-only and read-only. | Passed | API test verifies `403` without Operador role; endpoint is a query. |
| CA-06 | Empty and error states are visible in React web. | Passed | `TriviaOperationsPage.test.tsx` covers empty list; error mapping implemented in component. |

## Validation Evidence

- Backend application focused: `dotnet test "tests/Umbral.TriviaGame.Application.Tests/Umbral.TriviaGame.Application.Tests.csproj" --filter "FullyQualifiedName~GetOperatorSupervisableTriviaGamesQueryHandlerTests"` — Passed 2/2.
- Backend API focused: `dotnet test "tests/Umbral.TriviaGame.Api.Tests/Umbral.TriviaGame.Api.Tests.csproj" --filter "FullyQualifiedName~TriviaGamesControllerTests.GetOperatorSupervision"` — Passed 2/2.
- Frontend focused: `npm test -- --run src/features/trivia/TriviaOperationsPage.test.tsx` — Passed 7/7.
