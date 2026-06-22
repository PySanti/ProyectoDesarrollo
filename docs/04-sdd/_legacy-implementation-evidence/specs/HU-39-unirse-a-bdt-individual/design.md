# HU-39 - Design

## Owning Service

- BDT Game Service.

## Supporting Services

- Identity Service / Keycloak token claims for authenticated role `Participante`.
- PostgreSQL through EF Core inside BDT Game Service.
- React Native mobile as participant client.

Team Service is not required for HU-39 because individual games can be joined regardless of team membership.

## Domain Model

Entities and value objects involved:

- `PartidaBDT` aggregate root.
- `ExploradorBDT` child entity as the BDT lobby/participant registration for individual modality.
- `ExploradorId` value object used as `inscripcionId` in the HTTP response for HU-39.
- `EstadoPartida` with `Lobby`.
- `Modalidad` with `Individual`.
- Participant user id from authenticated token claims.

Domain invariants:

- Only `Lobby` BDT games accept registrations.
- Individual join applies only to `Individual` modality.
- A participant can register at most once in the same BDT game.
- Player capacity cannot be exceeded.
- Player capacity must remain valid under concurrent PostgreSQL-backed joins by different participants.

## Commands

### `UnirseABdtIndividualCommand`

Input:

- `PartidaId`
- `ParticipanteUserId` from authenticated context.

Output:

- Waiting screen DTO.

Handler responsibilities:

- Load BDT game aggregate.
- Validate state, modality, duplicate registration and capacity.
- Execute the registration persistence path with PostgreSQL-safe concurrency control so capacity is checked against locked/current persisted state before commit.
- Add individual `ExploradorBDT` with competitor type `Usuario`.
- Persist changes.
- Return waiting screen data.
- Do not trigger SignalR in HU-39; operator lobby real-time updates are deferred to HU-42 or HU-55.

## Queries

- No standalone query is required to join.
- Waiting screen data can be returned from the command response.

## HTTP Contract

Documented endpoint in `contracts/http/bdt-game-api.md`:

```txt
POST /api/bdt/games/{partidaId}/individual-inscriptions
```

Authorization:

- Authenticated `Participante`.

Request:

- No body. The participant id is taken from token claims.

Response `200 OK`:

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

Errors:

| Status | Reason |
|---|---|
| 400 | Invalid `partidaId` |
| 401 | Unauthenticated |
| 403 | Authenticated user is not participant |
| 404 | BDT game not found |
| 409 | Game is not in lobby, modality is not individual, duplicate registration, or capacity is full |
| 500 | Persistence failure |

## Events

Integration events:

- None required for HU-39 closure unless a later contract requires cross-service notification.

Internal/history event candidate:

- `InscripcionBdtIndividualRegistrada` may be recorded by BDT Game Service history.

## Real-Time Updates

User-visible lobby updates required by RF-13 are explicitly deferred to HU-42 or HU-55.

HU-39 closes through the synchronous HTTP state change plus mobile waiting screen response. Implementation must not invent hub names or SignalR payloads in HU-39.

## 10/10 Hardening Plan

The feature is considered `10/10` because the PostgreSQL persistence path proves the capacity invariant under concurrent joins by different participants.

Completed backend hardening:

- PostgreSQL-safe concurrency control is applied around individual registration for the target `PartidaBDT`.
- The implementation acquires a transaction-scoped PostgreSQL advisory lock keyed by `partidaId` before loading/counting `ExploradorBDT` registrations, then persists in the same transaction.
- Current registrations are loaded after acquiring the lock before applying `RegistrarParticipanteIndividual(...)`, so `MaximoParticipantes` is enforced against current committed state.
- The losing concurrent join maps to the documented `409` capacity-full business response, not `500`.
- Keep duplicate protection through the existing unique index on `(partida_id, competidor_id, tipo_competidor)`.
- Do not add Team Service calls, RabbitMQ events or SignalR updates as part of this hardening.

Evidence:

- A PostgreSQL/Npgsql concurrent test with `MaximoParticipantes = 1` and two different participant ids proves exactly one request succeeds and the other returns `409`.
- The same test asserts the database ends with exactly one `ExploradorBDT` for the game.
- Full BDT unit, integration and contract suites pass after the hardening.

## Design Patterns Applied

| Pattern | Location | Problem solved | Justification |
|---|---|---|---|
| CQRS + Mediator | `UnirseABdtIndividualCommand` and handler | Encapsulates state-changing use case | Required project architecture |
| Aggregate Method | `PartidaBDT.RegistrarParticipanteIndividual(...)` | Protects state, modality, duplicate and capacity invariants | Registration rules belong to the aggregate/domain |
| Repository | BDT application port and EF Core implementation | Isolates persistence | Clean/hexagonal architecture requirement |
| Result Pattern | Domain/application validation result | Maps expected conflicts to `409` | Avoids exceptions for business conflicts |
| DTO / Read Model | Waiting screen response | Returns mobile-ready lobby state without exposing domain entities | Keeps the mobile client aligned with the documented HTTP contract |

## Tests Required

Unit tests:

- Registering in `Lobby` individual BDT with capacity succeeds.
- Registering in non-lobby BDT fails.
- Registering in team modality through individual command fails.
- Duplicate participant registration fails.
- Full individual capacity fails.

Application tests:

- Handler persists successful registration.
- Handler returns waiting screen DTO.
- Handler maps business conflicts correctly.

Integration/API tests:

- `POST /api/bdt/games/{partidaId}/individual-inscriptions` returns `200` for valid participant.
- Returns `401` without authentication.
- Returns `403` for non-participant role.
- Returns `404` for missing game.
- Returns `409` for invalid state/modality/capacity/duplicate.

Contract tests:

- Request/response shape matches `contracts/http/bdt-game-api.md` once updated.

Mobile tests:

- Join button calls API for individual BDT.
- Success navigates to waiting screen.
- Conflict/error messages are shown clearly.

PostgreSQL tests:

- Registration persists using isolated Npgsql test database/schema.
- Duplicate registration remains rejected under database-backed execution.
- Concurrent joins by different participants cannot exceed `MaximoParticipantes`; exactly one succeeds and the other maps to `409`.

## Implementation Status

HU-39 was implemented from `tasks.md` and keeps the approved design decisions:

- the owning service is BDT Game Service;
- the HTTP endpoint is documented in `contracts/http/bdt-game-api.md`;
- the event/no-event decision is documented in `contracts/events/bdt-game-events.md`;
- SignalR is explicitly deferred to HU-42 or HU-55;
- the concrete registration model is `ExploradorBDT` with competitor type `Usuario`;
- Team Service is explicitly out of scope for individual BDT join.

Current review status: `10/10` / implemented / tested / PostgreSQL concurrent capacity hardening verified / mobile-tested / acceptance updated.
