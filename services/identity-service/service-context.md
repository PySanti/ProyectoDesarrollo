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
- Temporary password generation for new users (random, per-user, never persisted).
- Welcome/credential email notification via SMTP: welcome email on user creation (HU-01) and credential re-send on email change while a temporary password is still pending (HU-02).

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
- On user creation, a welcome email with a per-user temporary password is sent synchronously; the password is never persisted (lives only in memory during the request — RB-U03).
- On editing a user's email, if the user still has a pending temporary password (`UPDATE_PASSWORD` action in Keycloak, i.e. has not completed first login), a new temporary password is generated/reset, the email is synced in Keycloak, and the credentials are re-sent to the new email.
- If the email cannot be delivered, the operation fails (`502`) and is compensated/reverted (no orphan or inconsistent state).
- These notifications are synchronous (no RabbitMQ); the `UsuarioCreado` integration event remains unused for this flow.

> Extensión 2026-06-15 documentada en `docs/04-sdd/specs/HU-01-...` y `HU-02-...` (spec/design/tasks/acceptance) y en `contracts/http/identity-api.md`. SMTP se configura por env (`SMTP_*`, ver `.env.example` / `GUIA-LEVANTAMIENTO.md`).

## Expected SDD ownership

HU-01 and HU-02 must create SDD folders before implementation:

```txt
docs/04-sdd/specs/HU-01-crear-usuario-con-rol-inicial/
docs/04-sdd/specs/HU-02-consultar-y-editar-datos-generales-de-usuario/
```
