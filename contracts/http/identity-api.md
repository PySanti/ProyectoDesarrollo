# Identity API Contract

This file contains planned HTTP contracts for Identity Service.

Do not implement endpoints until the related SDD confirms request/response details.

## HU-01 — Crear usuario con rol inicial

### Create user

| Field | Value |
|---|---|
| Method | POST |
| Path | `/api/identity/users` |
| Auth | Administrador |
| Owning service | Identity Service |
| Client | React web |
| Status | Planned by SDD |

Request body draft:

```json
{
  "name": "string",
  "email": "string",
  "initialRole": "Administrador | Operador | Participante"
}
```

Response body draft:

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

Error cases draft:

- `400` invalid data.
- `409` email already exists.
- `403` authenticated user is not administrator.
- `502` Keycloak integration error.

## HU-02 — Consultar y editar datos generales de usuario

### List users

| Field | Value |
|---|---|
| Method | GET |
| Path | `/api/identity/users` |
| Auth | Administrador |
| Owning service | Identity Service |
| Client | React web |
| Status | Planned by SDD |

### Get user detail

| Field | Value |
|---|---|
| Method | GET |
| Path | `/api/identity/users/{userId}` |
| Auth | Administrador |
| Owning service | Identity Service |
| Client | React web |
| Status | Planned by SDD |

### Update general user data

| Field | Value |
|---|---|
| Method | PATCH |
| Path | `/api/identity/users/{userId}` |
| Auth | Administrador |
| Owning service | Identity Service |
| Client | React web |
| Status | Planned by SDD |

Request body draft:

```json
{
  "name": "string",
  "email": "string"
}
```

Important rule:

```txt
Do not allow role modification through this endpoint.
```

Error cases draft:

- `400` invalid data.
- `403` authenticated user is not administrator.
- `404` user not found.
- `409` email already exists.
