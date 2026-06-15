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
| Status | Confirmed by HU-01 SDD |

Request body:

```json
{
  "name": "string",
  "email": "string",
  "initialRole": "Administrador | Operador | Participante"
}
```

Response body:

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

Error cases:

- `400` invalid data.
- `409` email already exists.
- `403` authenticated user is not administrator.
- `502` Keycloak integration error.
- `502` welcome-email delivery error.
- `500` local persistence error.

Side effect (extensión 2026-06-15): al crear el usuario se le envía un **correo de bienvenida** con
su contraseña temporal (única por usuario, generada por el servicio y nunca persistida, RB-U03) con
los estilos de la plataforma. El envío es **síncrono**: si el correo no puede entregarse, la
operación falla con `502` y **no** queda usuario creado (se compensan Keycloak y la persistencia
local). La respuesta exitosa **no** incluye la contraseña.

## HU-02 — Consultar y editar datos generales de usuario

### List users

| Field | Value |
|---|---|
| Method | GET |
| Path | `/api/identity/users` |
| Auth | Administrador |
| Owning service | Identity Service |
| Client | React web |
| Status | Confirmed by HU-02 SDD |

Response body:

```json
[
  {
    "userId": "uuid",
    "keycloakId": "string",
    "name": "string",
    "email": "string",
    "role": "Administrador | Operador | Participante",
    "status": "Activo | Desactivado"
  }
]
```

Error cases:

- `401` unauthenticated.
- `403` authenticated user is not administrator.

### Get user detail

| Field | Value |
|---|---|
| Method | GET |
| Path | `/api/identity/users/{userId}` |
| Auth | Administrador |
| Owning service | Identity Service |
| Client | React web |
| Status | Confirmed by HU-02 SDD |

Response body:

```json
{
  "userId": "uuid",
  "keycloakId": "string",
  "name": "string",
  "email": "string",
  "role": "Administrador | Operador | Participante",
  "status": "Activo | Desactivado"
}
```

Error cases:

- `401` unauthenticated.
- `403` authenticated user is not administrator.
- `404` user not found.

### Update general user data

| Field | Value |
|---|---|
| Method | PATCH |
| Path | `/api/identity/users/{userId}` |
| Auth | Administrador |
| Owning service | Identity Service |
| Client | React web |
| Status | Confirmed by HU-02 SDD |

Request body:

```json
{
  "name": "string",
  "email": "string"
}
```

Response body:

```json
{
  "userId": "uuid",
  "name": "string",
  "email": "string",
  "role": "Administrador | Operador | Participante",
  "status": "Activo | Desactivado"
}
```

Important rule:

```txt
Do not allow role modification through this endpoint.
```

Error cases:

- `400` invalid data.
- `401` unauthenticated.
- `403` authenticated user is not administrator.
- `404` user not found.
- `409` email already exists.
- `502` Keycloak integration error (al sincronizar email / resetear contraseña en el reenvío).
- `502` welcome-email delivery error (en el reenvío de credenciales).
- `500` local persistence error.

Side effect (extensión 2026-06-15): si el **correo cambia** y el usuario **aún tiene contraseña
temporal pendiente** (acción `UPDATE_PASSWORD` en Keycloak, es decir no completó su primer inicio de
sesión), entonces el servicio: (1) sincroniza el email en Keycloak, (2) genera y **resetea una nueva
contraseña temporal** (la original es irrecuperable, RB-U03), y (3) **envía un correo** con esa
contraseña al nuevo email (misma plantilla de marca). Si el reenvío falla, la operación devuelve
`502` y **revierte** el cambio (email local + email en Keycloak al valor previo). Si el correo no
cambia o el usuario ya activó su cuenta, la edición se comporta como antes (sin correo).

### Deactivate user

| Field | Value |
|---|---|
| Method | PATCH |
| Path | `/api/identity/users/{userId}/deactivation` |
| Auth | Administrador |
| Owning service | Identity Service |
| Client | React web |
| Status | Confirmed by HU-02 SDD |

Request body:

- Empty body (no payload required).

Response body:

```json
{
  "userId": "uuid",
  "status": "Desactivado"
}
```

Error cases:

- `401` unauthenticated.
- `403` authenticated user is not administrator.
- `404` user not found.
- `500` local persistence error.
