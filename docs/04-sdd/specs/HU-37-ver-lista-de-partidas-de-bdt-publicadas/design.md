# HU-37 - Design

## Owning Service

- BDT Game Service.

## Supporting Services

- Identity Service / Keycloak token claims for authenticated role `Operador`.
- PostgreSQL through EF Core inside BDT Game Service.
- React web as operator client.

## Domain Model

Entities and value objects read by the query:

- `PartidaBDT`.
- `EtapaBDT` for stage count.
- `AreaBusqueda`.
- `EstadoPartida`.
- `Modalidad`.

No aggregate state change is performed by this HU.

## Commands

- None. HU-37 is read-only.

## Queries

### `ListarPartidasBdtPublicadasOperadorQuery`

Input:

- Authenticated operator context.

Output per item:

- `PartidaId`
- `Nombre`
- `Modalidad`
- `Estado`
- `AreaBusqueda`
- `CantidadEtapas`

Handler responsibilities:

- Query BDT Game Service persistence.
- Return only operator-visible published BDT games.
- Return games in deterministic order by `Nombre` ascending and then `PartidaId` ascending.
- Avoid state mutation.

## HTTP Contract

Documented endpoint in `contracts/http/bdt-game-api.md`:

```txt
GET /api/bdt/operator/games/published
```

Authorization:

- Authenticated `Operador`.

Request:

- No body.

Response `200 OK`:

```json
[
  {
    "partidaId": "uuid",
    "nombre": "Busqueda QR Campus",
    "modalidad": "Individual",
    "estado": "Lobby",
    "areaBusqueda": "Patio central y biblioteca",
    "cantidadEtapas": 3
  }
]
```

Errors:

| Status | Reason |
|---|---|
| 401 | Unauthenticated |
| 403 | Authenticated user is not operator |
| 500 | Persistence failure |

## Events

- No integration event is published. This is a read-only query.

## Real-Time Updates

- None required for HU-37 closure.
- A future SignalR update for newly published BDT games can be introduced by a separate real-time SDD.

## Design Patterns Applied

| Pattern | Location | Problem solved | Justification |
|---|---|---|---|
| CQRS + Mediator | `ListarPartidasBdtPublicadasOperadorQuery` and handler | Keeps query separate from commands | Required project architecture |
| Repository / Query Port | BDT application layer | Isolates persistence from application logic | Clean/hexagonal architecture requirement |
| DTO / Read Model | Operator list item | Avoids exposing domain entities to API/web | Maintains service boundary and stable contract |
| Dependency Injection | Query handler dependencies | Keeps handler testable | Required project architecture |

## Tests Required

Unit/application tests:

- Query returns only published BDT games.
- Query maps fields correctly.
- Query returns empty list when no published BDT games exist.
- Query returns rows in deterministic order by `Nombre` and then `PartidaId`.

Integration/API tests:

- `GET /api/bdt/operator/games/published` returns `200` for operator.
- Returns `401` without authentication.
- Returns `403` for non-operator role.
- Returns `403` without returning data when the authenticated operator has missing or malformed `sub` claim.
- Does not mutate database state.

Contract tests:

- Response shape matches `contracts/http/bdt-game-api.md` once updated.

Frontend tests:

- Web list renders rows.
- Web list renders loading, empty and error states.
- Web list renders a clear `401` unauthenticated/session-expired message distinct from `403` operator-role failures.
- Web list exposes an accessible table label for the published BDT games table.

PostgreSQL tests:

- Query reads persisted BDT games using isolated Npgsql test database/schema.

## Implementation Readiness Decision

HU-37 is ready for implementation once coding starts from `tasks.md` because:

- the owning service is BDT Game Service;
- the HTTP endpoint is documented in `contracts/http/bdt-game-api.md`;
- the event/no-event decision is documented in `contracts/events/bdt-game-events.md`;
- no SignalR behavior is required for closure;
- the query is read-only and must not mutate BDT state.
