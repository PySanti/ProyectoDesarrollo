# HU-12 — Design

## Owning service

- `BDT Game Service`.

## Supporting services

- `Identity Service` solo mediante autenticacion y claims del token cuando aplique.
- No hay consulta obligatoria a Team Service para filtrar partidas publicadas.
- No hay acceso directo a bases de datos de otros servicios.

## Client target

- `React Native mobile` (actor `Participante`).

## Domain entities and value objects involved

- `PartidaBDT`.
- `EstadoPartida`.
- `Modalidad`.
- `AreaBusqueda`.
- `EtapaBDT` para `cantidadEtapas` en read model.

## Command

- No command. HU-12 es una consulta/filtro y no modifica estado.

## Query

### `ListarPartidasBdtPublicadasQuery`

Fields:
- `ActorUserId` desde token.
- `Modalidad` opcional: `Individual | Equipo`.

Rules:
- Retorna solo partidas BDT en estado `Lobby`.
- Si `Modalidad` es `Individual`, retorna solo partidas individuales.
- Si `Modalidad` es `Equipo`, retorna solo partidas por equipo.
- Si `Modalidad` no se envia, retorna todas las partidas publicadas BDT.
- Si `Modalidad` tiene valor invalido, responde `400` antes de ejecutar la query.

## Application flow

1. Validar autenticacion del participante.
2. Leer filtro seleccionado en la app movil.
3. Enviar `modalidad` solo si el filtro es `Individual` o `Equipo`.
4. BDT Game Service valida el parametro opcional.
5. Ejecutar query de partidas publicadas con filtro de modalidad.
6. Retornar lista filtrada o lista vacia.
7. La app movil actualiza la lista y muestra estado vacio si no hay resultados.

## Domain behavior

- No se introduce comportamiento de agregado para HU-12.
- La query respeta el estado `Lobby` como definicion de partida publicada.
- El filtro no valida liderazgo ni pertenencia a equipo.

## Infrastructure

- Reutilizar repositorio/read model de HU-10.
- Agregar filtro `Modalidad` cuando el parametro opcional exista.
- Mantener la consulta dentro de la base del BDT Game Service.

## HTTP contracts

Contract file to update before implementation:
- `contracts/http/bdt-game-api.md`.

Endpoint reused by this SDD:

### `GET /api/bdt/games/published?modalidad=Individual|Equipo`

- Related HU: `HU-12`.
- Auth: participante autenticado (`Participante`).
- Type: Query.
- Query parameters:
  - `modalidad` opcional: `Individual | Equipo`.
  - Omitir `modalidad` equivale a filtro `Todas`.
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
  - `400` invalid modality query parameter.
  - `401` unauthenticated.
  - `403` authenticated user without participant authorization/policy.
  - `500` persistence failure.

## Events

Contract file:
- `contracts/events/bdt-game-events.md`.

HU-12 does not require an integration event. The operation is a read-only query.

## Real-time updates

- No SignalR/WebSocket subscription is required for HU-12 closure.
- The mobile app refreshes the list through HTTP when the selected filter changes.

## Design Patterns Applied

| Pattern | Location | Problem solved | Justification |
|---|---|---|---|
| CQRS | Application query | Keeps filtering read-only and separate from commands | Required by ADR-0003 |
| Mediator | Query handler | Decouples endpoint from query use case | Project MediatR convention |
| Repository / Read model | BDT persistence port | Encapsulates EF Core filtering | Clean Architecture |
| Adapter | Mobile API client | Maps UI filter state to documented HTTP query | Mobile contract rule |
| Dependency Injection | Composition root | Inverts dependencies between layers | Mandatory architecture rule |

## Tests required

### Unit / Application
- Query handler filters by `Individual`.
- Query handler filters by `Equipo`.
- Query handler returns all published BDT games when modality is omitted.
- Query validation rejects invalid modality values.

### Integration
- `GET /api/bdt/games/published?modalidad=Individual` returns only individual BDT games.
- `GET /api/bdt/games/published?modalidad=Equipo` returns only team BDT games.
- Invalid modality returns `400`.
- Endpoint rejects unauthenticated requests with `401`.
- Endpoint rejects authenticated non-participant actors with `403` when policy applies.

### Contract
- Validate query parameter, response shape and status codes against `contracts/http/bdt-game-api.md`.
- Confirm no integration event is required in `contracts/events/bdt-game-events.md`.

### Mobile frontend
- Renders filter controls: `Todas`, `Individual`, `Equipo`.
- Requests the endpoint without modality for `Todas`.
- Requests `modalidad=Individual` for individual filter.
- Requests `modalidad=Equipo` for team filter.
- Renders empty state scoped to selected filter.
- Renders request error state.
