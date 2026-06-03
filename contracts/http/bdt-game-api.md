# BDT Game API Contract

Owning service: BDT Game Service

## Status

Endpoint details must be completed feature by feature in the related SDD before implementation.

## GET /api/bdt/games/published

Related HU:

- HU-10
- HU-12

Related requirement:

- RF-05
- RF-35
- RNF-01
- RNF-04
- RNF-06
- RNF-13
- RNF-20

Authorization:

- Authenticated participant (`Participante`).

Query parameters:

| Name | Required | Values | Related HU | Description |
|---|---|---|---|---|
| `modalidad` | No | `Individual`, `Equipo` | HU-12 | When omitted, returns all published BDT games. |

Request:

- No body.

Response (`200 OK`):

```json
[
  {
    "partidaId": "uuid",
    "nombre": "string",
    "modalidad": "Individual | Equipo",
    "estado": "Lobby",
    "areaBusqueda": "string",
    "cantidadEtapas": 1
  }
]
```

Error responses:

| Status | Reason |
|---|---|
| 400 | Invalid `modalidad` query parameter |
| 401 | Unauthenticated |
| 403 | Authenticated user without participant authorization/policy |
| 500 | Persistence failure |

Business rules:

- Return only BDT games published for participants, represented by state `Lobby`.
- Without `modalidad`, return both individual and team BDT games.
- With `modalidad=Individual`, return only individual BDT games.
- With `modalidad=Equipo`, return only team BDT games.
- Listing and filtering do not create inscriptions and do not validate team leadership.

Events published:

- none required for HU-10 or HU-12 closure.

Real-time updates:

- none required for HU-10 or HU-12 closure.

## POST /api/bdt/games

Related HU:

- HU-34

Related requirement:

- RF-25
- RF-26
- RF-27
- RF-35
- RF-36
- RNF-01
- RNF-02
- RNF-04
- RNF-06
- RNF-13

Authorization:

- Authenticated operator (`Operador`).

Request:

```json
{
  "nombre": "Busqueda QR Campus",
  "areaBusqueda": "Patio central y biblioteca",
  "modalidad": "Individual",
  "minimoParticipantes": 2,
  "maximoParticipantes": 20,
  "maximoEquipos": null,
  "minimoJugadoresPorEquipo": null,
  "modoInicio": "Manual",
  "etapas": [
    {
      "orden": 1,
      "codigoQrEsperado": "QR-ETAPA-1",
      "tiempoLimiteSegundos": 300
    }
  ]
}
```

Response (`201 Created`):

```json
{
  "partidaId": "uuid",
  "nombre": "Busqueda QR Campus",
  "modalidad": "Individual",
  "estado": "Lobby",
  "areaBusqueda": "Patio central y biblioteca",
  "modoInicio": "Manual",
  "cantidadEtapas": 1
}
```

Error responses:

| Status | Reason |
|---|---|
| 400 | Invalid request payload, empty stage list, invalid expected QR, non-positive stage time limit, or invalid textual area/name |
| 401 | Unauthenticated |
| 403 | Authenticated user without operator authorization/policy |
| 409 | Invalid modality-specific limits, such as individual game without maximum players or team game without maximum teams/minimum players per team |
| 500 | Persistence failure |

Business rules:

- Only an operator can create BDT games.
- The created game is a BDT game and is published as state `Lobby` for first-delivery semantics.
- The search area is a textual description only.
- At least one valid stage is required.
- Every stage requires expected textual QR content and a positive time limit in seconds.
- In `Individual` modality, `maximoParticipantes` is required and represents maximum players.
- In `Equipo` modality, `maximoEquipos` and `minimoJugadoresPorEquipo` are required.
- BDT creation must not create numeric accumulated score or BDT ranking state.

Events published:

- none required for HU-34 closure.

Real-time updates:

- none required for HU-34 closure. Real-time publication/lobby updates are deferred to the BDT real-time/lobby stories such as HU-42 or HU-55 unless a later SDD introduces a SignalR contract.

