# Usuario Domain Model

Identity Service local user model for HU-01.

## Allowed local fields

- `UsuarioId`
- `KeycloakId`
- `Nombre`
- `Correo`
- `RolUsuario`
- `EstadoUsuario`

## Forbidden local fields

The local user domain model must not include:

- password hashes
- plain passwords
- password salts
- refresh tokens stored as user credentials
- any sensitive credential material managed by Keycloak

## Rationale

Authentication is managed by Keycloak.
UMBRAL stores only local user references and domain state.
