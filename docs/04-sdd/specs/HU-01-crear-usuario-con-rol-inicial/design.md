# HU-01 — Design

## Owning service

- `Identity Service`

## Supporting services

- Ningun microservicio UMBRAL adicional.
- Dependencia externa: `Keycloak` mediante adapter de infraestructura.

## Client target

- `React web`

## Domain entities and value objects involved

- `Usuario`
- `KeycloakId`
- `RolUsuario`
- `EstadoUsuario`

## Commands

### `CrearUsuarioConRolInicialCommand`

Suggested fields:
- `Name`
- `Email`
- `InitialRole`

### Handler

- `CrearUsuarioConRolInicialCommandHandler`

## Queries

- No aplica para esta HU.

## Command flow

1. Validar que el actor autenticado sea administrador.
2. Validar request y rol inicial permitido.
3. Verificar unicidad del correo segun politica del servicio.
4. Crear usuario en Keycloak mediante puerto de aplicacion.
5. Crear entidad local `Usuario` con `KeycloakId`, rol inicial y estado `Activo`.
6. Persistir usuario local.
7. Retornar DTO de usuario creado.

## Infrastructure

- Puerto sugerido: `IKeycloakIdentityPort`
- Repositorio sugerido: `IUsuarioRepository`
- Persistencia local con EF Core
- Manejo explicito de fallas de integracion para evitar persistencia parcial
- Mapeo EF Core actualizado a tabla/columnas existentes en PostgreSQL local (`usuarios`, `usuarioid`, `keycloakid`, `nombre`, `correo`, `rol`, `estado`) para ejecucion runtime.

## Runtime auth notes

- El pipeline JWT usa `issuer` de Keycloak y enriquecimiento de claims de `realm_access.roles` para garantizar resolucion de `Administrador` en autorizacion.
- `RequireHttpsMetadata` queda condicionado por ambiente: `false` en `Development` y `true` fuera de `Development`.
- La validacion de audiencia queda activa (`ValidateAudience=true`) usando `Keycloak:ClientId`/`KEYCLOAK_CLIENT_ID` como audiencia valida del token.
- Se agrega politica CORS `FrontendDev` para `http://localhost:5173` en `Identity Service` para habilitar consumo del frontend React web en desarrollo.

## React web integration notes

- Se implementa cliente React web en `frontend/` para HU-01.
- El frontend usa `keycloak-js` con `onLoad=login-required` para autenticacion real.
- El adaptador de autenticacion hace `init()` idempotente para evitar doble inicializacion en React `StrictMode`.
- Variables requeridas:
  - `VITE_KEYCLOAK_URL`
  - `VITE_KEYCLOAK_REALM`
  - `VITE_KEYCLOAK_CLIENT_ID`
  - `VITE_IDENTITY_API_BASE_URL`
- La pantalla de HU-01 se bloquea si el usuario autenticado no tiene rol `Administrador` en `realm_access.roles`.
- La llamada HTTP usa `Authorization: Bearer <access_token>`.
- El formulario frontend consume exactamente el contrato de `POST /api/identity/users`.
- El frontend traduce estados `400`, `403`, `409`, `500`, `502` a mensajes de usuario sin mover reglas autoritativas fuera del backend.

## HTTP contract

From `contracts/http/identity-api.md`:

### `POST /api/identity/users`

Auth:
- `Administrador`

Request draft:

```json
{
  "name": "string",
  "email": "string",
  "initialRole": "Administrador | Operador | Participante"
}
```

Response draft:

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

Error drafts:
- `400` invalid data
- `403` authenticated user is not administrator
- `409` email already exists
- `502` Keycloak integration error

## Events

- `HU-01` does not publish integration events as part of its Definition of Done.
- `UsuarioCreado` remains available as a future identity event candidate, but it is out of HU-01 scope.

## Real-time updates

- No real-time update is required for HU-01.

## Extensión 2026-06-15 — Notificación de credenciales por correo

### Command flow (actualizado)

1. Validar actor administrador y request (sin cambios).
2. Verificar unicidad del correo (sin cambios).
3. **Generar contraseña temporal aleatoria** mediante `ITemporaryPasswordGenerator` (texto plano solo en memoria).
4. Crear usuario en Keycloak con esa contraseña temporal (`temporary: true` + acción requerida `UPDATE_PASSWORD`).
5. Crear y **persistir** la entidad local `Usuario`. Si falla → compensar (eliminar usuario en Keycloak) y propagar error.
6. **Enviar el correo de bienvenida** con la contraseña temporal vía `IUserWelcomeEmailSender`. Si falla → compensar (eliminar local + eliminar usuario en Keycloak) y propagar `EmailDeliveryException`.
7. Retornar DTO (sin contraseña).

