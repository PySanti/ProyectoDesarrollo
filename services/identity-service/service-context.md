# Identity Service Context

## Migration State

This folder still maps by name to the current target `Identity` service, but it belongs to the previous implementation layout and may require future code migration before it fully satisfies the current doctrine.

Current target services are `Identity`, `Partidas`, `Operaciones de Sesion`, and `Puntuaciones`, behind the mandatory YARP gateway.

Use current documentation under `docs/02-project-context/`, `docs/03-microservices/`, and `contracts/` before planning new work.

## Responsibility

Identity owns identity, access governance and teams in the current doctrine. It integrates with Keycloak, stores only local references needed by the UMBRAL domain, absorbs the former team boundary, and owns temporary-credential email responsibility.

## Owns

- Usuario local.
- KeycloakId reference.
- Initial role assignment during user creation.
- User consultation.
- Editing general user data.
- User deactivation when required by SDD.
- Roles, functional permissions and governance privileges per role.
- Role modification for operators and participants, including promotion to administrator, propagated to Keycloak.
- Temporary-credential state and temporary password issuance; passwords are never persisted by UMBRAL.
- Team membership, leadership, invitations (`InvitacionEquipo`) and team-name history.
- Async temporary-credential email responsibility via RabbitMQ under the current doctrine.

## Does not own

- Trivia gameplay.
- BDT gameplay.
- QR validation.
- Game ranking.
- Keycloak internal password storage.

## Legacy implementation notes

Existing code in this folder may still reflect first-sprint behavior, synchronous SMTP, old SDDs or pre-migration assumptions. Treat that as migration debt until a current-doctrine SDD updates the implementation.

## Business rules

- UMBRAL must not store passwords or sensitive credentials.
- Role is assigned during user creation.
- Role modification follows current SRS rules: an administrator may modify operators and participants, including promotion to administrator, but may not modify an administrator's role.
- General user data can be edited by administrator according to SRS.
- Identity Service may coordinate with Keycloak through an infrastructure adapter.
- On user creation, a temporary password is issued and delivered according to the current Identity event/email design.
- On editing a user's email while the credential remains temporary, a new temporary credential is issued and delivered according to the current Identity event/email design.

> Legacy implementation evidence for older HU-01/HU-02 slices is not active doctrine unless it is regenerated under the current SDD workflow.

## Expected SDD ownership

All new Identity work must use a current spec listed in `docs/04-sdd/SPECS-LIST.md` and the current contracts under `contracts/http/identity-api.md` and `contracts/events/identity-events.md`.
