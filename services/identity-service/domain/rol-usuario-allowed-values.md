# RolUsuario Allowed Values for Creation

This file defines the allowed `RolUsuario` values for `HU-01` user creation in Identity Service.

## Allowed values

- `Administrador`
- `Operador`
- `Participante`

## Validation rule

During user creation, `initialRole` must match exactly one allowed value.
Any other value must be rejected as invalid input.

## Scope note

This rule applies to creation only.
Role modification after creation remains prohibited by business rules.
