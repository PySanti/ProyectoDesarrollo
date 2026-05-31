# CrearUsuarioConRolInicialResponse

Read model / response DTO definition for HU-01 user creation.

## Purpose

Represent the successful application/API output after creating a user in
Keycloak and persisting the local `Usuario` reference.

## Response fields

- `UserId`: string/uuid, required
- `KeycloakId`: string, required
- `Name`: string, required
- `Email`: string, required
- `Role`: string, required
- `Status`: string, required

## Value constraints

- `Role` must be one of:
  - `Administrador`
  - `Operador`
  - `Participante`
- `Status` for HU-01 creation is expected to be `Activo`.

## Contract alignment

This read model aligns with the HU-01 response draft in:

- `contracts/http/identity-api.md`

Expected response shape:

```json
{
  "userId": "uuid",
  "keycloakId": "string",
  "name": "string",
  "email": "string",
  "role": "Administrador | Operador | Participante",
  "status": "Activo"
}
```

## Scope note

This artifact defines successful output only.
Error payload models remain part of later API/error-mapping tasks.
