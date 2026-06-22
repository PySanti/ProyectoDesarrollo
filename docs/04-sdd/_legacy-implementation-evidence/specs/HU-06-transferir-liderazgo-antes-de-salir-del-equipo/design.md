# HU-06 — Design

## Owning service

- `Team Service`.

## Supporting services

- `Identity Service` solo mediante claims/autenticacion base del token cuando aplique.
- No hay consulta HTTP obligatoria a otro microservicio para cerrar HU-06.
- No hay acceso directo a bases de datos de otros servicios.

## Client target

- `React Native mobile` (actor `Participante lider`).

## Domain entities and value objects involved

- `Equipo` (aggregate root).
- `ParticipanteEquipo` (entidad hija).
- `EquipoId`.
- `EstadoEquipo`.

## Command

### `TransferirLiderazgoCommand`

Fields:
- `ActorUserId`.
- `NuevoLiderUserId`.

Rules:
- Busca el equipo activo al que pertenece `ActorUserId`.
- Si no existe equipo activo, falla como recurso no encontrado.
- Ejecuta comportamiento de dominio `Equipo.TransferirLiderazgo(actorUserId, nuevoLiderUserId)` o metodo equivalente.
- Rechaza si el actor no es el lider actual.
- Rechaza si `NuevoLiderUserId` no pertenece al mismo equipo.
- Rechaza si `NuevoLiderUserId` es el mismo lider actual.
- Rechaza si el equipo no esta `Activo`.
- Persiste el agregado manteniendo exactamente un lider.
- Retorna el identificador del lider anterior, el nuevo lider y el estado del equipo.

## Queries

- No query nueva obligatoria para cerrar HU-06.
- La pantalla movil puede usar datos de equipo ya existentes si estan disponibles; si una consulta de detalle de equipo se requiere en una tarea futura, debe documentarse como contrato separado antes de implementarse.

## Application flow

1. Validar autenticacion del actor.
2. Crear `TransferirLiderazgoCommand` con `ActorUserId` obtenido del token y `NuevoLiderUserId` enviado por la app movil.
3. Validar payload: `NuevoLiderUserId` requerido y distinto de `Guid.Empty`.
4. Consultar repositorio para obtener el equipo activo del actor.
5. Si no hay equipo activo, responder `404`.
6. Ejecutar comportamiento de dominio de transferencia.
7. Si el actor no es lider, responder `409`.
8. Si el nuevo lider no es integrante del equipo, responder `409`.
9. Si el nuevo lider es el mismo actor, responder `409`.
10. Si el equipo no esta activo, responder `409`.
11. Persistir cambios.
12. Retornar resultado minimo para la UI movil.

## Domain changes

- Agregar comportamiento en `Equipo` para transferencia de liderazgo:
  - validar que el equipo esta en estado `Activo`;
  - validar que el actor pertenece al equipo;
  - validar que el actor es el lider actual;
  - validar que el nuevo lider pertenece al equipo;
  - validar que el nuevo lider es diferente al lider actual;
  - quitar liderazgo al lider anterior;
  - marcar como lider al nuevo integrante;
  - asegurar que queda exactamente un lider.
- Excepciones de dominio sugeridas:
  - `EquipoNoActivoException` si se reutiliza la excepcion existente de HU-07;
  - `ParticipanteNoPerteneceAlEquipoException` si el actor no pertenece;
  - `ActorNoEsLiderEquipoException`;
  - `NuevoLiderNoPerteneceAlEquipoException`;
  - `NuevoLiderDebeSerDiferenteException`.
- El dominio debe conservar la cardinalidad `1..5` y no debe depender de EF Core, ASP.NET, RabbitMQ ni SignalR.

## Infrastructure

- Repositorio Team:
  - reutilizar `GetActiveByMemberUserIdAsync(userId)` si ya existe por HU-07, o metodo equivalente.
  - `UpdateAsync(equipo)` o persistencia equivalente por `SaveChanges`.
- Persistencia:
  - no se agregan ni eliminan filas de `ParticipanteEquipo`;
  - solo se actualizan flags de liderazgo (`EsLider`) de los dos integrantes afectados;
  - el equipo permanece `Activo`.
