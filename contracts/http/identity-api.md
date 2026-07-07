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

### User management (policy `AdminOnly` — role `Administrador`) (SP-5a re-homed)

Paths re-homed under the service's own prefix (`identity/`), replacing the former
`api/identity/*` paths, per the SP-3g convention (each service hosts under its own prefix) and
the gateway matrix (`/identity/users/{**catch-all}` → policy `Administrador`, Order 1).
Auth: `401` without a token; `403` without the `Administrador` role.

| Capability | Method | Path | Status | Notes |
|---|---|---|---|---|
| Create user with initial role | POST | `/identity/users` | Registered | 201 on success; 401/403 per above; 409 duplicate email |
| List users | GET | `/identity/users` | Registered | 200; 401/403 per above |
| Get user by id | GET | `/identity/users/{userId}` | Registered | 200 / 404; 401/403 per above |
| Update user general data | PATCH | `/identity/users/{userId}` | Registered | 200; body `{ name, email }`; 401/403 per above |
| Deactivate user | PATCH | `/identity/users/{userId}/deactivation` | Registered | 200; 401/403 per above |

### Governance (SP-5b)

Role/permission governance (`GovernanceController`) plus the role-change endpoint hosted on
`UsersController` (same resource, same policy). Auth: policy `AdminOnly` — role `Administrador`
(gateway routes `/identity/governance/{**catch-all}` and `/identity/users/{**catch-all}`, both
`Administrador`, per the gateway matrix). `401` without a token; `403` without the
`Administrador` role.

| Capability | Method | Path | Status | Notes |
|---|---|---|---|---|
| Get role → permission matrix | GET | `/identity/governance/roles` | Registered | 200 `{ roles: [{ rol, permisos: [], privilegiosGobernanza }] }`; `privilegiosGobernanza` is informational only (`true` for `Administrador`, never assignable/removable as a permission); 401/403 per above |
| Replace a role's functional permissions | PUT | `/identity/governance/roles/{rol}/permisos` | Registered | Body `{ permisos: [...] }` is the **desired final set** (replace, not a delta); 200 `{ rol, permisos, privilegiosGobernanza }` with the final applied set (empty diff → 200 with no Keycloak calls and no event); 400 invalid `rol` or unknown permission name; 401/403 per above; 502 if a Keycloak composite add/remove fails (see consistency note below) |
| Change a user's role | PATCH | `/identity/users/{userId}/role` | Registered | Body `{ rol }`; 200 `{ usuarioId, rol }` (same-role request is a no-op 200: no Keycloak call, no event); 400 invalid `rol`; 404 user not found; 409 target user is currently `Administrador` (BR-R04, admin role immutable) or target user has an active team affiliation (only reachable when moving away from `Participante`, since only participants can belong to a team); 401/403 per above; 502 if the Keycloak realm-role swap fails; promotion to `Administrador` is allowed and is irreversible for that user |

> **Partial-failure consistency note (spec §5.2/§10):** the PUT diffs the desired permission set
> against the current one and applies the Keycloak composite add/remove calls **before**
> persisting to the DB (Keycloak-first). If Keycloak partially succeeds and a subsequent call in
> the same request fails (502), the composites already applied in Keycloak are **not** rolled
> back and the local `permisos_rol` table is **not** updated — Keycloak and the DB can drift
> apart for that role. Recovery is re-issuing the **same** PUT: the diff is recomputed against
> the (unchanged) DB state, so only the still-missing composites are (re)applied — the operation
> is idempotent and self-healing by construction. No automatic reconciliation runs at startup.

### Teams and invitations (policy `GestionarEquipos` — functional permission, not a role) (SP-5a re-homed + swap)

Paths re-homed under `identity/teams` (was `api/teams`), same SP-3g convention. **Policy swap
(SP-5a):** was `ParticipantOnly` (role `Participante`); now `GestionarEquipos` — the functional
permission (BR-R02 literal: the permission, not the role, authorizes team management). By the
BR-R03 default composite, `Participante` carries `GestionarEquipos`, so behavior for today's
users is unchanged; the enforcement point moved from role to permission.
Auth: `401` without a token; `403` without the `GestionarEquipos` permission.

> **There is no team access code (`codigoAcceso`).** Members join only via `InvitacionEquipo` sent by the team leader from a dynamic participant list (see `GET /identity/teams/eligible-participants`). Access-code join is not supported.

| Capability | Method | Path | Status | Notes |
|---|---|---|---|---|
| Create team | POST | `/identity/teams` | Registered | 201; creator becomes leader; response has no `codigoAcceso`; 401/403 per above |
| Leave team | DELETE | `/identity/teams/membership` | Registered | 200; leader with no other members deletes the team; 401/403 per above |
| Transfer leadership | PATCH | `/identity/teams/leadership` | Registered | 200; body `{ nuevoLiderUserId }`; 401/403 per above |
| Send invitation (leader) | POST | `/identity/teams/invitations` | Registered | 201; body `{ invitadoUserId }`; 401/403 per above; 409 if pending invitation already exists or invitee already in a team |
| Get received invitations (invitee inbox) | GET | `/identity/teams/invitations` | Registered | 200; returns pending invitations for the authenticated participant; 401/403 per above |
| Accept invitation | POST | `/identity/teams/invitations/{invitacionId}/acceptance` | Registered | 200; 401/403 per above; 409 if already in a team or team is full |
| Reject invitation | POST | `/identity/teams/invitations/{invitacionId}/rejection` | Registered | 200; 401/403 per above |
| Get eligible participants (leader) | GET | `/identity/teams/eligible-participants` | Registered | 200; dynamic list excluding participants already in a team; blocked when team is full; 401/403 per above |
| Get my active team | GET | `/identity/teams/mine` | Registered | 200 `{ equipoId, nombreEquipo, estado, participantes:[{ usuarioId, esLider }] }`; 404 if caller has no active team; 401/403 per above |
