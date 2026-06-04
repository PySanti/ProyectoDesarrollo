# HU-43 - Design

## Owning Service

- BDT Game Service.

## Supporting Services

- Identity Service / Keycloak token claims for authenticated role `Operador`.
- PostgreSQL through EF Core inside BDT Game Service.
- React web as operator client.
- SignalR/WebSocket adapter owned by BDT Game Service for user-visible BDT state/stage updates.

Team Service is not called by HU-43. Team validity for team registrations belongs to HU-40/HU-41 and must be represented as BDT Game Service registration state before start.

## Domain Model

Entities and value objects involved:

- `PartidaBDT` aggregate root.
- `EtapaBDT` child entity.
- `ExploradorBDT` active lobby/participant registration.
- `EstadoPartida`: `Lobby`, `Iniciada`.
- `EstadoEtapa`: `Pendiente`, `Activa`.
- `ModoInicioPartida`: `Manual`, `Automatico`, `ManualYAutomatico`.
- Timer fields for active stage: `EtapaIniciadaEnUtc`, `EtapaCierraEnUtc` or equivalent persisted values.

Domain invariants:

- A BDT can start only from `Lobby`.
- A manually-triggered operator start is allowed only for `Manual` or `ManualYAutomatico`.
- Configured minimum participation must be satisfied before state transition.
- Starting activates exactly one stage: the first stage by order.
- Starting does not create numeric BDT score or ranking state.

## Commands

### `IniciarPartidaBdtCommand`

Input:

- `PartidaId`.
- `OperadorUserId` from authenticated context.
- `StartTrigger`, fixed as `Manual` for the HTTP operator endpoint.

Output:

- Started BDT game summary with active-stage timer data.

Handler responsibilities:

- Load `PartidaBDT` with stages and current BDT registrations.
- Invoke a domain method such as `PartidaBDT.IniciarManualmente(...)`.
- Persist aggregate state transition and first-stage activation atomically.
- Publish user-visible real-time update after persistence succeeds.
- Catch and log post-commit real-time dispatch failures without rolling back state or changing a successful HTTP result into `500`.
- Return response DTO.

Reliability and concurrency requirements for 10/10 hardening:

- Serialize start attempts for the same `PartidaBDT` using a PostgreSQL transaction/advisory lock or an EF Core concurrency token.
- If two operator requests attempt to start the same game concurrently, exactly one can commit the `Lobby` to `Iniciada` transition.
- Competing requests must observe the committed transition and return the documented `409` conflict.
- The first stage must be activated once and only once.

## Resolved Reliability Decision

SignalR/WebSocket dispatch is non-authoritative and occurs after the database commit.

If persistence succeeds and the SignalR/WebSocket adapter fails:

- the command remains successful;
- the BDT game remains `Iniciada`;
- the first stage remains active;
- the HTTP endpoint returns the same `200 OK` response;
- the failure is logged for operational follow-up;
- clients recover by querying the active-stage endpoint planned by HU-44.

This avoids the inconsistent behavior where a persisted start transition would return `500` and a retry would then fail with `409` because the game is no longer in `Lobby`.

## Queries

- None required to start the game.
- A read-only subscription authorization check is required for the SignalR hub before adding a connection to a partida group.
- Operator lobby/detail read models are owned by HU-42/HU-38 if needed.

### `AutorizarSuscripcionPartidaBdtQuery` or equivalent port

Input:

- `PartidaId` requested by the SignalR subscription.
- `UserId` from authenticated token claims.
- Authenticated role claims: `Operador` or `Participante`.

Output:

- Authorized / rejected decision.

Rules:

- `Operador` users may subscribe for operator supervision in HU-43.
- `Participante` users may subscribe only if BDT Game Service has an active registration/explorer for that `PartidaId` and user.
- Missing or invalid `sub` claim rejects subscription.
- Missing game rejects subscription.
- The check is read-only and must not mutate BDT state.
- The hub must delegate to this query/port and must not implement BDT business rules inline.

## HTTP Contract

Endpoint documented in `contracts/http/bdt-game-api.md` before implementation:

```txt
POST /api/bdt/games/{partidaId}/start
```

Authorization:

- Authenticated `Operador`.

Request:

- No body. The operator id is taken from authenticated token claims.

Response `200 OK`:

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

Errors:

