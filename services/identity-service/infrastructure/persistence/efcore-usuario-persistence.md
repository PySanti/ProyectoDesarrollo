# EF Core Usuario Persistence

Infrastructure persistence definition for local `Usuario` in HU-01.

## Purpose

Define how Identity Service persists local `Usuario` data using EF Core,
aligned with domain and application contracts already defined for HU-01.

## Persistence scope

Persists only local identity reference and domain state:

- `UsuarioId`
- `KeycloakId`
- `Nombre`
- `Correo`
- `RolUsuario`
- `EstadoUsuario`

Forbidden in persistence model:

- password hashes
- plain passwords
- password salts
- sensitive credential material managed by Keycloak

## EF Core mapping requirements

1. `UsuarioId` as primary key.
2. `KeycloakId` required.
3. `Correo` required and unique index.
4. `RolUsuario` required.
5. `EstadoUsuario` required.
6. Text lengths and nullability must enforce the command/domain constraints.

## Repository alignment

Persistence implementation must satisfy:

- `services/identity-service/application/ports/i-usuario-repository.md`

Required operations:

- `AddAsync(usuario)`
- `ExistsByEmailAsync(email)`

## Transaction and consistency notes

- Local persistence is executed after successful Keycloak identity creation.
- Any local failure must be surfaced as application/infrastructure failure.
- No cross-service database access is allowed.

## Related artifacts

- Domain model: `services/identity-service/domain/usuario-domain-model.md`
- Repository port: `services/identity-service/application/ports/i-usuario-repository.md`
- Handler: `services/identity-service/application/handlers/crear-usuario-con-rol-inicial-command-handler.md`
- SDD task source: `docs/04-sdd/specs/HU-01-crear-usuario-con-rol-inicial/tasks.md`
