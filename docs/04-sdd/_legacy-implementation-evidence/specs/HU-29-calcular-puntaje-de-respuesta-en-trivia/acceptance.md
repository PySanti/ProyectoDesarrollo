# HU-29 — Acceptance

## Criterios de aceptación

| ID | Criterio | Estado | Evidencia |
|---|---|---|---|
| CA-01 | Respuesta correcta suma exactamente el `assignedScore` | ✅ | App test `Handle_ConRespuestas_RetornaScoreAcumuladoCorrecto` |
| CA-02 | Respuesta incorrecta suma 0 puntos | ✅ | App test mismo — 300 pts de 3 respuestas (100+0+200) |
| CA-03 | Tiempo no modifica el puntaje | ✅ | `TiempoAcumuladoSegundos` se calcula aparte, no afecta `PuntajeAcumulado` |
| CA-04 | Endpoint GET score retorna puntaje acumulado, tiempo y conteo | ✅ | 3 API tests: 0 respuestas, 1 correcta, 2 correctas |
| CA-05 | Endpoint retorna 404 si partida no existe | ✅ | API test `GetScore_GameNotFound_Returns404` |
| CA-06 | Endpoint retorna 401 sin autenticación | ✅ | API test `GetScore_Unauthenticated_Returns401` |
| CA-07 | Mobile muestra puntaje acumulado calculado por backend sin recalcularlo | ✅ | `TriviaScoreScreen` + `triviaParticipantFlow.test.js` |

## Artefactos

| Artefacto | Ubicación |
|---|---|
| Spec | `docs/04-sdd/specs/HU-29-calcular-puntaje-de-respuesta-en-trivia/spec.md` |
| Design | `docs/04-sdd/specs/HU-29-calcular-puntaje-de-respuesta-en-trivia/design.md` |
| Tasks | `docs/04-sdd/specs/HU-29-calcular-puntaje-de-respuesta-en-trivia/tasks.md` |
| Acceptance | `docs/04-sdd/specs/HU-29-calcular-puntaje-de-respuesta-en-trivia/acceptance.md` |

## Integration pass evidence

- `mobile/src/features/trivia/screens/TriviaScoreScreen.tsx` displays backend `puntajeAcumulado`, response counts and time telemetry.
- `mobile/tests/triviaParticipantFlow.test.js` verifies the mobile summary does not recompute score.
- Validation run: `npm test --prefix mobile` → 77 passed.
- Validation run: `npm run typecheck --prefix mobile` → passed.
