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

## POST /api/bdt/games/{partidaId}/start

Related HU:

- HU-43

Related requirement:

- RF-04
- RF-11
- RF-13
- RF-27
- RF-31
- RF-32
- RF-35
- RF-36
- RNF-01
- RNF-02
- RNF-03
- RNF-04
- RNF-06
- RNF-13
- RNF-17

Authorization:

- Authenticated operator (`Operador`).

Path parameters:

| Name | Required | Description |
|---|---|---|
| `partidaId` | Yes | BDT game identifier. |

Request:

- No body. The operator user id is taken from authenticated token claims.

Response (`200 OK`):

```json
{
  "partidaId": "uuid",
  "nombre": "Busqueda QR Campus",
  "estado": "Iniciada",
  "modalidad": "Individual",
  "etapaActiva": {
    "etapaId": "uuid",
    "orden": 1,
    "tiempoLimiteSegundos": 300,
    "iniciadaEnUtc": "2026-01-01T00:00:00Z",
    "cierraEnUtc": "2026-01-01T00:05:00Z"
  },
  "mensaje": "Partida BDT iniciada."
}
```

Error responses:

| Status | Reason |
|---|---|
| 400 | Invalid `partidaId` |
| 401 | Unauthenticated |
| 403 | Authenticated user without operator authorization/policy or invalid `sub` claim |
| 404 | BDT game not found |
| 409 | Game is not in `Lobby`, minimum participation is not met, no stages exist, or manual start is not allowed by `modoInicio` |
| 500 | Persistence failure or unexpected failure before the start transition is committed |

Business rules:

- Only an operator can manually start a BDT game.
- Manual operator start is allowed only when `modoInicio` is `Manual` or `ManualYAutomatico`.
- The game must be in `Lobby`.
- Configured minimum participation must be satisfied using BDT Game Service registration state.
- Starting the game changes state to `Iniciada` and activates exactly the first stage by order.
- Active-stage timer fields are generated from backend/server time.
- Starting a BDT game must not calculate numeric BDT score or ranking state.
- The endpoint and SignalR hub must not contain business rules.
- The `/hubs/bdt` SignalR hub must require authenticated `Operador` or `Participante` users for HU-43 updates.
- `PartidaBDTIniciada` must be delivered through partida-scoped SignalR groups and must not be broadcast to all connected clients.
- `SubscribeToPartida(partidaId)` must authorize group membership before adding a connection to the partida group.
- Authenticated participants must be registered/active in the requested BDT game before subscribing to that partida group.
- Authenticated operators may subscribe for operator supervision.
- Authentication alone is not enough for a participant to subscribe to a different BDT game.
- A post-commit SignalR/WebSocket dispatch failure does not roll back the start transition and does not convert the already-persisted command into HTTP `500`; it is logged and clients can recover through the active-stage query.

Events published:

- No RabbitMQ integration event is required for HU-43 closure.

Real-time updates:

- After successful persistence, BDT Game Service attempts to emit SignalR/WebSocket message `PartidaBDTIniciada` with the payload documented in `contracts/events/bdt-game-events.md`.
- If the dispatch fails after persistence, the HTTP command still returns `200 OK` with the persisted state response.

## GET /api/bdt/games/{partidaId}/active-stage

Related HU:

- HU-44

Related requirement:

