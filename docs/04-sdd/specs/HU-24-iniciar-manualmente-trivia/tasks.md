# HU-24 — Tasks: Iniciar manualmente Trivia

## Task list

| # | Task | Layer | Status |
|---|------|-------|--------|
| 1 | Agregar `ManualYAutomatico = 2` al enum `ModoInicio` | Domain | Pendiente |
| 2 | Crear `ModoInicioAutomaticoException` | Domain | Pendiente |
| 3 | Agregar parámetro `esInicioManual` a `PartidaTrivia.Iniciar()` con validación | Domain | Pendiente |
| 4 | Agregar "ManualYAutomatico" al switch en `TriviaGameMapper.ToModoInicio()` | Application | Pendiente |
| 5 | Pasar `esInicioManual: true` en `StartTriviaGameCommandHandler` | Application | Pendiente |
| 6 | Agregar handler test: `ModoInicioAutomatico` lanza excepción | Tests | Pendiente |
| 7 | Agregar handler test: `ManualYAutomatico` permite inicio | Tests | Pendiente |
| 8 | Agregar API test: `ModoInicioAutomatico` returns 409 | Tests | Pendiente |
| 9 | Actualizar contrato `contracts/http/trivia-game-api.md` | Docs | Pendiente |
| 10 | Ejecutar test suite completo | QA | Pendiente |
