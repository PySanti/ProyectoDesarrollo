# Identity Service Context

## Responsibility

Manages local user references, roles, user state and Keycloak mapping.

## Owns

- Usuario
- KeycloakId
- RolUsuario
- EstadoUsuario
- local user references

## Related stories

- HU-01
- HU-02

## Does not own

- Teams
- Team membership
- Trivia sessions
- BDT sessions
- Game scoring
- Game ranking
- Game history

## Notes

Authentication and authorization base are handled through Keycloak. UMBRAL stores only local references needed by the domain.
