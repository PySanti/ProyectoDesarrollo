# Trivia Game API Contract

Owning service: Trivia Game Service

## Status

HU-15, HU-17, HU-18, HU-22, HU-23, HU-24 and HU-26 endpoints are implemented. Endpoint details for future HUs must be completed feature by feature in the related SDD before implementation.

## Base path

```
/api/trivia-forms
```

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

---

## POST /api/trivia-forms

Create a new Trivia form.

| Field | Value |
|---|---|
| Related HU | HU-15 |
| Related requirements | RF-15, RF-16, RB-T01, RB-T02, RB-T03 |
| Authorization | Operador |
| Type | Command |

### Request

```json
{
  "title": "string",
  "questions": [
    {
      "text": "string (1-300 caracteres)",
      "assignedScore": "int (1-1000)",
      "timeLimitSeconds": "int (5-300)",
      "displayOrder": "int (1-based, ≥1)",
      "options": [
        {
          "text": "string (1-150 caracteres)",
          "isCorrect": "bool (exactamente 1 true por pregunta)"
        }
      ]
    }
  ]
}
```

Constraints:
- `questions` array must have at least 1 element.
- Each question must have exactly 4 options.
- Exactly one option per question must have `isCorrect: true`.
- `assignedScore` per question: 1-1000.
- `timeLimitSeconds` per question: 5-300.

### Response

**201 Created**

```json
{
  "id": "uuid",
  "title": "string",
  "isComplete": true,
  "incompleteReasons": [],
  "createdAtUtc": "2026-05-31T00:00:00Z",
  "updatedAtUtc": "2026-05-31T00:00:00Z",
  "questions": [
    {
      "id": "uuid",
      "text": "string",
      "assignedScore": 100,
      "timeLimitSeconds": 30,
      "displayOrder": 0,
      "options": [
        { "index": 0, "text": "string", "isCorrect": true },
        { "index": 1, "text": "string", "isCorrect": false },
        { "index": 2, "text": "string", "isCorrect": false },
        { "index": 3, "text": "string", "isCorrect": false }
      ]
    }
  ]
}
```

**Headers**: `Location: /api/trivia-forms/{id}`

### Error responses

| Status | Reason |
|---|---|
| 400 | Invalid request data / business rule violation |
| 401 | Unauthenticated |
| 403 | Authenticated user is not Operador |
| 500 | Unexpected error |

### Events published

- None

### Real-time updates

- None

---

## GET /api/trivia-games

List all published Trivia games (state = Lobby). Optionally filter by modalidad.

| Field | Value |
|---|---|
| Related HU | HU-09, HU-11 |
| Related requirements | RF-05 |
| Authorization | Authenticated (Participante u Operador) |
| Type | Query |

### Query parameters

| Parameter | Type | Required | Description |
|---|---|---|---|
| modalidad | string | No | Filter by modality. Valid values: `Individual`, `Equipo` (case-insensitive). |

### Request

```
GET /api/trivia-games
GET /api/trivia-games?modalidad=Individual
GET /api/trivia-games?modalidad=Equipo
```

### Response

**200 OK**

```json
[
  {
    "id": "uuid",
    "nombre": "string",
    "modalidad": "Individual | Equipo",
    "estado": "Lobby",
    "tiempoInicio": "ISO 8601 DateTimeOffset",
    "minimoParticipantes": 1,
    "maximoJugadores": 10,
    "maximoEquipos": null
  }
]
```

### Error responses

| Status | Reason |
|---|---|
| 400 | Invalid modalidad value |
| 401 | Unauthenticated |
| 500 | Unexpected error |

### Events published

- None

### Real-time updates

- None

---

## PUT /api/trivia-forms/{id}

Replace the title and all questions of an existing Trivia form.

| Field | Value |
|---|---|
| Related HU | HU-15 |
| Related requirements | RF-15, RF-16, RB-T01, RB-T02, RB-T03 |
| Authorization | Operador |
| Type | Command |

### Request

Same body as POST.