- Mapeo de errores:
  - participante sin equipo activo -> `404`;
  - actor no lider -> `409`;
  - nuevo lider no integrante -> `409`;
  - nuevo lider igual al actor -> `409`;
  - equipo no activo -> `409`;
  - error de persistencia -> `500`.

## HTTP contracts

Contract file to update before implementation:
- `contracts/http/team-api.md`.

Endpoint planned by this SDD:

### `PATCH /api/teams/leadership`

- Related HU: `HU-06`.
- Auth: participante autenticado (`Participante`) que es lider del equipo activo.
- Type: Command.
- Request:

```json
{
  "nuevoLiderUserId": "uuid"
}
```

- Response `200 OK`:

```json
{
  "equipoId": "uuid",
  "liderAnteriorUserId": "uuid",
  "nuevoLiderUserId": "uuid",
  "equipoEstado": "Activo"
}
```

- Errors:
  - `400` invalid payload.
  - `401` unauthenticated.
  - `403` authenticated user without participant authorization/policy.
  - `404` actor has no active team.
  - `409` actor is not leader, target is not a member, target is current leader, or team is not active.
  - `500` persistence failure.

Rationale:
- The actor transfers leadership in their own active team; the team is identified by backend state, not by a client-provided team id.
- This prevents transferring leadership in a team the actor does not belong to.

## Events

Contract file:
- `contracts/events/team-events.md`.

HU-06 does not require a cross-service integration event to be considered complete.

Internal/domain facts may be recorded by Team Service history mechanisms when implemented:
- `LiderazgoTransferido` with `EquipoId`, `LiderAnteriorId`, `NuevoLiderId`.

If this fact is published externally in a later task, it must be documented in `contracts/events/team-events.md` before implementation.

## Real-time updates

- No user-visible SignalR/WebSocket update is required for HU-06 closure.
- The mobile app updates its local state from the HTTP command response.

## Design Patterns Applied

| Pattern | Location | Problem solved | Justification |
|---|---|---|---|
| CQRS | Application layer command | Separates state mutation from reads | Required by ADR-0003 |
| Mediator | Command handler | Decouples API endpoint from use case | Project MediatR convention |
| Repository | Team persistence port | Isolates application/domain from EF Core | Clean Architecture |
| Aggregate behavior | `Equipo.TransferirLiderazgo(...)` | Protects leadership invariant | Team rules belong in the aggregate |
| Dependency Injection | Composition root | Inverts dependencies between layers | Mandatory architecture rule |

## Tests required

### Unit (Domain)
- `Equipo` transfers leadership from current leader to another member successfully.
- `Equipo` keeps exactly one leader after transfer.
- `Equipo` rejects transfer when actor is not current leader.
- `Equipo` rejects transfer when target user is not a member.
- `Equipo` rejects transfer when target user is the current leader.
- `Equipo` rejects transfer when team is not active.

### Unit (Application)
- `TransferirLiderazgoCommandHandler` success for active team leader and valid target member.
- `TransferirLiderazgoCommandHandler` returns not found when actor has no active team.
- `TransferirLiderazgoCommandHandler` returns conflict when actor is not leader.
- `TransferirLiderazgoCommandHandler` returns conflict when target is not member.
- `TransferirLiderazgoCommandHandler` returns conflict when target is current leader.
- `TransferirLiderazgoCommandHandler` returns conflict when team is not active.

### Integration
- `PATCH /api/teams/leadership`:
  - `200` successful leadership transfer;
  - `400` invalid request;
  - `401` without authentication;
  - `403` actor not authorized as participant;
  - `404` no active team;
  - `409` actor not leader;
  - `409` target is not member;
  - `409` target is current leader.
- Persistence regression: after successful transfer, the former leader can call HU-07 and leave as non-leader.

### Contract
- Validate request/response/error codes of `PATCH /api/teams/leadership` against `contracts/http/team-api.md`.
- Confirm `contracts/events/team-events.md` states no integration event is required for HU-06 closure.

### Mobile frontend
- Flow React Native for selecting a new leader from current team members:
  - loading state;
  - success state with new leader visible;
  - `404` no active team handling;
  - `409` not leader handling;
  - `409` invalid target member handling;
  - post-success guidance that the previous leader can now leave the team through HU-07.
