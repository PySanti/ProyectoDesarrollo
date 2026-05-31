# Usuario Creation Invariants

This file defines the domain invariants for creating `Usuario` in Identity Service for HU-01.

## Invariants

1. Initial role is mandatory at creation time.
2. Initial role must be one of the allowed values defined in
   `services/identity-service/domain/rol-usuario-allowed-values.md`.
3. Initial state after creation is always `Activo`.
4. `KeycloakId` local reference is mandatory for a persisted user.
5. UMBRAL must not store password or sensitive credential fields in local persistence.

## Scope note

These invariants cover only creation behavior for HU-01.
Role updates after creation are out of scope for this task and remain prohibited by project business rules.
