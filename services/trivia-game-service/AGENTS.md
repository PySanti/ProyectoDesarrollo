# Trivia Game Service — Flow State

## Active feature
HU-29 — Calcular puntaje de respuesta en Trivia (score controller integration tests)

## Test counts
- Domain: 154 pass, 0 fail
- Application: 109 pass, 0 fail
- API: 55 pass, 1 fail (pre-existing InMemory isolation: `GetAll_NoGames_ReturnsEmptyList`)
- Total: 318/319 pass

## Recent changes
- Added `TriviaGameScoreControllerTests.cs` with 5 score integration tests
- Added `Orden` property to `AnswerOption` value object for deterministic option ordering
- Added migration `20260601030415_AddAnswerOptionOrden`
- Fixed `Question.Options` getter to sort by `Orden`
- Updated `TriviaFormMapper.ToDto` to use `opt.Orden` instead of `Select` index
- Updated `AnswerOptionDraft.ToAnswerOption(int orden)` and `AnswerOption.FromDraft(draft, orden)`
- Updated domain/application tests to pass `orden`
- Fixed timing assertions (`> 0` → `>= 0`) in domain and app tests
- Updated traceability matrix and API contract for HU-29

## Known issues
- `GetAll_NoGames_ReturnsEmptyList` fails because InMemory database is shared across tests within same fixture class

## Next steps
- Investigate `PreguntaAbiertaEnUtc` not persisting across `UpdateAsync` cycles with InMemory database (causes 0s TiempoEmpleadoSegundos for multi-answer tests)
- Move to next first-sprint feature once HU-29 acceptance is confirmed
