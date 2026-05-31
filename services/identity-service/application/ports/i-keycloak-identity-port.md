# IKeycloakIdentityPort

Application port definition for Keycloak identity operations required by HU-01.

## Purpose

Expose Keycloak user creation and initial role assignment capabilities to the
application layer without coupling handlers to Keycloak SDK/HTTP details.

## Contract

### `CreateUserAsync(name, email)`

- Input:
  - `name`
  - `email`
- Behavior:
  - Creates a Keycloak user identity.
- Output:
  - `keycloakUserId` (external identity reference).

### `AssignInitialRoleAsync(keycloakUserId, role)`

- Input:
  - `keycloakUserId`
  - `role`
- Behavior:
  - Assigns one initial realm/client role to the created Keycloak user.
- Output:
  - success result.

## Input constraints

- `role` must map to one of:
  - `Administrador`
  - `Operador`
  - `Participante`
- The role value source of truth is:
  - `services/identity-service/domain/rol-usuario-allowed-values.md`

## Error boundaries

Adapter implementation must surface integration failures so application/API
can map them to the defined HTTP error behavior (`502` for integration errors).

## Ownership and boundaries

- Port is owned by `Identity Service`.
- It must not read or write other service databases.
- It must not manage local persistence; that is `IUsuarioRepository` scope.

## Related artifacts

- Handler orchestration:
  - `services/identity-service/application/handlers/crear-usuario-con-rol-inicial-command-handler.md`
- HTTP contract draft:
  - `contracts/http/identity-api.md`
- SDD task source:
  - `docs/04-sdd/specs/HU-01-crear-usuario-con-rol-inicial/tasks.md`
