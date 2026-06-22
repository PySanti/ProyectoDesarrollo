# HU-30 — Tasks

## Backend

- [x] T-01: Crear SDD (spec, design, tasks, acceptance)
- [x] T-02: Crear `RankingEntryDto`
- [x] T-03: Crear `GetRankingQuery`
- [x] T-04: Crear `GetRankingQueryHandler`
- [x] T-05: Agregar endpoint `GET /api/trivia-games/{id}/ranking`
- [x] T-06: Crear `ITriviaRankingNotifier` en Application.Ports
- [x] T-07: Crear `TriviaRankingHub` en Api/Hubs
- [x] T-08: Crear `TriviaRankingNotifier` en Api/Hubs
- [x] T-09: Inyectar `ITriviaRankingNotifier` en `AnswerTriviaQuestionCommandHandler`
- [x] T-10: Registrar hub y notifier en `Program.cs`

## Tests

- [x] T-11: Escribir tests de handler para `GetRankingQueryHandler`
- [x] T-12: Escribir tests API para endpoint ranking

## Documentación

- [x] T-13: Actualizar `trivia-game-api.md`
- [x] T-14: Actualizar `traceability-matrix.md`
- [x] T-15: Actualizar `SPECS-LIST.md`
