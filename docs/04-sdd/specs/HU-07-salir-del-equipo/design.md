# HU-07 — Design

## Owning service

- `Team Service`.

## Supporting services

- `Identity Service` solo mediante claims/autenticacion base del token cuando aplique.
- No hay consulta HTTP obligatoria a otro microservicio para cerrar HU-07.
- No hay acceso directo a bases de datos de otros servicios.

## Client target

- `React Native mobile` (actor `Participante`).

## Domain entities and value objects involved

- `Equipo` (aggregate root).
- `ParticipanteEquipo` (entidad hija).
- `EquipoId`.
- `EstadoEquipo`.

## Command

### `SalirDeEquipoCommand`

Fields:
- `ActorUserId`.

Rules:
- Busca el equipo activo al que pertenece `ActorUserId`.
- Si no existe equipo activo, falla como recurso no encontrado.
- Si el participante no es lider, lo remueve del equipo.
- Si el participante es lider y hay otros integrantes, rechaza con conflicto de negocio porque debe ejecutar HU-06 primero.
- Si el participante es lider y es el unico integrante, marca el equipo como `Eliminado` y remueve la membresia activa.
- Si el agregado cargado no esta `Activo`, rechaza la operacion para evitar salida sobre equipos eliminados o desactivados.
- Persiste el agregado manteniendo la trazabilidad historica.

## Queries

- No query nueva obligatoria para cerrar HU-07.
- La respuesta del comando puede devolver un DTO minimo con el resultado de salida.

## Application flow

1. Validar autenticacion del actor.
2. Crear `SalirDeEquipoCommand` con `ActorUserId` obtenido del token.
3. Consultar repositorio para obtener el equipo activo del participante.
4. Si no hay equipo activo, responder `404`.
5. Ejecutar comportamiento de dominio `Equipo.Salir(actorUserId)` o metodo equivalente.
6. Si el actor es lider con otros integrantes, responder `409`.
7. Si el equipo cargado no esta activo, responder `409` mediante error de aplicacion.
8. Si el actor es no lider, persistir remocion de integrante.
9. Si el actor es lider unico, persistir estado `Eliminado` del equipo.
10. Retornar resultado minimo para la UI movil.

## Domain changes

- Agregar comportamiento en `Equipo` para salida de integrante:
  - validar que el equipo esta en estado `Activo`;
  - validar que el usuario pertenece al equipo;
  - permitir salida directa de no lider;
  - impedir salida directa de lider con otros integrantes;
  - eliminar logicamente el equipo si el lider es el unico integrante.
- Excepciones de dominio usadas por el comportamiento:
  - `EquipoNoActivoException`;
  - `ParticipanteNoPerteneceAlEquipoException`;
  - `LiderDebeTransferirLiderazgoException`.
- El dominio debe conservar la regla de que un equipo activo nunca queda sin lider.
- El dominio no debe depender de EF Core, ASP.NET, RabbitMQ ni SignalR.

## Infrastructure

- Repositorio Team:
  - `GetActiveByMemberUserIdAsync(userId)` o metodo equivalente.
  - `UpdateAsync(equipo)` o persistencia equivalente por `SaveChanges`.
- Persistencia:
  - si el participante no lider sale, se remueve la fila de `ParticipanteEquipo` para liberar el indice unico `ux_equipos_participantes_usuarioid` y permitir futuras uniones por HU-04;
  - si el lider unico sale, el equipo queda en estado `Eliminado` sin borrado fisico de `Equipo`, y tambien se remueve la fila de `ParticipanteEquipo` para evitar una membresia activa obsoleta;
  - no introducir estado activo/inactivo de participante en HU-07, porque el modelo actual no lo tiene y cambiarlo ampliaria el alcance.
- Mapeo de errores:
  - participante sin equipo activo -> `404`;
  - lider con otros integrantes -> `409` por `LeaveTeamConflictException`;
  - equipo no activo -> `409` por `LeaveTeamConflictException`;
  - usuario no integrante del equipo cargado -> `409` por `LeaveTeamConflictException`.
  - La API mapea errores de aplicacion y no depende directamente de excepciones de dominio.

## HTTP contracts

Contract file:
- `contracts/http/team-api.md`.

Endpoint defined for HU-07 before implementation:

### `DELETE /api/teams/membership`

- Auth: participante autenticado.
- Type: Command.
- Request: empty body.
- Response `200 OK`:
  - `userId`, `equipoId`, `resultado`, `equipoEstado`.
- Errors:
  - `401` unauthenticated.
  - `403` unauthorized by role/policy.
  - `404` participant has no active team.
  - `409` leader must transfer leadership before leaving.
  - `500` persistence failure.

Rationale:
- The actor leaves their own active membership; the team is identified by backend state, not by a client-provided team id.
- This prevents a participant from attempting to leave a team they do not belong to.

## Events

Contract file:
- `contracts/events/team-events.md`.

HU-07 does not require a cross-service integration event to be considered complete.

Internal/domain facts may be recorded by Team Service history mechanisms when implemented:
- `ParticipanteSalioDeEquipo` for non-leader exit.
- `EquipoEliminado` for leader-only exit.

If these are published externally in a later task, they must be documented in `contracts/events/team-events.md` before implementation.

## Real-time updates

- No user-visible SignalR/WebSocket update is required for HU-07 closure.
- The mobile app updates its local state from the HTTP command response.

## Design Patterns Applied

| Pattern | Location | Problem solved | Justification |
|---|---|---|---|
| CQRS | Application layer command | Separates state mutation from reads | Required by ADR-0003 |
| Mediator | Command handler | Decouples API endpoint from use case | Project MediatR convention |
| Repository | Team persistence port | Isolates application/domain from EF Core | Clean Architecture |
| Aggregate behavior | `Equipo.Salir(...)` | Protects leadership and membership invariants | Team rules belong in the aggregate |
| Dependency Injection | Composition root | Inverts dependencies between layers | Mandatory architecture rule |

## Tests required

### Unit (Domain)
- `Equipo` removes a non-leader member successfully.
- `Equipo` rejects leader exit when other members exist.
- `Equipo` marks itself as `Eliminado` when the only leader leaves.
- `Equipo` rejects exit for a user that is not a member.

### Unit (Application)
- `SalirDeEquipoCommandHandler` success for non-leader exit.
- `SalirDeEquipoCommandHandler` success for leader-only exit.
- `SalirDeEquipoCommandHandler` returns not found when actor has no active team.
- `SalirDeEquipoCommandHandler` returns conflict when leader has other members.

### Integration
- `DELETE /api/teams/membership`:
  - `200` non-leader exit;
  - `200` leader-only exit with team eliminated;
  - `401` without authentication;
  - `403` actor not authorized as participant;
  - `404` no active team;
  - `409` leader with other members.
- Persistence regression: after a successful exit, the same user can join or create another team without hitting `ux_equipos_participantes_usuarioid`.

### Contract
- Validate request/response/error codes of `DELETE /api/teams/membership` against `contracts/http/team-api.md`.

### Mobile frontend
- Flow React Native for leaving current team:
  - confirmation action;
  - loading state;
  - success state returning to no-team view;
  - post-success action disabled with short label `Sin equipo activo`;
  - `404` no active team handling;
  - `409` leader must transfer leadership message.
- Render-level test coverage uses `LeaveTeamScreenController`, the render/state controller used by `LeaveTeamScreen`, with injected React Native components. It presses the leave action, mocks a successful response, asserts `Sin equipo activo`, and asserts the leave action is disabled.
