# KeycloakIdentityAdapter

Infrastructure adapter definition for `IKeycloakIdentityPort` in HU-01.

## Purpose

Implement Keycloak integration details required by Identity Service to:

1. create an external user identity,
2. assign the initial role to that created user.

This adapter keeps Keycloak HTTP/token/protocol concerns out of the
application layer.

## Implements

- `services/identity-service/application/ports/i-keycloak-identity-port.md`

## Operations

### `CreateUserAsync(name, email)`

- Build Keycloak admin request payload.
- Call Keycloak admin API to create user.
- Resolve and return `keycloakUserId`.

### `AssignInitialRoleAsync(keycloakUserId, role)`

- Resolve role representation in Keycloak.
- Assign role to `keycloakUserId`.

## Required configuration inputs

- Keycloak authority/base URL
- Realm name
- Client ID (service account client)
- Client secret
- Token endpoint information

## Role mapping rule

The adapter must map `InitialRole` using allowed values from:

- `services/identity-service/domain/rol-usuario-allowed-values.md`

Any role outside that set must be rejected before attempting assignment.

## Error handling behavior

- Convert Keycloak/network/auth failures into integration failures that can be
  mapped by application/API to the HU-01 contract behavior (`502`).
- Do not swallow errors.
- Return enough error context for logging and handler-level decision making.

## Boundary rules

- Adapter must not persist local `Usuario` data.
- Adapter must not access any other microservice database.
- Adapter is owned only by Identity Service.

## Related artifacts

- Port: `services/identity-service/application/ports/i-keycloak-identity-port.md`
- Handler: `services/identity-service/application/handlers/crear-usuario-con-rol-inicial-command-handler.md`
- HTTP contract: `contracts/http/identity-api.md`