## GET /api/bdt/operator/games/published

Related HU:

- HU-37

Related requirement:

- RF-25
- RF-27
- RF-35
- RNF-01
- RNF-02
- RNF-04
- RNF-06
- RNF-13

Authorization:

- Authenticated operator (`Operador`).

Request:

- No body.

Response (`200 OK`):

```json
[
  {
    "partidaId": "uuid",
    "nombre": "Busqueda QR Campus",
    "modalidad": "Individual | Equipo",
    "estado": "Lobby",
    "areaBusqueda": "Patio central y biblioteca",
    "cantidadEtapas": 3
  }
]
```

Error responses:

| Status | Reason |
|---|---|
| 401 | Unauthenticated |
| 403 | Authenticated user without operator authorization/policy |
| 500 | Persistence failure |

Business rules:

- Return only BDT games published for operator supervision, represented by state `Lobby` in first delivery.
- Return games ordered by `nombre` ascending and then `partidaId` ascending for deterministic operator display.
- This endpoint is read-only and must not create inscriptions or mutate BDT state.
- The web client must not use this endpoint for participant gameplay.
- The query must not calculate BDT ranking or numeric score.

Events published:

- none required for HU-37 closure.

Real-time updates:

- none required for HU-37 closure.

## POST /api/bdt/games/{partidaId}/individual-inscriptions

Related HU:

- HU-39

Related requirement:

- RF-05
- RF-06
- RF-11
- RF-13
- RF-27
- RF-35
- RF-36
- RNF-01
- RNF-02
- RNF-04
- RNF-06
- RNF-13
- RNF-20

Authorization:

- Authenticated participant (`Participante`).

Path parameters:

| Name | Required | Description |
|---|---|---|
| `partidaId` | Yes | BDT game identifier. |

Request:

- No body. The participant user id is taken from authenticated token claims.

Response (`200 OK`):

```json
{
  "partidaId": "uuid",
  "nombre": "Busqueda QR Campus",
  "modalidad": "Individual",
  "estado": "Lobby",
  "inscripcionId": "uuid",
  "participanteUserId": "uuid",
  "posicionEnLobby": 3,
  "mensaje": "Te uniste a la BDT. Espera el inicio de la partida."
}
```

Error responses:

| Status | Reason |
|---|---|
| 400 | Invalid `partidaId` |
| 401 | Unauthenticated |
| 403 | Authenticated user without participant authorization/policy |
| 404 | BDT game not found |
| 409 | Game is not in `Lobby`, modality is not `Individual`, participant is already registered, or individual capacity is full |
| 500 | Persistence failure |

Business rules:

- Individual registration applies only to BDT games in state `Lobby` and modality `Individual`.
- The participant id is authoritative from token claims, not from the request body.
- A participant can register at most once in the same BDT game.
- The number of individual registrations must not exceed the game maximum player capacity.
- Individual BDT join must not call Team Service and must not validate team leadership.
- Successful registration creates a BDT lobby registration represented in implementation as `ExploradorBDT` with competitor type `Usuario`.

Events published:

- none required for HU-39 closure.

Real-time updates:

- none required for HU-39 closure. Operator lobby real-time updates are deferred to HU-42 or HU-55 unless a later SDD introduces a SignalR contract.

## Required Endpoint Template

Use this template for future BDT endpoints after the related SDD confirms request, response, authorization and business rules.

```md
## <METHOD> <PATH>

Related HU:

- <HU-ID>

Related requirement:

- <RF/RNF/RB>

Authorization:

- <role or authenticated user condition>

Request:

- <request body or no body>

Response:

- <response body>

Error responses:

| Status | Reason |
|---|---|
| 400 | Invalid request |
| 401 | Unauthenticated |
| 403 | Unauthorized |
| 404 | Resource not found |
| 409 | Business rule conflict |
| 422 | QR/image cannot be processed when applicable |

Business rules:

- <rules>

Events published:

- <events or none>

Real-time updates:

- <updates or none>
```
