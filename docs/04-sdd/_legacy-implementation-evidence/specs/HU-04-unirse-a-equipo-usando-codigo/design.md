# HU-04 — Design

## Owning service

- `Team Service`

## Supporting services

- `Identity Service` (solo referencia de identidad/autenticacion por claims si aplica).
- Persistencia local de Team Service con EF Core/PostgreSQL.

## Client target

- `React Native mobile` (actor `Participante`).

## Domain entities and value objects involved

- `Equipo` (aggregate root)
- `ParticipanteEquipo` (entidad hija)
- `EquipoId`
- `CodigoAcceso`
- `EstadoEquipo`

## Command

### `UnirseAEquipoPorCodigoCommand`

Fields:
- `ActorUserId`
- `CodigoAcceso`

Rules:
- Verifica que `ActorUserId` no pertenezca a otro equipo activo.
- Busca el equipo activo por `CodigoAcceso` normalizado.
- Verifica que el equipo no tenga 5 integrantes.
- Agrega un nuevo `ParticipanteEquipo` como no lider.
- Persiste el agregado manteniendo cardinalidad `1..5`.

## Queries

- No query nueva obligatoria para cerrar HU-04.
- La respuesta del comando puede incluir un read model minimo del equipo actualizado.

## Application flow

1. Validar autenticacion del actor.
2. Validar payload (`CodigoAcceso` requerido, formato normalizable y longitud permitida).
3. Consultar repositorio para verificar no pertenencia a otro equipo activo.
4. Buscar equipo activo por codigo de acceso normalizado.
5. Si el equipo no existe, responder `404`.
6. Cargar agregado `Equipo` y ejecutar metodo de dominio para agregar participante.
7. Rechazar si el equipo ya tiene 5 integrantes.
8. Persistir cambios.
9. Retornar DTO del equipo actualizado.

## Domain changes

- Agregar comportamiento de dominio en `Equipo` para incorporar un integrante por `UsuarioId`.
- El agregado debe impedir:
  - integrantes duplicados dentro del mismo equipo;
  - exceder la cardinalidad maxima de 5;
  - marcar como lider al integrante agregado por HU-04.

## Infrastructure

- Repositorio Team:
  - `ExistsActiveTeamByUserIdAsync(userId)`
  - `GetActiveByAccessCodeAsync(code)`
  - `UpdateAsync(equipo)` o persistencia equivalente por `SaveChanges`
- Normalizacion de codigo de acceso:
  - trim + uppercase antes de consultar persistencia.
- Mapeo de errores:
  - participante ya pertenece a equipo activo -> `409`
  - codigo inexistente -> `404`
  - equipo lleno -> `409`

## HTTP contracts

Contract file:
- `contracts/http/team-api.md`

Endpoint to define for HU-04:

### `POST /api/teams/join-by-code`

- Auth: participante autenticado.
- Type: Command.
- Request:
  - `codigoAcceso`
- Response:
  - `equipoId`, `nombreEquipo`, `codigoAcceso`, `estado`, `liderUserId`, `integrantes`.
- Errors:
  - `400` invalid payload.
  - `401` unauthenticated.
  - `403` unauthorized by role/policy.
  - `404` team not found by access code.
  - `409` already belongs to active team / target team full / duplicate membership conflict.

## Events

Contract file:
- `contracts/events/team-events.md`

HU-04 does not require a new integration event to be considered complete.

## Real-time updates

- No user-visible real-time update required for HU-04 closure.

## Design Patterns Applied

| Pattern | Location | Problem solved | Justification |
|---|---|---|---|
| CQRS | Application layer command | Separa escritura de lecturas | Requerido por ADR-0003 |
| Mediator | Command handler | Desacopla API del caso de uso | Convencion MediatR |
| Repository | Team persistence port | Aisla dominio/aplicacion de EF Core | Clean Architecture |
| Aggregate behavior | `Equipo.AgregarParticipante(...)` | Protege cardinalidad y no duplicacion | Reglas de dominio del Team Service |
| Dependency Injection | Composition root | Inversion de dependencias por capas | Arquitectura obligatoria |

## Tests required

### Unit (Domain)
- `Equipo` agrega integrante no lider con exito.
- `Equipo` rechaza un sexto integrante.
- `Equipo` rechaza agregar un `UsuarioId` ya presente en el mismo equipo.

### Unit (Application)
- `UnirseAEquipoPorCodigoCommandValidator` valida `CodigoAcceso`.
- `UnirseAEquipoPorCodigoCommandHandler`:
  - exito
  - conflicto si ya pertenece a equipo activo
  - `404` si codigo no existe
  - conflicto si el equipo esta lleno

### Integration
- `POST /api/teams/join-by-code`:
  - `200` union exitosa
  - `400` payload invalido
  - `401` sin autenticacion
  - `403` actor no autorizado
  - `404` codigo inexistente
  - `409` conflicto de negocio

### Contract
- Validar request/response/error codes de `POST /api/teams/join-by-code` vs `contracts/http/team-api.md`.

### Mobile frontend
- Flujo React Native de unirse por codigo:
  - envio de formulario
  - manejo de `200`
  - manejo de `404` (codigo invalido)
  - manejo de `409` (ya pertenece / equipo lleno)
  - estados de carga/error.