| Status | Reason |
|---|---|
| 400 | Invalid `partidaId` |
| 401 | Unauthenticated |
| 403 | Authenticated user is not operator or has invalid `sub` claim |
| 404 | BDT game not found |
| 409 | Game is not in `Lobby`, minimum participation is not met, no stages exist, or manual start is not allowed by `modoInicio` |
| 500 | Persistence failure or unexpected failure before the start transition is committed |

## Events

RabbitMQ integration events:

- None required for HU-43 closure unless a future cross-service consumer is approved.

Domain/history event candidate inside BDT Game Service:

- `PartidaBDTIniciada` with `partidaId`, `operadorId`, `modoInicio`, `etapaActivaId`, `occurredOnUtc`.

User-visible real-time update:

- `PartidaBDTIniciada` SignalR/WebSocket message owned by BDT Game Service.
- Payload must include `partidaId`, `estado`, `etapaActiva`, `iniciadaEnUtc`, `cierraEnUtc`.
- Hubs/adapters must not contain business rules.
- `/hubs/bdt` must require authenticated connections.
- The adapter must target partida-scoped groups or an equivalent authorized observer set; it must not broadcast `PartidaBDTIniciada` to all connected clients.
- Group membership must be based on already-authorized user/partida access and must not duplicate domain decisions inside the hub.
- `SubscribeToPartida(partidaId)` must reject authenticated participants who are not registered/active in that BDT game; authentication alone is not enough to join the group.

## Real-Time Updates

- Required for HU-43 completion because participants need to move from lobby/waiting state into active-stage state.
- SignalR update is emitted after database commit.
- Clients must use backend timestamps for timers.
- If dispatch fails after commit, the failure is logged and the endpoint still returns success. Recovery uses the HU-44 active-stage query.

## Design Patterns Applied

| Pattern | Location | Problem solved | Justification |
|---|---|---|---|
| CQRS + Mediator | `IniciarPartidaBdtCommand` and handler | Encapsulates state-changing use case | Required project architecture |
| Aggregate Method | `PartidaBDT.IniciarManualmente(...)` | Protects state transition, minimums and stage activation | BDT lifecycle rules belong to the aggregate |
| Repository | BDT application port and EF Core implementation | Isolates persistence | Clean/hexagonal architecture requirement |
| Adapter / Observer | SignalR adapter | Notifies already-accepted backend state to clients | Real-time updates are user-visible but not authoritative |
| Domain Event | `PartidaBDTIniciada` candidate | Captures state transition fact | Enables history/real-time mapping without controller rules |

## Tests Required

Unit tests:

- Starting a `Lobby` BDT with enough participants succeeds.
- Starting a non-lobby BDT fails.
- Starting without minimum participation fails.
- Starting a strictly `Automatico` BDT through manual operator path fails.
- Starting activates only the first stage.

Application tests:

- Handler persists state transition and active stage.
- Handler maps not-found and conflicts correctly.
- Handler calls real-time port only after persistence succeeds.
- Handler does not fail the command when the post-commit real-time port throws; it logs the failure and returns the persisted state response.

Integration/API tests:

- `POST /api/bdt/games/{partidaId}/start` returns `200` for valid operator request.
- Returns `400`, `401`, `403`, `404` and `409` for documented cases.
- Database contains `Iniciada` state and active first stage after success.

Contract tests:

- Request and response shape match `contracts/http/bdt-game-api.md` once updated.

Frontend web tests:

- Operator start button calls documented endpoint.
- Loading, success and business error states render correctly.

Real-time tests:

- BDT real-time adapter is invoked with the documented payload after successful start.
- No update is emitted when start fails.
- Real-time dispatch failure after commit does not roll back persisted state and does not change the successful response into an error.
- Hub authorization rejects unauthenticated connections.
- `PartidaBDTIniciada` is delivered only to the partida-scoped group or authorized observer target.
- Authenticated but unregistered participants cannot subscribe to another BDT game's partida group.
- Registered/active participants can subscribe to their own BDT game's partida group.
- Operators can subscribe to BDT partida updates for supervision.
- The real adapter payload matches `contracts/events/bdt-game-events.md`.

PostgreSQL tests:

- State transition and stage activation are persisted with Npgsql using isolated schema.
- Concurrent start attempts for the same game produce exactly one success and one or more documented `409` conflicts.
