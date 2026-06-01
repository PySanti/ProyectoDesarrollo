# HU-28 — Design

## Owning service

Trivia Game Service

## Context

Trivia Context

## Aggregates touched

- `PartidaTrivia` (aggregate root)
- `RespuestaTrivia` (entity hija)
- `FormularioTrivia` (solo consulta para obtener respuesta correcta)

## Domain model changes

### Nuevo enum

`Umbral.TriviaGame.Domain.Enums.MotivoCierre`

| Value | Description |
|---|---|
| `CorrectAnswer` | Pregunta cerrada porque un jugador acertó |
| `TimeExpired` | Pregunta cerrada porque se agotó el tiempo |

### Nuevo domain event

`PreguntaTriviaCerradaDomainEvent`

| Campo | Tipo | Descripción |
|---|---|---|
| `PartidaId` | Guid | ID de la partida |
| `PreguntaId` | Guid | ID de la pregunta cerrada |
| `Motivo` | MotivoCierre | Por qué se cerró |
| `RespuestaCorrecta` | string | Texto de la opción correcta |

### Cambios en PartidaTrivia

| Método | Cambio |
|---|---|
| `CerrarPreguntaActual(MotivoCierre motivo, string respuestaCorrecta)` | Antes: sin parámetros. Ahora emite `PreguntaTriviaCerradaDomainEvent` y recibe motivo + respuesta correcta |
| `AvanzarPregunta(QuestionId nextQuestionId)` | Nuevo. Valida estado `Iniciada`, actualiza `PreguntaActualId` y `PreguntaAbiertaEnUtc` |
| `FinalizarPartida()` | Nuevo. Cambia estado a `Terminada`, limpia pregunta activa |
| `RegistrarRespuestaDefinitiva(...)` | Agregado parámetro `respuestaCorrecta` para pasarlo a `CerrarPreguntaActual` |

### Cambios en RespuestaTrivia

| Campo | Cambio |
|---|---|
| `TiempoEmpleadoSegundos` | Nuevo campo (double). Se calcula y persiste al crear la respuesta |

### Nuevo DTO

`QuestionResultDto`

| Campo | Tipo |
|---|---|
| `PartidaId` | Guid |
| `PreguntaId` | Guid |
| `RespuestaCorrecta` | string |
| `RespuestaSeleccionada` | string |
| `EsCorrecta` | bool |
| `PuntajeObtenido` | int |
| `TiempoEmpleadoSegundos` | double |
| `MotivoCierre` | string |
| `FechaCierre` | DateTime |

## Query

### GetQuestionResultQuery

| Campo | Tipo |
|---|---|
| `PartidaId` | Guid |
| `PreguntaId` | Guid |
| `UsuarioId` | string (del claim JWT) |

### Handler logic

1. Cargar `PartidaTrivia` por `PartidaId`.
2. Si no existe, throw NotFound.
3. Verificar que la pregunta exista en el formulario asociado.
4. Obtener la `RespuestaTrivia` del participante para esa pregunta.
5. Si la pregunta `PreguntaActualId` es null (cerrada) o distinta, la pregunta está cerrada.
6. Inferir `MotivoCierre`: si hay respuesta correcta registrada para esa pregunta por algún participante → `CorrectAnswer`, si no → `TimeExpired`.
7. Si la pregunta está activa → throw 400.

## Command changes

### AnswerTriviaQuestionCommandHandler

- Ahora recibe `respuestaCorrecta` del formulario y lo pasa a `RegistrarRespuestaDefinitiva`.
- Tras respuesta correcta, busca la siguiente pregunta por `DisplayOrder` ascendente.
- Si existe siguiente pregunta → llama `AvanzarPregunta`.
- Si no existe siguiente pregunta (era la última) → llama `FinalizarPartida`.

## Business logic decisions

1. **Motivo de cierre inferido**: no se persiste el enum `MotivoCierre` en base de datos. Se infiere desde las respuestas almacenadas al momento de la consulta. Esto evita agregar una columna adicional y mantiene compatibilidad con datos existentes.
2. **Avance por DisplayOrder**: la siguiente pregunta se determina por el `DisplayOrder` más cercano mayor al de la pregunta actual.
3. **Domain events no persisten**: `PreguntaTriviaCerradaDomainEvent` se dispara en memoria para posibles handlers de integración futuros (SignalR, RabbitMQ).

## Design Patterns Applied

| Pattern | Location | Problem solved |
|---|---|---|
| CQRS | `GetQuestionResultQuery` / handler | Lectura separada de escritura |
| Mediator | MediatR handler pipeline | Orquestación del caso de uso |
| Domain Event | `PreguntaTriviaCerradaDomainEvent` | Notificar cierre de pregunta a otros componentes |
| Aggregate Method | `PartidaTrivia.AvanzarPregunta()`, `FinalizarPartida()` | Encapsular invariantes de transición de estado |
