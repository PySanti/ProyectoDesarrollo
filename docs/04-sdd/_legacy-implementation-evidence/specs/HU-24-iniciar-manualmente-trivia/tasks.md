# HU-24 — Tasks: Iniciar manualmente Trivia

## Task list

| # | Task | Layer | Status |
|---|------|-------|--------|
| 1 | Agregar `ManualYAutomatico = 2` al enum `ModoInicio` | Domain | Hecho |
| 2 | Crear `ModoInicioAutomaticoException` | Domain | Hecho |
| 3 | Agregar parámetro `esInicioManual` a `PartidaTrivia.Iniciar()` con validación | Domain | Hecho |
| 4 | Agregar "ManualYAutomatico" al switch en `TriviaGameMapper.ToModoInicio()` | Application | Hecho |
| 5 | Pasar `esInicioManual: true` en `StartTriviaGameCommandHandler` | Application | Hecho |
| 6 | Agregar handler test: `ModoInicioAutomatico` lanza excepción | Tests | Hecho |
| 7 | Agregar handler test: `ManualYAutomatico` permite inicio | Tests | Hecho |
| 8 | Agregar API test: `ModoInicioAutomatico` returns 409 | Tests | Hecho |
| 9 | Actualizar contrato `contracts/http/trivia-game-api.md` | Docs | Hecho |
| 10 | Ejecutar test suite completo | QA | Hecho — 325/326 pass (1 pre-existing InMemory isolation issue) |
