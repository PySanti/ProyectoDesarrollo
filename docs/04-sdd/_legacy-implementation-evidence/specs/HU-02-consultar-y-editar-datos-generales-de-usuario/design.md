# HU-02 — Design

## Owning service

- `Identity Service`

## Supporting services

- Ningun microservicio UMBRAL adicional.
- Dependencia externa: `Keycloak` solo para autenticacion/claims del actor.
- Persistencia: PostgreSQL + EF Core dentro de Identity Service.

## Client target

- `React web` (actor `Administrador`).

## Domain entities and value objects involved

- `Usuario`
- `KeycloakId`
- `RolUsuario`
- `EstadoUsuario`

## Commands

### `UpdateUserGeneralDataCommand`

Suggested fields:
- `UserId`
- `Name`
- `Email`

Rules:
- Actualiza solo datos generales permitidos.
- No acepta ni procesa cambio de `Role`.

### `DeactivateUserCommand`

Suggested fields:
- `UserId`

Rules:
- Marca el usuario como `Desactivado`.

## Queries

### `GetUsersQuery`

Purpose:
- Obtener lista de usuarios para gestion administrativa.

### `GetUserByIdQuery`

Purpose:
- Obtener detalle de un usuario especifico.

## Application flow

### List users

1. Validar actor `Administrador`.
2. Ejecutar `GetUsersQuery`.
3. Retornar listado de read models.

### Get user detail

1. Validar actor `Administrador`.
2. Ejecutar `GetUserByIdQuery`.
3. Si no existe, responder `404`.
4. Retornar read model de detalle.

### Update general data

1. Validar actor `Administrador`.
2. Validar payload (name/email).
3. Cargar usuario por `UserId`.
4. Si no existe, responder `404`.
5. Validar duplicado de correo excluyendo el propio usuario.
6. Aplicar cambio de datos generales.
7. Persistir cambios.
8. Retornar estado actualizado.

### Deactivate user

1. Validar actor `Administrador`.
2. Cargar usuario por `UserId`.
3. Si no existe, responder `404`.
4. Cambiar estado a `Desactivado`.
5. Persistir cambios.
6. Retornar confirmacion de desactivacion.

## Infrastructure

- Extender `IUsuarioRepository` para consultas y actualizacion:
  - `GetAllAsync`
  - `GetByIdAsync`
  - `ExistsByEmailAsync(email, excludingUserId)`
  - `UpdateAsync` (o tracking + `SaveChanges`)
- Manejo consistente de excepciones:
  - `404` not found
  - `409` duplicate email
  - `500` persistence error
- Reutilizar configuracion auth y policy `AdminOnly` existente en API.

## HTTP contracts

Based on `contracts/http/identity-api.md` (HU-02 section):

### `GET /api/identity/users`

- Auth: `Administrador`
- Type: Query
- Result: listado de usuarios

### `GET /api/identity/users/{userId}`

- Auth: `Administrador`
- Type: Query
- Result: detalle de usuario

### `PATCH /api/identity/users/{userId}`

- Auth: `Administrador`
- Type: Command
- Request: `name`, `email`
- Rule: no permitir cambio de rol

### `PATCH /api/identity/users/{userId}/deactivation`

- Auth: `Administrador`
- Type: Command
- Purpose: desactivar usuario

## Events

- No se requiere evento de integracion obligatorio para cerrar HU-02.
- Si se introduce `UsuarioDesactivado`, debe tratarse como ampliacion futura de contrato/eventos y no como requisito minimo de esta HU.

## Real-time updates

- No aplica para HU-02.

## Design Patterns Applied

| Pattern | Location | Problem solved | Justification |
|---|---|---|---|
| CQRS | Application layer | Separa comandos de consultas | Requerido por ADR y arquitectura |
| Mediator | Handlers MediatR | Desacopla API de casos de uso | Convencion del proyecto |
| Repository | Persistence abstraction | Aisla aplicacion/dominio de EF Core | Clean Architecture |
| Adapter | Auth/Keycloak integration | Aisla dependencia externa | Testabilidad |
| Dependency Injection | Composition root | Invierte dependencias entre capas | Requerido por arquitectura |

## Tests required

### Unit (Domain)
- `Usuario.EditarDatosGenerales` aplica cambios permitidos.
- `Usuario.Desactivar` cambia estado a `Desactivado`.

### Unit (Application)
- Validators de update y deactivate.
- `UpdateUserGeneralDataCommandHandler`:
  - path exitoso
  - `404` not found
  - `409` duplicate email
- `DeactivateUserCommandHandler`:
  - path exitoso
  - `404` not found

### Integration
- `GET /api/identity/users` retorna `200` para admin.
- `GET /api/identity/users/{userId}` retorna `200` o `404`.
- `PATCH /api/identity/users/{userId}` retorna `200`, `400`, `404`, `409`.
- `PATCH /api/identity/users/{userId}/deactivation` retorna `200` o `404`.
- `403` para actor no admin y `401` sin autenticacion.

### Contract
- Shape request/response de endpoints HU-02 en `contracts/http/identity-api.md`.

### Frontend
- Guard admin para pantalla HU-02.
- Flujo de listado, detalle, edicion y desactivacion.
- Manejo de errores (`400`, `403`, `404`, `409`, `500`).

## Extensión 2026-06-15 — Reenvío de credenciales al cambiar el correo

### Flujo de `UpdateUserGeneralDataCommandHandler` (actualizado)

1. Cargar usuario local; validar request y unicidad de correo (sin cambios).
2. Calcular `emailChanged`. Si cambió, consultar Keycloak `HasTemporaryPasswordAsync` (acción
   `UPDATE_PASSWORD` pendiente) → `mustResendCredentials`.
3. Aplicar la edición local y persistir (`UpdateAsync`).
4. Si `mustResendCredentials`:
   a. `UpdateEmailAsync` en Keycloak (sincroniza `email`).
   b. Generar contraseña con `ITemporaryPasswordGenerator` + `ResetTemporaryPasswordAsync` (temporary=true).
   c. `IUserWelcomeEmailSender.SendWelcomeEmailAsync` al nuevo correo con la nueva contraseña.
   d. Si algo falla → revertir (restaurar nombre/email local previos + `UpdateEmailAsync` al email previo)
      y propagar la excepción.
5. Retornar DTO (sin contraseña).

### Puertos Keycloak añadidos (`IKeycloakIdentityPort`)

- `HasTemporaryPasswordAsync(keycloakId)` → `GET /admin/realms/{realm}/users/{id}`, busca `UPDATE_PASSWORD` en `requiredActions`.
- `UpdateEmailAsync(keycloakId, email)` → `PUT /admin/realms/{realm}/users/{id}` con `{ email }` (merge parcial).
- `ResetTemporaryPasswordAsync(keycloakId, password)` → `PUT /admin/realms/{realm}/users/{id}/reset-password` con `temporary=true`.

Reutiliza `ITemporaryPasswordGenerator`, `IUserWelcomeEmailSender` y la plantilla de marca introducidos en HU-01.

### Error mapping (PATCH actualizado)

- `502` para `KeycloakIntegrationException` (sincronizar email / resetear contraseña) y para `EmailDeliveryException` (reenvío). Tras el `502` el cambio queda revertido.

### Alcance (decisión del usuario)

- La sincronización de Keycloak y el reenvío **solo** ocurren en el caso correo-cambia + contraseña-temporal-pendiente. Para usuarios ya activos no se toca Keycloak (se mantiene el comportamiento previo; el desfase histórico BD↔Keycloak para usuarios activos queda fuera de alcance).