Path parameter: `id` (guid) must match `formId` in body.

```json
{
  "formId": "uuid (must match path id)",
  "title": "string",
  "questions": [ "... same shape as POST ..." ]
}
```

### Response

**200 OK** — Returns the full updated form:

```json
{ "... same shape as POST 201 response ..." }
```

### Error responses

| Status | Reason |
|---|---|
| 400 | Path id does not match body formId / invalid data |
| 401 | Unauthenticated |
| 403 | Authenticated user is not Operador |
| 404 | Form not found |
| 500 | Unexpected error |

### Events published

- None

### Real-time updates

- See SignalR hub section below for real-time lobby events.

---

## GET /api/trivia-games/{id}/lobby

Get the lobby status (waiting screen data) for an active Trivia game.

| Field | Value |
|---|---|
| Related HU | HU-21 |
| Related requirements | RF-08, RF-13 |
| Authorization | Participant (must be registered in the game) |
| Type | Query |

### Response

**200 OK**

```json
{
  "partidaId": "uuid",
  "nombre": "string",
  "estado": "Lobby",
  "modalidad": "Individual | Equipo",
  "tiempoInicio": "ISO 8601 DateTimeOffset",
  "minimoParticipantes": 1,
  "maximoJugadores": 10,
  "participantesActual": 3,
  "participantes": [
    {
      "inscripcionId": "uuid",
      "usuarioId": "string",
      "fechaInscripcion": "2026-05-31T00:00:00Z"
    }
  ]
}
```

### Error responses

| Status | Reason |
|---|---|
| 401 | Unauthenticated |
| 403 | Authenticated user is not registered for this game |
| 404 | Game not found |
| 500 | Unexpected error |

### Events published

- None

### Real-time updates

- See SignalR hub section below.

---

## SignalR Hub: /hubs/trivia-lobby

| Field | Value |
|---|---|
| Related HU | HU-21 |
| Related requirements | RF-13 |
| Owning service | Trivia Game Service |
| Hub path | `/hubs/trivia-lobby` |

### Client methods (events sent from server)

| Event name | Payload | Trigger |
|---|---|---|
| `ParticipantJoined` | `{ partidaId: guid, usuarioId: string }` | When a participant joins the game lobby |
| `GameStarted` | `{ partidaId: guid }` | When game transitions from Lobby to Iniciada |
| `GameCancelled` | `{ partidaId: guid }` | When game is cancelled |

### Client-side groups

| Group name | Usage |
|---|---|
| `game-{partidaId}` | Clients join this group to receive lobby updates for a specific game |

### Invocable client methods

| Method | Parameters | Description |
|---|---|---|
| `JoinGameGroup` | `gameId: string` | Subscribe to lobby events for a specific game |
| `LeaveGameGroup` | `gameId: string` | Unsubscribe from lobby events |

---

## GET /api/trivia-forms/{id}

Get a Trivia form by its unique identifier.

| Field | Value |
|---|---|
| Related HU | HU-15 |
| Related requirements | RF-15, RF-16 |
| Authorization | Operador |
| Type | Query |

### Response

**200 OK**

```json
{ "... same shape as POST 201 response ..." }
```

**404 Not Found** — When form does not exist.

### Error responses

| Status | Reason |
|---|---|
| 401 | Unauthenticated |
| 403 | Authenticated user is not Operador |
| 404 | Form not found |
| 500 | Unexpected error |

### Events published

- None

### Real-time updates

- None

---

## POST /api/trivia-games/{id}/questions/{preguntaId}/answer

Submit an answer to an active Trivia question (individual mode only).

| Field | Value |
|---|---|
| Related HU | HU-26 |
| Related requirements | RF-20, RF-21, RF-22, RB-T11, RB-T14, RB-T15, RB-T16, RB-T21, RB-T24, RB-T25, RB-T28 |
| Authorization | Participante autenticado |
| Type | Command |

### Request

```json
{
  "opcionIndex": "int (0-based index of selected option)"
}
```

### Response

**200 OK**

