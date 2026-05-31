# IUsuarioRepository

Application port definition for user persistence in HU-01.

## Purpose

Expose persistence operations required by `CrearUsuarioConRolInicialCommandHandler`
without coupling application/domain layers to EF Core or storage details.

## Contract

### `AddAsync(usuario)`

- Input: local `Usuario` aggregate/entity
- Behavior: persists the user entity
- Output: persisted user reference (or success result, depending on final implementation)

### `ExistsByEmailAsync(email)`

- Input: email value
- Behavior: checks whether a local user already exists with that email
- Output: boolean

## Rules

- The repository must persist only fields allowed by
  `services/identity-service/domain/usuario-domain-model.md`.
- The repository must not store passwords or sensitive credential material.
- The repository is owned by Identity Service and must not access other service databases.

## Related artifacts

- Handler: `services/identity-service/application/handlers/crear-usuario-con-rol-inicial-command-handler.md`
- Domain model: `services/identity-service/domain/usuario-domain-model.md`
- SDD task source: `docs/04-sdd/specs/HU-01-crear-usuario-con-rol-inicial/tasks.md`
