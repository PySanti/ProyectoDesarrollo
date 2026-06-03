# HU-34 - Design

## Owning Service

- BDT Game Service.

## Supporting Services

- Identity Service / Keycloak token claims for authenticated role `Operador`.
- PostgreSQL through EF Core inside BDT Game Service.
- React web as operator client.

No other microservice is allowed to own BDT creation. Team Service is not required for HU-34 because no team is registered during creation.

## Domain Model

Entities and value objects involved:

- `PartidaBDT` aggregate root.
- `EtapaBDT` child entity.
- `AreaBusqueda` value object.
- `CodigoQREsperado` value object.
- `TiempoLimiteEtapa` value object with positive seconds.
- `EstadoPartida` enum with `Lobby`.
- `Modalidad` enum: `Individual`, `Equipo`.
- `ModoInicioPartida` enum: `Manual`, `Automatico`, `ManualYAutomatico`.

Domain invariants:

- A BDT game must have at least one stage.
- Every stage must have expected textual QR content.
- Every stage must have a positive time limit.
- Search area is text only.
- BDT creation must not introduce numeric BDT scoring as ranking rule.

## Commands

### `CrearPartidaBdtCommand`

Input:

- `Nombre`
- `AreaBusqueda`
- `Modalidad`
- `MinimoParticipantes`
- `MaximoParticipantes` for individual modality
- `MaximoEquipos` for team modality
- `MinimoJugadoresPorEquipo` for team modality
- `ModoInicio`
- `Etapas[]` with `Orden`, `CodigoQrEsperado`, `TiempoLimiteSegundos`

Output:

- Created BDT game summary.

Handler responsibilities:

- Validate role authorization at API/policy boundary.
- Build aggregate through domain factory.
- Persist aggregate through repository.
- Return DTO.

## Queries

- None required for HU-34. Listing is HU-37.

## HTTP Contract

Documented endpoint in `contracts/http/bdt-game-api.md`:

```txt
POST /api/bdt/games
```

Authorization:

- Authenticated `Operador`.

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

Response `201 Created`:

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

Errors:

| Status | Reason |
|---|---|
| 400 | Invalid request payload or invalid stage data |
| 401 | Unauthenticated |
| 403 | Authenticated user is not operator |
| 409 | Business rule conflict, such as invalid modality-specific limits |
| 500 | Persistence failure |

## Events

Integration events:

- None required for HU-34 closure.

Internal/domain event candidate:

- `PartidaBdtCreada` may be recorded internally by BDT Game Service history when history is implemented.

## Real-Time Updates

- None required for HU-34 closure.
- RF-13 publication/lobby real-time updates are explicitly deferred to HU-42 or HU-55 because this HU closes through synchronous persistence and HTTP response.
- A future real-time publication update can be added only when a SignalR lobby/publication contract is approved.

## Design Patterns Applied

| Pattern | Location | Problem solved | Justification |
|---|---|---|---|
| CQRS + Mediator | `CrearPartidaBdtCommand` and handler | Separates state mutation from queries | Required project architecture |
| Factory Method | `PartidaBDT.Crear(...)` | Creates aggregate with required invariants | BDT creation must validate stages and modality limits atomically |
| Composite | `PartidaBDT` with `EtapaBDT` children | Models game composed of ordered stages | BDT is naturally a parent aggregate with stage children |
| Repository | BDT application port and EF Core implementation | Isolates persistence | Clean/hexagonal architecture requirement |
| Result Pattern | Domain/application validation result | Maps expected business validation failures to HTTP responses | Avoids using exceptions for normal validation failures |

## Tests Required

Unit tests:

- Creating BDT with at least one valid stage succeeds.
- Creating BDT without stages fails.
- Creating BDT with empty expected QR fails.
- Creating BDT with non-positive stage time limit fails.
- Individual modality requires max players.
- Team modality requires max teams and minimum players per team.

Application tests:

- `CrearPartidaBdtCommandHandler` persists valid aggregate.
- Handler returns business error for invalid aggregate input.

Integration/API tests:

- `POST /api/bdt/games` returns `201` for valid operator request.
- Returns `400` for invalid payload.
- Returns `401` without authentication.
- Returns `403` for non-operator role.

Contract tests:

- Request/response shape matches `contracts/http/bdt-game-api.md` once updated.

Frontend tests:

- Web form validates required fields for usability.
- Web form submits to documented API and renders success/error states.

PostgreSQL tests:

- Created BDT and stages are persisted and read back with Npgsql using isolated test database/schema.

## Implementation Readiness Decision

HU-34 is ready for implementation once coding starts from `tasks.md` because:

- the owning service is BDT Game Service;
- the HTTP endpoint is documented in `contracts/http/bdt-game-api.md`;
- the event/no-event decision is documented in `contracts/events/bdt-game-events.md`;
- SignalR is explicitly deferred to HU-42 or HU-55;
- `TiempoLimiteEtapa` is the selected BDT stage time-limit value object;
- no cross-service call is required.
