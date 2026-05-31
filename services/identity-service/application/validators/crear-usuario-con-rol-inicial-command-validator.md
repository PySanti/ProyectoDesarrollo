# CrearUsuarioConRolInicialCommandValidator

Application-level validator definition for `CrearUsuarioConRolInicialCommand` in HU-01.

## Validation targets

- `Name`
- `Email`
- `InitialRole`

## Validation rules

1. `Name` is required and cannot be blank.
2. `Email` is required and cannot be blank.
3. `Email` must follow a valid email format.
4. `InitialRole` is required.
5. `InitialRole` must match one allowed value documented in:
   - `services/identity-service/domain/rol-usuario-allowed-values.md`

## Failure behavior

- If any rule fails, command processing must be rejected before handler execution.
- Validation failures are mapped to bad request behavior by the API layer.

## Scope note

This validator covers only command input constraints.
Business state rules (for example, duplicate email) are handled in subsequent tasks.
