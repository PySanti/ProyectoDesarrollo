# HU-17 — Tasks: Crear y publicar partida de Trivia

## Task list

| # | Task | Layer | Status |
|---|------|-------|--------|
| 1 | Crear value objects: `NombrePartida`, `CantidadMinima`, `CantidadMaximaJugadores`, `CantidadMaximaEquipos`, `JugadoresPorEquipoMin`, `JugadoresPorEquipoMax`, `TiempoInicio`, `OperatorId` | Domain | Hecho |
| 2 | Agregar `PartidaTrivia.Create()` con validación de invariantes | Domain | Hecho |
| 3 | Crear excepciones `TriviaFormNotFoundException`, `FormularioIncompletoException` | Domain | Hecho |
| 4 | Crear `CreateTriviaGameCommand` con todos los campos | Application | Hecho |
| 5 | Crear `CreateTriviaGameCommandValidator` (FluentValidation) | Application | Hecho |
| 6 | Crear `TriviaGameMapper` (`ToModalidad`, `ToModoInicio`, `ToDto`) | Application | Hecho |
| 7 | Crear `CreateTriviaGameCommandHandler` con flujo completo | Application | Hecho |
| 8 | Agregar `POST /api/trivia-games` en `TriviaGamesController` | API | Hecho |
| 9 | Agregar handler tests: creación individual, equipos, formulario no encontrado, formulario incompleto | Tests | Hecho |
| 10 | Agregar API tests: 201, 404, 403 | Tests | Hecho |
| 11 | Ejecutar test suite completo | QA | Hecho |
| 12 | Documentar endpoint en `contracts/http/trivia-game-api.md` | Docs | Hecho |
| 13 | Crear SDD: `spec.md`, `design.md`, `tasks.md`, `acceptance.md` | Docs | Hecho |
