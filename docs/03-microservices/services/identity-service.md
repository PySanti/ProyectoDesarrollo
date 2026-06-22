# Identity Service

## Status

Current target service.

## Responsibility

Identity materializes the Identidad (Generic) and Equipos (Support) bounded contexts plus permission/role governance. It manages users and their Keycloak mapping, the per-role permission/governance matrix, role modification, temporary-credential state, and the full team lifecycle (membership, leadership, invitations, history). It absorbs the former Team Service entirely. It also sends the temporary-password email asynchronously over RabbitMQ. It owns no game configuration, runtime or scoring. DB: `umbral_identity`.

## Owns

- Users, local user references and Keycloak mapping (UMBRAL stores no passwords).
- Roles, functional permissions and governance privileges **per role** (never per user), managed from the governance panel.
- Role modification for operators/participants — including promotion to admin — propagated to Keycloak (the Administrador role and its governance privileges are protected; no new roles are ever created).
- Temporary-credential state (temporary pending vs. definitive); mandatory first-login change is handled by Keycloak.
- Teams (1–5 members), membership, leadership and transfer.
- Team invitations (`InvitacionEquipo`) from a dynamic participant list (excludes anyone already in a team; blocked when the team is full; no access code).
- Per-participant team-name history.
- Async email notification of the temporary password over RabbitMQ (welcome on creation; re-issue on email change while the credential is still temporary).

## Does Not Own

- Partida or game configuration (Partidas).
- Live runtime, answer/QR validation, clues, geolocation, inscriptions/convocatorias (Operaciones de Sesion).
- Scoring or ranking (Puntuaciones).
- Keycloak's internal password storage.

## Communication

- HTTP through the YARP gateway.
- RabbitMQ for cross-service domain events where required (e.g. `UsuarioCreado`, `CredencialTemporalEmitida` driving the async email).
- SignalR/WebSockets through the gateway for user-visible updates where required.