```json
{
  "respuestaId": "uuid",
  "partidaId": "uuid",
  "preguntaId": "uuid",
  "esCorrecta": true,
  "puntajeObtenido": 100,
  "fechaRespuesta": "2026-05-31T00:00:00Z"
}
```

### Error responses

| Status | Reason |
|---|---|
| 400 | Invalid option index / question not in game / duplicate answer / game state invalid / answer too late / time expired |
| 401 | Unauthenticated |
| 404 | Game not found |
| 500 | Unexpected error |

### Business rules enforced

- Answer must be submitted while the game is in `Iniciada` state.
- The requested `preguntaId` must match the game's active question (`PreguntaActualId`).
- The answer must be submitted within the question's `timeLimitSeconds` from when the question was opened.
- Each participant can submit only one answer per question (duplicates are rejected).
- A correct answer closes the question immediately (`PreguntaActualId` becomes null).
- An incorrect answer leaves the question open.
- Score is earned only on correct answers; `puntajeObtenido = pregunta.assignedScore`.
- `puntajeObtenido` is 0 for incorrect answers.

### Events published

- `RespuestaTriviaRegistradaDomainEvent` (in-process, for potential ranking/history updates)

### Real-time updates

- None (pending SignalR integration)

---

## POST /api/trivia-games/{id}/join

Join an individual participant to a Trivia game.

| Field | Value |
|---|---|
| Related HU | HU-18 |
| Related requirements | RF-05, RF-06, RF-11, RF-18, RB-T11, RB-T12, RB-08, RB-18, RB-20 |
| Authorization | Participante autenticado |
| Type | Command |

### Response

**200 OK**

```json
{
  "inscripcionId": "uuid",
  "partidaId": "uuid",
  "fechaInscripcion": "2026-05-31T00:00:00Z"
}
```

### Error responses

| Status | Reason |
|---|---|
| 400 | Invalid request data / business rule violation |
| 401 | Unauthenticated |
| 404 | Game not found |
| 409 | Business rule conflict (not in lobby, wrong modality, capacity full, already registered) |
| 500 | Unexpected error |

### Events published

- None

### Real-time updates

- None

---

## POST /api/trivia-games

Create a new Trivia game and publish it in the lobby.

| Field | Value |
|---|---|
| Related HU | HU-17 |
| Related requirements | RF-17, RF-18, RB-T04, RB-T05, RB-T06, RB-T07, RB-T08, RB-T09 |
| Authorization | Operador |
| Type | Command |

### Request

```json
{
  "nombre": "string (1-100 caracteres)",
  "modalidad": "Individual | Equipo",
  "modoInicio": "Manual | Automatico | ManualYAutomatico",
  "formularioId": "uuid (must reference an existing, complete TriviaForm)",
  "tiempoInicio": "ISO 8601 DateTimeOffset (future)",
  "minimoParticipantes": "int (1-1000)",
  "maximoJugadores": "int? (required when modalidad=Individual, 1-1000)",
  "maximoEquipos": "int? (required when modalidad=Equipo, 1-1000)",
  "minimoJugadoresPorEquipo": "int? (required when modalidad=Equipo, 1-5)",
  "maximoJugadoresPorEquipo": "int? (required when modalidad=Equipo, 1-5)"
}
```

### Response

**201 Created**

```json
{
  "id": "uuid",
  "nombre": "string",
  "estado": "Lobby",
  "modalidad": "Individual | Equipo",
  "modoInicio": "Manual | Automatico | ManualYAutomatico",
  "formularioId": "uuid",
  "tiempoInicio": "ISO 8601 DateTimeOffset",
  "minimoParticipantes": 1,
  "maximoJugadores": 10,
  "maximoEquipos": null,
  "minimoJugadoresPorEquipo": null,
  "maximoJugadoresPorEquipo": null,
  "createdAtUtc": "2026-05-31T00:00:00Z",
  "startedAtUtc": null
}
```

**Headers**: `Location: /api/trivia-games/{id}`

### Error responses

