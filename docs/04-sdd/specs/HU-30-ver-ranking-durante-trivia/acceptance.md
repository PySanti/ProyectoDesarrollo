# HU-30 — Acceptance

## Criterios de aceptación

| ID | Criterio | Estado | Evidencia |
|---|---|---|---|
| CA-01 | Ranking retorna lista ordenada por puntaje descendente | ⏳ | App test |
| CA-02 | Empate se desempata por menor tiempo acumulado | ⏳ | App test |
| CA-03 | Participante sin respuestas aparece al final con 0 | ⏳ | App test |
| CA-04 | Endpoint retorna 404 si partida no existe | ⏳ | API test |
| CA-05 | Endpoint retorna 401 si no autenticado | ⏳ | API test |
| CA-06 | Hub SignalR emite RankingUpdated tras cada respuesta | ⏳ | Integration test |

## Artefactos

| Artefacto | Ubicación |
|---|---|
| Spec | `docs/04-sdd/specs/HU-30-ver-ranking-durante-trivia/spec.md` |
| Design | `docs/04-sdd/specs/HU-30-ver-ranking-durante-trivia/design.md` |
| Tasks | `docs/04-sdd/specs/HU-30-ver-ranking-durante-trivia/tasks.md` |
| Acceptance | `docs/04-sdd/specs/HU-30-ver-ranking-durante-trivia/acceptance.md` |
