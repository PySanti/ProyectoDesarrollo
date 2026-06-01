# HU-28 — Ver resultado al cerrar pregunta de Trivia

## User story

Como **Participante**, quiero ver el resultado de la pregunta cuando se cierre, para saber cuál era la respuesta correcta.

## Source

| Campo | Valor |
|---|---|
| HU | HU-28 |
| Actor | Participante |
| Requerimientos | RF-22 |
| Reglas de negocio | RB-T25, RB-T26, RB-T27, RB-T28, RB-T29 |
| Microservicio | Trivia Game Service |
| Servicios de apoyo | Identity Service (JWT / autenticación) |
| Cliente objetivo | React Native mobile |

## Scope

### Incluido

- Consultar resultado de una pregunta cerrada: saber si fue correcta o incorrecta, cuál era la respuesta correcta, puntaje obtenido, tiempo empleado y motivo de cierre.
- Mostrar la respuesta correcta a todos los participantes, incluyendo quienes no respondieron.
- Avance automático a la siguiente pregunta tras respuesta correcta.
- Finalización automática de la partida al responder correctamente la última pregunta.

### Excluido

- Visualización de resultado en tiempo real vía SignalR (pendiente para integración futura).
- Modalidad por equipos (pendiente para HU-27).
- Historial persistente de respuestas (se resuelve desde datos existentes).

## Business rules

| ID | Regla |
|---|---|
| RB-T25 | La pregunta activa se cierra para todos cuando algún jugador responde correctamente o cuando se agota el tiempo límite. |
| RB-T26 | Al cerrarse una pregunta, el sistema debe mostrar la respuesta correcta a todos los participantes, incluyendo a quienes no alcanzaron a responder. |
| RB-T27 | Al cerrarse una pregunta, el sistema avanza automáticamente a la siguiente pregunta si existe. |
| RB-T28 | El puntaje se otorga únicamente cuando la respuesta es correcta. |
| RB-T29 | El puntaje de una respuesta correcta debe ser igual al puntaje asignado a la pregunta. |

## API / Events

### HTTP endpoints

| Método | Ruta | Auth | Propósito |
|---|---|---|---|
| GET | `/api/trivia-games/{id}/questions/{preguntaId}/result` | Participante | Obtener resultado de pregunta cerrada |

### Response (200 OK)

```json
{
  "partidaId": "uuid",
  "preguntaId": "uuid",
  "respuestaCorrecta": "string",
  "respuestaSeleccionada": "string",
  "esCorrecta": true,
  "puntajeObtenido": 100,
  "tiempoEmpleadoSegundos": 12.5,
  "motivoCierre": "CorrectAnswer | TimeExpired",
  "fechaCierre": "2026-05-31T00:00:00Z"
}
```

### Events publicados

- `PreguntaTriviaCerradaDomainEvent` (in-process) — cuando se cierra una pregunta por respuesta correcta o tiempo agotado.

## Criterios de aceptación

- [ ] CA-01: Un participante puede consultar el resultado de una pregunta cerrada y ver la respuesta correcta, su respuesta, puntaje, tiempo empleado y motivo de cierre.
- [ ] CA-02: Si el participante no respondió, `respuestaSeleccionada` retorna información indicando que no hubo respuesta.
- [ ] CA-03: Si la pregunta sigue activa, el endpoint retorna 400.
- [ ] CA-04: Al responder correctamente, se avanza a la siguiente pregunta automáticamente.
- [ ] CA-05: Al responder correctamente la última pregunta, la partida finaliza (estado Terminada).
- [ ] CA-06: Responder incorrectamente no cierra la pregunta ni avanza.

## Tests

| Tipo | Cantidad | Descripción |
|---|---|---|
| Unit (dominio) | 5 | CerrarPreguntaActual domain event, AvanzarPregunta estados válido/inválido, FinalizarPartida estados válido/inválido |
| Unit (handler) | 5 | GetQuestionResultQueryHandler: cerrada por tiempo, cerrada por correcta, pregunta activa, partida no encontrada, formulario no encontrado |
| API (integración) | 4 | GET result tras respuesta correcta, pregunta activa -> 400, game not found -> 404, no autenticado -> 401 |
