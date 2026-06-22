# HU-44 - Design

## Owning Service

- BDT Game Service.

## Supporting Services

- Identity Service / Keycloak token claims for authenticated role `Participante`.
- PostgreSQL through EF Core inside BDT Game Service.
- React Native mobile as participant client.
- SignalR/WebSocket adapter owned by BDT Game Service for active-stage updates.
- Mobile geolocation permission API for local permission gating.

No Team Service call is required in HU-44. Team membership/leadership validation must already have been resolved by BDT registration flows.

## Domain Model

Entities and value objects involved:

- `PartidaBDT` aggregate root.
- `EtapaBDT` active child entity.
- `ExploradorBDT` active participant/team competitor.
- `EstadoPartida.Iniciada`.
- `EstadoEtapa.Activa`.
- `TiempoLimite` / active-stage start and close timestamps.
- `UbicacionGeografica` permission requirement, without route history.

Domain/read rules:

- Only participants registered in the BDT game can access their active-stage view.
- Active-stage view is read-only.
- Upload availability is derived from backend state, active stage state and participant eligibility.
- Timer display uses backend timestamps.
- No active stage is returned as a business conflict (`409`), not as `200 OK` with `puedeSubirTesoro=false`.

## Resolved Active-Stage Decision

The active-stage HTTP query returns `200 OK` only when BDT Game Service can provide an active stage for the registered participant.

If the game is not `Iniciada`, or if the game is `Iniciada` but there is no active stage, the endpoint returns `409`.

The `puedeSubirTesoro` field remains in the successful response as the backend-authoritative upload availability flag. It must not be used to represent a missing active stage. This avoids a mobile ambiguity where a screen would render a stage payload that does not actually exist.

## Commands

- None. HU-44 is a read-only participant view.

## Queries

### `ObtenerEtapaActivaBdtQuery`

Input:

- `PartidaId`.
- `ParticipanteUserId` from authenticated token claims.

Output:

- Active-stage mobile DTO.

Handler responsibilities:

- Load BDT game, active stage and participant registration.
- Validate participant access.
- Return active-stage data and upload availability.
- Avoid state mutation.

## HTTP Contract

Endpoint documented in `contracts/http/bdt-game-api.md` before implementation:

```txt
GET /api/bdt/games/{partidaId}/active-stage
```

Authorization:

- Authenticated `Participante`.

Request:

- No body.

Response `200 OK`:

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

Errors:

| Status | Reason |
|---|---|
| 400 | Invalid `partidaId` |
| 401 | Unauthenticated |
| 403 | Authenticated user is not participant or has invalid `sub` claim |
| 404 | BDT game not found |
| 409 | Game is not `Iniciada` or there is no active stage |
| 500 | Persistence failure |

## Events

RabbitMQ integration events:

- None required for HU-44 closure because this is a read-only query.

User-visible real-time subscriptions:

- Mobile subscribes to the documented HU-43 `PartidaBDTIniciada` SignalR/WebSocket message to refresh active-stage state after a game starts.
- Stage-closed, stage-advanced and game-cancelled message names are deferred to HU-47/HU-53 unless those contracts are approved before implementation.
- HU-44 mobile code must not invent event names. It may implement a reusable refresh/invalidation handler that is wired only to documented messages.

## Real-Time Updates

- Required for a good active-stage experience because the start transition is user-visible and stage changes will be user-visible when their owning contracts exist.
- For HU-44 closure, the mobile screen must refresh active-stage state when `PartidaBDTIniciada` is received.
- The mobile screen must be structured to handle future refresh/invalidation when a documented active-stage change or close message is introduced by HU-47.
- Backend remains authoritative; real-time messages trigger refresh or update local display state.

## Mobile Runtime Hardening Requirements

- Geolocation permission must use a React Native-compatible adapter approved for the project runtime, not browser-only `navigator.geolocation` behavior.
- The permission adapter must expose granted, denied and unavailable states so the screen can show a clear participant-facing message.
- The countdown must be live while the screen is mounted and must be derived from backend `cierraEnUtc` timestamps.
- The integrated `BdtActiveStage` route must wire `Subir tesoro` to the HU-45 upload route or to an explicit HU-45 handoff placeholder until HU-45 implementation is completed.
- Team-modality active-stage access remains deferred to HU-40 unless a documented Team Service validation/read mapping is added before implementation.

## Design Patterns Applied

| Pattern | Location | Problem solved | Justification |
|---|---|---|---|
| CQRS + Mediator | `ObtenerEtapaActivaBdtQuery` and handler | Keeps read-only view separate from commands | Required project architecture |
| Repository / Query Port | BDT application layer | Isolates persistence and read model projection | Clean/hexagonal architecture requirement |
| DTO / Read Model | Active-stage response | Avoids exposing domain entities to mobile | Stable participant contract |
| Adapter / Observer | SignalR subscription in mobile/backend adapter | Keeps mobile synced with backend state changes | Required for user-visible real-time updates |
| Permission Gate | Mobile geolocation permission module | Enforces SRS active BDT participation precondition at UX boundary | Backend remains authoritative; mobile handles device permission |

## Tests Required

Unit/application tests:

- Query returns active stage for registered participant in initiated BDT.
- Query rejects missing game.
- Query rejects not-started/cancelled/terminated games.
- Query rejects unregistered participant.
- Query rejects initiated games without active stage with `409`.
- Query returns `puedeSubirTesoro` from backend state when an active stage is available.

Integration/API tests:

- `GET /api/bdt/games/{partidaId}/active-stage` returns `200` for valid participant.
- Returns `400`, `401`, `403`, `404` and `409` for documented cases.
- Query does not mutate database state.

Contract tests:

- Response shape matches `contracts/http/bdt-game-api.md` once updated.

Mobile tests:

- Screen renders active stage, timer and upload action.
- Screen blocks active participation when geolocation permission is denied.
- Screen uses a React Native-compatible geolocation permission adapter for granted, denied and unavailable states.
- Screen updates countdown over time using fake timers and backend close timestamp.
- Integrated route/container navigates to HU-45 upload flow, or approved HU-45 handoff placeholder, when upload action is pressed.
- Screen handles loading, error and closed-stage states.
- Screen handles the `409` no-active-stage conflict as an unavailable-stage state without showing the upload action.

Real-time tests:

- Mobile refreshes active-stage state on documented `PartidaBDTIniciada` message.
- Mobile does not subscribe to undocumented stage-closed or cancellation messages.

PostgreSQL tests:

- Query reads active stage and participant registration from Npgsql with isolated schema.
