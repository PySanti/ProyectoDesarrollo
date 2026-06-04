# HU-18 — Acceptance

| Criterio | Verificado | Evidencia |
| --- | --- | --- |
| CA-01 — Inscripción exitosa | ✅ | `JoinTriviaGameCommandHandlerTests.Handle_PartidaIndividualEnLobbyConCupo_InscribeExitosamente` + `JoinTriviaGameControllerTests.Join_IndividualGameEnLobby_Returns200` |
| CA-02 — Game no existe | ✅ | `JoinTriviaGameCommandHandlerTests.Handle_PartidaNoExiste_ThrowsPartidaTriviaNotFoundException` + `JoinTriviaGameControllerTests.Join_GameNotExists_Returns404` |
| CA-03 — Game no está en Lobby | ✅ | `JoinTriviaGameCommandHandlerTests.Handle_PartidaNoEnLobby_ThrowsInvalidStateTransitionException` + `JoinTriviaGameControllerTests.Join_GameAlreadyStarted_Returns409` |
| CA-04 — Game es modalidad Equipo | ✅ | `JoinTriviaGameCommandHandlerTests.Handle_PartidaModalidadEquipo_ThrowsModalidadInvalidaException` + `JoinTriviaGameControllerTests.Join_EquipoGame_Returns409` |
| CA-05 — Cupo lleno | ✅ | `JoinTriviaGameCommandHandlerTests.Handle_CupoLleno_ThrowsCupoLlenoException` + `JoinTriviaGameControllerTests.Join_CupoLleno_Returns409` |
| CA-06 — Ya inscrito | ✅ | `JoinTriviaGameCommandHandlerTests.Handle_UsuarioYaInscrito_ThrowsJugadorYaInscritoException` + `JoinTriviaGameControllerTests.Join_Duplicado_Returns409` |
| CA-07 — Mobile permite abrir espera y ejecutar inscripción individual | ✅ | `mobile/src/features/trivia/screens/TriviaGamesListScreen.tsx`, `TriviaLobbyScreen.tsx`; `npm run typecheck --prefix mobile` passed |

## Integration pass evidence

- React Native list now opens `TriviaLobby` for a selected published Trivia game.
- `TriviaLobbyScreen` calls `POST /api/trivia-games/{id}/join` for individual join and displays backend conflict messages.
- Validation run: `npm test --prefix mobile` → 77 passed.
- Validation run: `npm run typecheck --prefix mobile` → passed.
