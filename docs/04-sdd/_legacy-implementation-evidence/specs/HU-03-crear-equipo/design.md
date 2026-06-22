# HU-03 — Design

## Owning service

- `Team Service`

## Supporting services

- `Identity Service` (solo referencia de identidad/autenticación por claims o consulta autorizada si el diseño final lo requiere).
- Persistencia local de Team Service con EF Core/PostgreSQL.

## Client target

- `React Native mobile` (actor `Participante`).

## Domain entities and value objects involved

- `Equipo` (aggregate root)
- `ParticipanteEquipo` (entidad hija)
- `EquipoId`
- `NombreEquipo`
- `CodigoAcceso`
- `EstadoEquipo`

## Command

### `CrearEquipoCommand`

Fields:
- `ActorUserId`
- `NombreEquipo`

Rules:
- Verifica que `ActorUserId` no pertenezca a otro equipo activo.
- Crea `Equipo` activo con un integrante inicial.
- Registra integrante inicial como líder.
- Genera `CodigoAcceso` único.
- Persiste agregado respetando cardinalidad `1..5`.

## Queries

- No query nueva obligatoria para cerrar HU-03.
- La respuesta del comando puede incluir un read model mínimo de equipo creado.

## Application flow

1. Validar autenticación del actor.
2. Validar payload (`NombreEquipo` requerido y formato permitido).
3. Consultar repositorio para verificar no pertenencia a equipo activo.
4. Generar código de acceso único.
5. Crear agregado `Equipo` con creador como integrante y líder.
6. Persistir agregado.
7. Publicar evento de dominio/integración si aplica por contrato.
8. Retornar DTO de equipo creado.

## Infrastructure

- Repositorio Team:
  - `ExistsActiveTeamByUserIdAsync(userId)`
  - `ExistsByAccessCodeAsync(code)` (para unicidad)
  - `AddAsync(equipo)`
- Generador de código de acceso:
  - servicio de aplicación o dominio para producir códigos y verificar unicidad.
- Mapeo de conflicto de negocio:
  - Usuario ya pertenece a equipo activo -> `409`.

## HTTP contracts

Contract file:
- `contracts/http/team-api.md`

Endpoint to define for HU-03:

### `POST /api/teams`

- Auth: participante autenticado.
- Type: Command.
- Request:
  - `nombreEquipo`
- Response:
  - `equipoId`, `nombreEquipo`, `codigoAcceso`, `estado`, `liderUserId`, `integrantes`.
- Errors:
  - `400` invalid payload.
  - `401` unauthenticated.
  - `403` unauthorized by role/policy.
  - `409` already belongs to active team / unique code conflict retry exhaustion.

## Events

Contract file:
- `contracts/events/team-events.md`

Optional event for HU-03:
- `EquipoCreado` (v1)
  - Publisher: Team Service
  - Consumers: none required for HU-03 closure
  - Trigger: successful team creation
  - Payload minimum: `equipoId`, `liderUserId`, `codigoAcceso`, `occurredOnUtc`

HU-03 does not require cross-service asynchronous workflow to be considered complete.

## Real-time updates

- No user-visible real-time update required for HU-03 closure.

## Design Patterns Applied

| Pattern | Location | Problem solved | Justification |
|---|---|---|---|
| CQRS | Application layer command | Separa escritura de lecturas | Requerido por ADR-0003 |
| Mediator | Command handler | Desacopla API del caso de uso | Convención MediatR |
| Repository | Team persistence port | Aísla dominio/aplicación de EF Core | Clean Architecture |
| Factory Method | `Equipo.Crear...` | Garantiza invariantes de creación | Creador como líder y miembro inicial |
| Dependency Injection | Composition root | Inversión de dependencias por capas | Arquitectura obligatoria |

## Tests required

### Unit (Domain)
- `Equipo` se crea con 1 integrante inicial.
- El creador queda con `EsLider = true`.
- Se asigna `EstadoEquipo = Activo`.
- Se respeta cardinalidad mínima de 1 al crear.

### Unit (Application)
- `CrearEquipoCommandValidator` valida `NombreEquipo`.
- `CrearEquipoCommandHandler`:
  - éxito
  - conflicto si ya pertenece a equipo activo
  - conflicto si no se logra código único en intentos permitidos.

### Integration
- `POST /api/teams`:
  - `201` creación exitosa
  - `400` payload inválido
  - `401` sin autenticación
  - `403` actor no autorizado
  - `409` conflicto de negocio

### Contract
- Validar request/response/error codes de `POST /api/teams` vs `contracts/http/team-api.md`.

### Mobile frontend
- Flujo React Native de crear equipo:
  - envío de formulario
  - manejo de `201`
  - manejo de `409` (ya pertenece a equipo)
  - estados de carga/error.
