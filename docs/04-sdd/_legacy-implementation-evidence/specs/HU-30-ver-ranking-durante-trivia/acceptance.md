# HU-30 — Acceptance

## Criterios de aceptación

| ID | Criterio | Estado | Evidencia |
|---|---|---|---|
| CA-01 | Ranking retorna lista ordenada por puntaje descendente | ✅ | Backend app/API tests + web rendering test |
| CA-02 | Empate se desempata por menor tiempo acumulado | ✅ | Backend app tests |
| CA-03 | Participante sin respuestas aparece al final con 0 | ✅ | Backend app tests |
| CA-04 | Endpoint retorna 404 si partida no existe | ✅ | API test |
| CA-05 | Endpoint retorna 401 si no autenticado | ✅ | API test |
| CA-06 | Hub SignalR emite RankingUpdated tras cada respuesta | ⏳ | Integration test |
| CA-07 | Operador puede ver ranking desde React web | ✅ | `TriviaOperationsPage.test.tsx` ranking case |

## Artefactos

| Artefacto | Ubicación |
|---|---|
| Spec | `docs/04-sdd/specs/HU-30-ver-ranking-durante-trivia/spec.md` |
| Design | `docs/04-sdd/specs/HU-30-ver-ranking-durante-trivia/design.md` |
| Tasks | `docs/04-sdd/specs/HU-30-ver-ranking-durante-trivia/tasks.md` |
| Acceptance | `docs/04-sdd/specs/HU-30-ver-ranking-durante-trivia/acceptance.md` |

## Integration pass evidence

- `frontend/src/features/trivia/TriviaOperationsPage.tsx` renders ranking from `GET /api/trivia-games/{id}/ranking`.
- Validation run: `npm test --prefix frontend` → 43 passed.
- Validation run: `npm run build --prefix frontend` → passed.
