# HU-10 — Design

## Owning service

- `BDT Game Service`.

## Supporting services

- `Identity Service` solo mediante autenticacion y claims del token cuando aplique.
- No hay consulta obligatoria a Team Service para listar partidas publicadas.
- No hay acceso directo a bases de datos de otros servicios.

## Client target

- `React Native mobile` (actor `Participante`).

## Domain entities and value objects involved

- `PartidaBDT`.
- `PartidaId`.
- `EstadoPartida`.
- `Modalidad`.
- `AreaBusqueda`.
- `EtapaBDT` para calcular `cantidadEtapas` como dato de lectura.

## Command

- No command. HU-10 es una consulta y no modifica estado.

## Query

### `ListarPartidasBdtPublicadasQuery`

Fields:
- `ActorUserId` desde token, solo para trazabilidad/autorizacion de consulta.
- `Modalidad` opcional; queda sin valor para HU-10 y se usa en HU-12.

Rules:
- Retorna solo partidas BDT en estado `Lobby`.
- No filtra por modalidad cuando `Modalidad` no se envia.
- Orden sugerido: fecha de publicacion o creacion descendente si existe; si no existe, orden estable por nombre o identificador.
- No calcula ranking, inscripciones ni liderazgo.

Read model:

```json
{
  "partidaId": "uuid",
  "nombre": "string",
  "modalidad": "Individual | Equipo",
  "estado": "Lobby",
  "areaBusqueda": "string",
  "cantidadEtapas": 1
}
```

## Application flow

1. Validar autenticacion del participante.
2. Ejecutar `ListarPartidasBdtPublicadasQuery` sin filtro de modalidad.
3. Consultar repositorio/read model de partidas BDT publicadas.
4. Retornar lista de partidas; si no hay partidas, retornar lista vacia `200 OK`.
5. La app movil renderiza carga, lista, vacio o error.

## Domain behavior

- No se introduce comportamiento de agregado para HU-10 porque es una consulta.
- La definicion de publicacion se basa en estado `Lobby` aceptado por la BDT antes de esta consulta.
- La query no debe activar partidas, crear inscripciones ni modificar etapas.

## Infrastructure

- Repositorio/read model BDT:
  - metodo sugerido `ListPublishedAsync(modalidad, cancellationToken)`.
  - aplica filtro `Estado == Lobby`.
  - aplica filtro `Modalidad` solo cuando se recibe valor.
- Persistencia:
  - EF Core/PostgreSQL dentro de BDT Game Service.
  - Sin acceso a bases de datos de Team Service o Identity Service.

## HTTP contracts

Contract file to update before implementation:
- `contracts/http/bdt-game-api.md`.

Endpoint planned by this SDD:

### `GET /api/bdt/games/published`

- Related HU: `HU-10`.
- Auth: participante autenticado (`Participante`).
- Type: Query.
- Query parameters:
  - `modalidad` opcional: `Individual | Equipo`. No se envia para HU-10.
- Response `200 OK`:

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

- Errors:
  - `400` invalid query parameter.
  - `401` unauthenticated.
  - `403` authenticated user without participant authorization/policy.
  - `500` persistence failure.

## Events

Contract file:
- `contracts/events/bdt-game-events.md`.

HU-10 does not require an integration event. The operation is a read-only query.

## Real-time updates

- No SignalR/WebSocket subscription is required for HU-10 closure.
- Future real-time publication updates may be designed in a separate SDD if required.

## Design Patterns Applied

| Pattern | Location | Problem solved | Justification |
|---|---|---|---|
| CQRS | Application query | Separates read-only listing from commands | Required by ADR-0003 |
| Mediator | Query handler | Decouples API endpoint from use case | Project MediatR convention |
| Repository / Read model | BDT persistence port | Isolates query from EF Core | Clean Architecture |
| Adapter | Mobile API client | Keeps mobile consumption aligned with HTTP contract | Mobile contract rule |
| Dependency Injection | Composition root | Inverts dependencies between layers | Mandatory architecture rule |

## Tests required

### Unit / Application
- Query handler returns only `Lobby` BDT games.
- Query handler returns both `Individual` and `Equipo` games when no modality is provided.
- Query handler returns empty list when no published games exist.

### Integration
- `GET /api/bdt/games/published` returns `200` with published BDT games.
- `GET /api/bdt/games/published` returns `200` with empty list when no games are published.
- Endpoint rejects unauthenticated requests with `401`.
- Endpoint rejects authenticated non-participant actors with `403` when policy applies.

### Contract
- Validate response shape and status codes against `contracts/http/bdt-game-api.md`.
- Confirm no integration event is required in `contracts/events/bdt-game-events.md`.

### Mobile frontend
- Renders loading state.
- Renders list with BDT games.
- Renders empty state.
- Renders request error state.
- Does not create inscriptions or duplicate backend business rules.