El envío es el último paso para que la compensación deshaga exactamente las dos creaciones previas (all-or-nothing). El password nunca aparece en la respuesta HTTP ni se persiste (`RB-U03`).

### Nuevos puertos / abstracciones (Application)

- `ITemporaryPasswordGenerator` (`Abstractions/Security`): `string Generate()`.
- `IUserWelcomeEmailSender` (`Abstractions/Notifications`): `Task SendWelcomeEmailAsync(UserWelcomeEmailMessage, ct)`; record `UserWelcomeEmailMessage(Name, Email, Role, TemporaryPassword)`.
- `IKeycloakIdentityPort` ampliado: `CreateUserWithInitialRoleAsync(..., temporaryPassword, ct)` y `DeleteUserAsync(keycloakId, ct)` (compensación).
- `IUsuarioRepository.RemoveAsync(usuario, ct)` (compensación local).
- Nueva excepción `EmailDeliveryException` (Application).

### Nuevos adapters (Infrastructure)

- `CryptoTemporaryPasswordGenerator` (`Security`): genera 16 chars con al menos 1 minúscula/mayúscula/dígito/símbolo usando `RandomNumberGenerator` (sin caracteres ambiguos).
- `SmtpUserWelcomeEmailSender` (`Notifications`): `System.Net.Mail.SmtpClient` (sin dependencias nuevas; STARTTLS para Gmail). Lanza `EmailDeliveryException` ante cualquier fallo.
- `WelcomeEmailTemplate`: HTML inline con la paleta/tipografía de marca (`DESIGN.md`), en español.
- `SmtpOptions` (sección `Smtp`): `Host`, `Port`, `Username`, `Password`, `FromAddress`, `FromName`, `UseStartTls`. Binding por configuración + fallback a env `SMTP_*` (mismo patrón que Keycloak).
- `KeycloakIdentityAdapter`: usa la contraseña recibida (se elimina `KeycloakOptions.TemporaryPassword`) y agrega `DeleteUserAsync` (`DELETE /admin/realms/{realm}/users/{id}`, tratando `404` como ya-eliminado).

### Error mapping (actualizado)

- `502` también para `EmailDeliveryException` (dependencia externa de correo). Tras el `502` por correo no queda usuario creado.

### Configuración (env)

- `Smtp__Host` / `SMTP_HOST` (Gmail: `smtp.gmail.com`)
- `Smtp__Port` / `SMTP_PORT` (`587`)
- `Smtp__Username` / `SMTP_USERNAME`
- `Smtp__Password` / `SMTP_PASSWORD` (app password de Gmail)
- `Smtp__FromAddress` / `SMTP_FROM_ADDRESS`
- `Smtp__FromName` / `SMTP_FROM_NAME` (`UMBRAL`)
- `Smtp__UseStartTls` / `SMTP_USE_STARTTLS` (`true`)

### Eventos

- Se mantiene la decisión de HU-01: no se publican eventos de integración. La notificación es **síncrona** (no usa `UsuarioCreado` ni RabbitMQ), porque el repositorio no tiene mensajería real implementada (solo un publisher No-Op) y el flujo es intra-servicio.

## Design Patterns Applied

| Pattern | Location | Problem solved | Justification |
|---|---|---|---|
| CQRS | Application layer | Separates write use case from reads | Required by project ADR |
| Mediator | Command handler dispatch | Decouples API from application use case | Required project convention |
| Repository | User persistence | Isolates application/domain from EF Core | Clean Architecture |
| Adapter | Keycloak integration port | Isolates external identity provider | Testable and replaceable integration |
| Dependency Injection | Service registration | Inverts dependencies between layers | Required by architecture style |

## Tests required

### Unit
- Validation of required fields
- Validation of allowed initial roles
- Rejection of unauthorized actor
- Rejection of duplicated email rule

### Application / handler
- Successful creation in Keycloak and local persistence
- Keycloak failure prevents inconsistent local creation
- Duplicate email conflict path
- Authorization failure path

### Integration
- `POST /api/identity/users` happy path
- `403` for non-admin
- `409` for duplicate email
- `502` for Keycloak integration failure

### Contract
- Request/response shape aligned with `contracts/http/identity-api.md`
- No event contract is required for HU-01 closure
- Frontend tests for auth guard and create-user flow using mocked adapter/contracts