| Status | Reason |
|---|---|
| 400 | Invalid request data / business rule violation (e.g., CantidadMinima=0) |
| 401 | Unauthenticated |
| 403 | Authenticated user is not Operador |
| 404 | Referenced TriviaForm not found |
| 500 | Unexpected error |

### Events published

- `TriviaGameCreatedDomainEvent` (in-process)

### Real-time updates

- Lobby update to participants (pending SignalR integration)

---

## GET /api/trivia-games/{id}/participants

Get the list of participants registered in a Trivia game lobby (operator view).

| Field | Value |
|---|---|
| Related HU | HU-22 |
| Related requirements | RF-08, RF-13 |
| Authorization | Operador |
| Type | Query |

### Response

**200 OK**

```json
{
  "partidaId": "uuid",
  "nombre": "string",
  "estado": "Lobby",
  "modalidad": "Individual | Equipo",
  "tiempoInicio": "ISO 8601 DateTimeOffset",
  "minimoParticipantes": 1,
  "maximoJugadores": 10,
  "participantesActual": 2,
  "participantes": [
    {
      "inscripcionId": "uuid",
      "usuarioId": "string",
      "fechaInscripcion": "2026-05-31T00:00:00Z"
    }
  ]
}
```

### Error responses

| Status | Reason |
|---|---|
| 401 | Unauthenticated |
| 403 | Authenticated user is not Operador |
| 404 | Game not found |
| 500 | Unexpected error |

### Events published

- None

### Real-time updates

- Operator subscribes to `/hubs/trivia-lobby` (game-{partidaId} group) for real-time join/start/cancel events.

---

## GET /api/trivia-games/{id}/teams

Get the list of teams registered in a Trivia game lobby (operator view for Equipo modality games).

| Field | Value |
|---|---|
| Related HU | HU-23 |
| Related requirements | RF-08, RF-13 |
| Authorization | Operador |
| Type | Query |

### Response

**200 OK**

```json
[
  {
    "equipoId": "string",
    "fechaInscripcion": "2026-05-31T00:00:00Z"
  }
]
```

### Error responses

| Status | Reason |
|---|---|
| 401 | Unauthenticated |
| 403 | Authenticated user is not Operador |
| 404 | Game not found |
| 500 | Unexpected error |

### Events published

- None

### Real-time updates

- Operator subscribes to `/hubs/trivia-lobby` (game-{partidaId} group) for real-time join/start/cancel events.

---

## POST /api/trivia-games/{id}/start

Start a Trivia game manually. The game must be in `Lobby` state and meet minimum participation requirements. If the game is configured with `ModoInicio = Automatico` only, manual start is rejected with 409.

| Field | Value |
|---|---|
| Related HU | HU-17, HU-24 |
| Related requirements | RF-18, RB-T17, RB-T18, RB-26, RB-27, HU-24-R01, HU-24-R02 |
| Authorization | Operador |
| Type | Command |

### Response

**200 OK** — Returns the updated game in `Iniciada` state with `startedAtUtc` populated.

### Error responses

| Status | Reason |
|---|---|
| 401 | Unauthenticated |
| 403 | Authenticated user is not Operador |
| 404 | Game not found |
| 409 | Minimum participants not met / invalid state transition |
| 500 | Unexpected error |

### Events published

- `TriviaGameStartedDomainEvent` (in-process)

### Real-time updates

- Game state change to participants (pending SignalR integration)

---

## GET /api/trivia-games/{id}

Get a Trivia game by its unique identifier.

| Field | Value |
|---|---|
| Related HU | HU-17 |
| Related requirements | RF-35 |
| Authorization | Operador |
| Type | Query |

### Response

**200 OK**

```json
{ "... same shape as POST 201 response ..." }
```

**404 Not Found**

### Error responses

| Status | Reason |
|---|---|
| 401 | Unauthenticated |
| 403 | Authenticated user is not Operador |
| 404 | Game not found |
| 500 | Unexpected error |

### Events published

- None

### Real-time updates

- None
