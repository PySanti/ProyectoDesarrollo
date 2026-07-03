# Identity HTTP Contract

## Status

Current contract index. Concrete endpoints require a current-doctrine SDD before implementation.

## Access Path

Requests enter through the YARP gateway.

## Owned Capabilities

- User creation with initial role, local user references and Keycloak mapping.
- User consultation, general-data editing and deactivation.
- Role modification for operators and participants, including promotion to administrator, propagated to Keycloak.
- Per-role functional permissions and governance privileges.
- Teams, team membership, leadership transfer and team deletion.
- Team invitations (`InvitacionEquipo`) and per-participant team-name history.
- Temporary-credential state and async email notification over RabbitMQ.

## Endpoint Registry

### User management (AdminOnly — role `Administrador`)

| Capability | Method | Path | Status | Notes |
|---|---|---|---|---|
| Create user with initial role | POST | `/api/identity/users` | Registered | 201 on success; 409 duplicate email |
| List users | GET | `/api/identity/users` | Registered | 200 |
| Get user by id | GET | `/api/identity/users/{userId}` | Registered | 200 / 404 |
| Update user general data | PATCH | `/api/identity/users/{userId}` | Registered | 200; body `{ name, email }` |
| Deactivate user | PATCH | `/api/identity/users/{userId}/deactivation` | Registered | 200 |

### Teams and invitations (ParticipantOnly — role `Participante`)

> **There is no team access code (`codigoAcceso`).** Members join only via `InvitacionEquipo` sent by the team leader from a dynamic participant list (see `GET /api/teams/eligible-participants`). Access-code join is not supported.

| Capability | Method | Path | Status | Notes |
|---|---|---|---|---|
| Create team | POST | `/api/teams` | Registered | 201; creator becomes leader; response has no `codigoAcceso` |
| Leave team | DELETE | `/api/teams/membership` | Registered | 200; leader with no other members deletes the team |
| Transfer leadership | PATCH | `/api/teams/leadership` | Registered | 200; body `{ nuevoLiderUserId }` |
| Send invitation (leader) | POST | `/api/teams/invitations` | Registered | 201; body `{ invitadoUserId }`; 409 if pending invitation already exists or invitee already in a team |
| Get received invitations (invitee inbox) | GET | `/api/teams/invitations` | Registered | 200; returns pending invitations for the authenticated participant |
| Accept invitation | POST | `/api/teams/invitations/{invitacionId}/acceptance` | Registered | 200; 409 if already in a team or team is full |
| Reject invitation | POST | `/api/teams/invitations/{invitacionId}/rejection` | Registered | 200 |
| Get eligible participants (leader) | GET | `/api/teams/eligible-participants` | Registered | 200; dynamic list excluding participants already in a team; blocked when team is full |
| Get my active team | GET | `/api/teams/mine` | Registered | 200 `{ equipoId, nombreEquipo, estado, participantes:[{ usuarioId, esLider }] }`; 404 if caller has no active team |