- RF-13
- RF-14
- RF-27
- RF-28
- RF-31
- RF-32
- RF-34
- RF-35
- RF-36
- RNF-01
- RNF-03
- RNF-04
- RNF-06
- RNF-13
- RNF-17
- RNF-19
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
  "estado": "Iniciada",
  "modalidad": "Individual",
  "exploradorId": "uuid",
  "etapaActiva": {
    "etapaId": "uuid",
    "orden": 1,
    "estado": "Activa",
    "tiempoLimiteSegundos": 300,
    "iniciadaEnUtc": "2026-01-01T00:00:00Z",
    "cierraEnUtc": "2026-01-01T00:05:00Z"
  },
  "puedeSubirTesoro": true,
  "requiereGeolocalizacion": true,
  "mensaje": "Etapa activa disponible."
}
```

Error responses:

| Status | Reason |
|---|---|
| 400 | Invalid `partidaId` |
| 401 | Unauthenticated |
| 403 | Authenticated user without participant authorization/policy, invalid `sub` claim, or participant is not registered/active in the BDT game |
| 404 | BDT game not found |
| 409 | Game is not `Iniciada` or there is no active stage |
| 500 | Persistence/read failure |

Business rules:

- The query is read-only and must not mutate BDT state.
- Only a participant registered/active in the BDT game can view the participant active-stage state.
- The game must be `Iniciada`.
- The game must have an active stage.
- Missing active stage is a business conflict (`409`), not a `200 OK` response with a synthetic empty stage.
- Active-stage timer fields come from backend/server timestamps.
- `puedeSubirTesoro` is derived by BDT Game Service and controls whether the mobile upload action is shown/enabled.
- The mobile app must request/check geolocation permission before enabling active BDT participation, but backend remains authoritative for BDT state and upload availability.
- The mobile app must not decode or validate QR content in HU-44.

Events published:

- No RabbitMQ integration event is required for HU-44 closure because this endpoint is a read-only query.

Real-time updates:

- The mobile app may refresh this query after receiving the documented `PartidaBDTIniciada` SignalR/WebSocket message.
- Stage-close, stage-advance and cancellation real-time message names are deferred to their owning HUs unless documented before implementation.

## POST /api/bdt/games/{partidaId}/stages/{etapaId}/treasures

Related HU:

- HU-45

Related requirement:

- RF-13
- RF-28
- RF-29
- RF-30
- RF-31
- RF-35
- RF-36
- RNF-01
- RNF-02
- RNF-03
- RNF-04
- RNF-06
- RNF-13
- RNF-16
- RNF-17
- RNF-18
- RNF-19
- RNF-20

Authorization:

- Authenticated participant (`Participante`).

Path parameters:

| Name | Required | Description |
|---|---|---|
| `partidaId` | Yes | BDT game identifier. |
| `etapaId` | Yes | BDT stage identifier. Must match the active stage. |

Request:

- `multipart/form-data`.
- Required field `image`: QR treasure image captured or selected from mobile.
- Accepted media types: `image/jpeg`, `image/png`.
- Maximum image size: `5 MB`.
- The participant user id is taken from authenticated token claims; it must not be accepted from request fields.

Response (`201 Created`):

```json
{
  "tesoroId": "uuid",
  "partidaId": "uuid",
  "etapaId": "uuid",
  "exploradorId": "uuid",
  "fechaEnvioUtc": "2026-01-01T00:03:00Z",
  "estadoProcesamiento": "Decodificado | NoLegible",
  "qrDecodificado": "QR-ETAPA-1",
  "mensaje": "Tesoro recibido para validacion."
}
```

Unreadable QR response example (`201 Created`):

```json
{
  "tesoroId": "uuid",
  "partidaId": "uuid",
  "etapaId": "uuid",
  "exploradorId": "uuid",
  "fechaEnvioUtc": "2026-01-01T00:03:00Z",
  "estadoProcesamiento": "NoLegible",
  "qrDecodificado": null,
  "mensaje": "Tesoro recibido, pero no se pudo leer un QR en la imagen."
}
```

Error responses:

| Status | Reason |
|---|---|
| 400 | Invalid `partidaId`, invalid `etapaId`, missing `image` field or invalid multipart metadata |
| 401 | Unauthenticated |
| 403 | Authenticated user without participant authorization/policy, invalid `sub` claim, or participant is not registered/active in the BDT game |
| 404 | BDT game or stage not found |
| 409 | Game is not `Iniciada`, stage is not active, stage is closed, stage does not match the current active stage, or attempts are no longer allowed |
| 413 | Image exceeds `5 MB` |
| 415 | Image media type is not `image/jpeg` or `image/png` |
| 500 | Persistence, storage or decoder infrastructure failure |

Business rules:

- Only a participant registered/active in the BDT game can upload a treasure for that game.
- The game must be `Iniciada`.
- The `etapaId` path parameter must match the active stage.
- Upload attempts are accepted only while the stage is active and still accepts attempts.
- Multiple attempts are allowed while the stage remains active and unsolved.
- Each accepted upload attempt is recorded by BDT Game Service with game, active stage, explorer, image reference, timestamp, processing status and decoded QR content when available.
- When QR decoding cannot read a QR, the attempt is still recorded with `estadoProcesamiento = NoLegible` and `qrDecodificado = null`.
- HU-45 does not perform final QR comparison against the expected QR, does not close stages and does not update BDT ranking.
- The mobile app must not decode or validate QR content authoritatively.
- The endpoint must not contain business rules; rules belong in the BDT application/domain model.

Events published:

- No RabbitMQ integration event is required for HU-45 closure.

Real-time updates:

- No new SignalR/WebSocket message is required for HU-45 closure.
- QR validation, stage close, stage advance and operator treasure supervision real-time messages are deferred to their owning HUs unless documented before implementation.

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
