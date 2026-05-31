# CrearUsuarioConRolInicialCommand

Application command definition for HU-01 in Identity Service.

## Purpose

Represents the write request to create a local `Usuario` with an initial role,
coordinated with Keycloak identity creation.

## Command fields

- `Name`: string, required
- `Email`: string, required
- `InitialRole`: string, required

## Expected behavior boundaries

- The command itself does not apply business rules.
- Business validation is applied by:
  - command validator (next task), and
  - domain/application rules during handling.

## Allowed role values reference

`InitialRole` must match one of the allowed values documented in:

- `services/identity-service/domain/rol-usuario-allowed-values.md`

## Related artifacts

- SDD: `docs/04-sdd/specs/HU-01-crear-usuario-con-rol-inicial/`
- Task source: `docs/04-sdd/specs/HU-01-crear-usuario-con-rol-inicial/tasks.md`
