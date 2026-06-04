# HU-28 — Tasks

## Backend

- [x] T-01: Crear enum `MotivoCierre` y exponer el motivo público como `RespuestaCorrecta | TiempoAgotado`
- [x] T-02: Crear domain event `PreguntaTriviaCerradaDomainEvent`
- [x] T-03: Agregar `TiempoEmpleadoSegundos` a `RespuestaTrivia` entity y factory
- [x] T-04: Refactorizar `PartidaTrivia.CerrarPreguntaActual(motivo, respuestaCorrecta)` para emitir domain event
- [x] T-05: Agregar método `PartidaTrivia.AvanzarPregunta(nextQuestionId)`
- [x] T-06: Agregar método `PartidaTrivia.FinalizarPartida()`
- [x] T-07: Actualizar `RespuestaTriviaDto` con `TiempoEmpleadoSegundos`
- [x] T-08: Crear `QuestionResultDto`
- [x] T-09: Crear `GetQuestionResultQuery` + handler
- [x] T-10: Actualizar `AnswerTriviaQuestionCommandHandler` para avanzar pregunta tras respuesta correcta
- [x] T-11: Agregar endpoint `GET /api/trivia-games/{id}/questions/{preguntaId}/result`

## Tests

- [x] T-12: Escribir tests de dominio para `CerrarPreguntaActual`, `AvanzarPregunta`, `FinalizarPartida`
- [x] T-13: Escribir tests de handler para `GetQuestionResultQueryHandler`
- [x] T-14: Escribir tests API para endpoint question result

## Documentación

- [x] T-15: Actualizar `trivia-game-api.md` con nuevo endpoint
- [x] T-16: Actualizar `traceability-matrix.md`
- [x] T-17: Actualizar `SPECS-LIST.md`
- [x] T-18: Completar `spec.md`, `design.md`, `tasks.md`, `acceptance.md`
