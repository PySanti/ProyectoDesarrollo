# HU-21 — Acceptance

| Criterio | Verificado | Evidencia |
| --- | --- | --- |
| CA-01 — Consultar lobby exitoso | Backend verificado | `TriviaGameLobbyControllerTests.GetLobby_UserInscrito_Returns200WithData`; `GetTriviaGameLobbyQueryHandlerTests` |
| CA-02 — No inscrito retorna 403 | Backend verificado | `TriviaGameLobbyControllerTests.GetLobby_UserNotInscrito_Returns403`; handler throws unauthorized application error |
| CA-03 — Partida no existe retorna 404 | Backend verificado | `TriviaGameLobbyControllerTests.GetLobby_GameNotExists_Returns404` |
| CA-04 — Partida no lobby retorna estado actual | Backend parcial / mobile integrated | Endpoint returns lobby DTO from backend state; mobile renders returned state without mutating backend |
| CA-05 — Notificación SignalR al iniciar | Backend parcial | `TriviaLobbyHub` and notifier are wired by tasks; authorization hardening remains planned for later integration work |
| CA-06 — Notificación SignalR al cancelar | Fuera de alcance de implementación actual | Cancellation participant notification belongs to a later cancellation flow, not current partial-code integration |

## Current integration status

HU-21 has backend/API support and a React Native waiting screen integrated through `TriviaLobbyScreen`. SignalR hardening remains deferred to later real-time work.

## Integration pass evidence

- `mobile/src/features/trivia/screens/TriviaLobbyScreen.tsx` renders lobby state, participant count and join success/errors.
- Validation run: `npm test --prefix mobile` → 77 passed.
- Validation run: `npm run typecheck --prefix mobile` → passed.
