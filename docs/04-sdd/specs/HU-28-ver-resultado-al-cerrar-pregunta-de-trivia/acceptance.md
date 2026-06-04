# HU-28 — Acceptance

## Criterios de aceptación

| ID | Criterio | Estado | Evidencia |
|---|---|---|---|
| CA-01 | Participante consulta resultado de pregunta cerrada y ve respuesta correcta, su respuesta, puntaje, tiempo y motivo de cierre | ✅ | API test `GetQuestionResult_AfterCorrectAnswer_ReturnsResult` verifica todos los campos del DTO |
| CA-02 | Si el participante no respondió, se refleja en la respuesta | ✅ | Handler retorna valores por defecto cuando no hay respuesta del participante |
| CA-03 | Si la pregunta sigue activa, endpoint retorna 400 | ✅ | API test `GetQuestionResult_ActiveQuestion_ReturnsBadRequest` |
| CA-04 | Responder correctamente avanza a la siguiente pregunta automáticamente | ✅ | Handler busca siguiente pregunta por DisplayOrder y llama `AvanzarPregunta` |
| CA-05 | Responder correctamente la última pregunta finaliza la partida | ✅ | Handler llama `FinalizarPartida` cuando no hay siguiente pregunta |
| CA-06 | Responder incorrectamente no cierra la pregunta ni avanza | ✅ | `RegistrarRespuestaDefinitiva` con respuesta incorrecta no llama `CerrarPreguntaActual` |
| CA-07 | Mobile muestra resultado de pregunta desde endpoint documentado | ✅ | `TriviaResultScreen` + `triviaParticipantFlow.test.js` |

## Test results

| Suite | Total | Passed | Failed |
|---|---|---|---|
| Domain | 149 | 149 | 0 |
| Application | 105 | 105 | 0 |
| API | 51 | 50 | 1 (pre-existing: GetAll_NoGames_ReturnsEmptyList — shared DB state, unrelated) |

## Artefactos

| Artefacto | Ubicación |
|---|---|
| Spec | `docs/04-sdd/specs/HU-28-ver-resultado-al-cerrar-pregunta-de-trivia/spec.md` |
| Design | `docs/04-sdd/specs/HU-28-ver-resultado-al-cerrar-pregunta-de-trivia/design.md` |
| Tasks | `docs/04-sdd/specs/HU-28-ver-resultado-al-cerrar-pregunta-de-trivia/tasks.md` |
| Acceptance | `docs/04-sdd/specs/HU-28-ver-resultado-al-cerrar-pregunta-de-trivia/acceptance.md` |
| API contract | `contracts/http/trivia-game-api.md` |
| Traceability | `docs/04-sdd/traceability-matrix.md` |
| SPECS-LIST | `docs/04-sdd/SPECS-LIST.md` |

## Integration pass evidence

- `mobile/src/features/trivia/screens/TriviaResultScreen.tsx` displays correct answer, selected answer, score and closure reason returned by backend.
- Validation run: `npm test --prefix mobile` → 77 passed.
- Validation run: `npm run typecheck --prefix mobile` → passed.
