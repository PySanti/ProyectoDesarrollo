# Identity Service Context

## Responsibility

Identity Service owns identity-related application behavior inside UMBRAL.

It integrates with Keycloak and stores only local references needed by the UMBRAL domain.

## Owns

- Usuario local.
- KeycloakId reference.
- Initial role assignment during user creation.
- User consultation.
- Editing general user data.
- User deactivation when required by SDD.
- Role read model for authorization decisions.

## Does not own

- Team leadership.
- Team membership.
- Trivia gameplay.
- BDT gameplay.
- QR validation.
- Game ranking.
- Keycloak internal password storage.

## Active first-sprint stories

| HU | Feature | Client |
|---|---|---|
| HU-01 | Crear usuario con rol inicial | React web |
| HU-02 | Consultar y editar datos generales de usuario | React web |

## Business rules

- UMBRAL must not store passwords or sensitive credentials.
- Role is assigned during user creation.
- Role cannot be modified later from UMBRAL unless a future SDD explicitly changes this rule.
- General user data can be edited by administrator according to SRS.
- Identity Service may coordinate with Keycloak through an infrastructure adapter.

## Expected SDD ownership

HU-01 and HU-02 must create SDD folders before implementation:

```txt
docs/04-sdd/specs/HU-01-crear-usuario-con-rol-inicial/
docs/04-sdd/specs/HU-02-consultar-y-editar-datos-generales-de-usuario/
```
