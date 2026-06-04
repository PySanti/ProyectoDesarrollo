# Trivia Game API Contract

Owning service: Trivia Game Service

## Status

Endpoint details must be completed feature by feature in the related SDD before implementation.

## Trivia scoring rule

Any endpoint that submits, validates or closes a Trivia answer must respect:

```txt
scoreEarned = question.assignedScore
```

Time must not be included in score calculation.

The timer can still be used to reject late answers.

## Team modality rule

When Trivia is team-based:

- the team is represented as the active Trivia participant;
- Team Service validates team existence, state and leadership;
- a team can have 1 to 5 members.

## GET /api/trivia-forms

Related HU:

- HU-17

Related requirement:

- RF-15, RF-16

Authorization:

- Authenticated operator (`Operador`) or administrator.

Request:

- No body.

Response (`200 OK`):

```json
[
  {
    "id": "uuid",
    "title": "Formulario de prueba",
    "isComplete": true,
    "questionsCount": 5,
    "createdAtUtc": "2026-01-01T00:00:00Z"
  }
]
```

Error responses:

| Status | Reason |
|---|---|
| 401 | Unauthenticated |
| 403 | Unauthorized |
| 500 | Persistence failure |

Business rules:

- Only returns forms created by the Trivia Game Service. IsComplete filter applied client-side or server-side.

Events published:

- none

Real-time updates:

- none

## POST /api/trivia-games

Related HU:

- HU-17

Related requirement:

- RF-17, RF-18, RB-T04, RB-T05, RB-T06, RB-T07, RB-T08, RB-T09

Authorization:

- Authenticated operator (`Operador`).

Request:

```json
{
  "nombre": "Trivia Semanal",
  "modalidad": "Individual",
  "modoInicio": "Manual",
  "formularioId": "uuid",
  "tiempoInicio": "2026-12-31T23:59:59Z",
  "minimoParticipantes": 2,
  "maximoJugadores": 20,
  "maximoEquipos": null,
  "minimoJugadoresPorEquipo": null,
  "maximoJugadoresPorEquipo": null
}
```

Response (`201 Created`):

```json
{
  "id": "uuid",
  "nombre": "Trivia Semanal",
  "estado": "Lobby",
  "modalidad": "Individual",
  "modoInicio": "Manual",
  "formularioId": "uuid",
  "tiempoInicio": "2026-12-31T23:59:59Z",
  "minimoParticipantes": 2,
  "maximoJugadores": 20,
  "maximoEquipos": null,
  "minimoJugadoresPorEquipo": null,
  "maximoJugadoresPorEquipo": null,
  "createdUtc": "2026-06-01T00:00:00Z",
  "startedAtUtc": null
}
```

Error responses:

| Status | Reason |
|---|---|
| 400 | Invalid request (invalid modalidad, modoInicio, missing required fields per modality, incomplete form) |
| 401 | Unauthenticated |
| 403 | Unauthorized (not operator) |
| 404 | FormularioId not found |
| 409 | Business rule conflict |
| 500 | Persistence failure |

Business rules:

- Only an operator can create Trivia games.
- The game is created in state `Lobby`.
- FormularioId must reference an existing and complete form.
- Modalidad must be `Individual` or `Equipo`.
- ModoInicio must be `Manual`, `Automatico` or `ManualYAutomatico`.
- MinimoParticipantes must be ≥ 1.
- If Individual: MaximoJugadores is required and ≥ MinimoParticipantes.
- If Equipo: MaximoEquipos is required; MinimoJugadoresPorEquipo and MaximoJugadoresPorEquipo are optional.
- TiempoInicio must be a future date.

Events published:

- `TriviaGameCreatedDomainEvent` (in-process domain event, not RabbitMQ)

Real-time updates:

- none (lobby real-time updates handled by HU-21)
