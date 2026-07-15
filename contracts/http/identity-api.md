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
| Delete my team (leader, HU-06) | DELETE | `/identity/teams/mine` | Registered (SP-Bloque4A) | 204; the leader deletes their own team **even with members** (soft-delete `Estado=Eliminado`; frees members; deletes pending invitations, BR-E06; publishes `EquipoEliminado` + notifies members); 401/403 (not leader) per above; 404 if caller has no active team; **409 if the team has an active participation in a `Lobby`/`Iniciada` partida (BR-E10)** |
| Get my team-name history (HU-48) | GET | `/identity/teams/mine/history` | Registered (SP-Bloque4A) | 200 `{ historial: [{ nombreEquipo, equipoId, fechaRegistro }] }` ordered ascending by `fechaRegistro`; **always 200, empty list if none** (BR-E11); 401 per above |

### Teams listing for the web console (policy `OperadorOAdministrador` — Bloque 3b)

Read-only listing for the admin/operator web view. Unlike the rest of this section, this endpoint
is NOT under `GestionarEquipos`: it uses the role-based policy `OperadorOAdministrador`
(`RequireRole("Operador", "Administrador")`), enforced both at the gateway (route
`identity-teams-listing`, exact path + `GET` only, Order 0) and inside Identity
(`TeamsAdminController`). `POST /identity/teams` and `GET /identity/teams/mine` keep their
existing policies. Auth: `401` without a token; `403` without `Operador`/`Administrador`.

| Capability | Method | Path | Status | Notes |
|---|---|---|---|---|
| List all teams | GET | `/identity/teams` | Registered | 200 `[{ equipoId, nombreEquipo, estado, participantes:[{ usuarioId, nombre, esLider }] }]`; ALL states (`Activo`/`Desactivado`/`Eliminado`), ordered by `nombreEquipo` asc; empty → `200 []`. `usuarioId` is the Keycloak `sub`; `nombre` resolved via the local user reference (`Usuario.KeycloakId`), `""` when no local row exists. |

### Directorio de nombres (policy `Default` — cualquier usuario autenticado)

Resuelve lotes de ids de competidor a nombres, para que las pantallas de operador y de
participante pinten nombres en vez de GUIDs. Es el **único** endpoint de Identity alcanzable por
`Participante` fuera de `/identity/teams/**`: el móvil lo necesita para el ranking en vivo. No
usa `AdminOnly` a propósito — ver el caveat de exposición en el spec
`docs/superpowers/specs/2026-07-14-nombres-competidores-design.md`.

| Capability | Method | Path | Status | Notes |
|---|---|---|---|---|
| Resolver nombres de competidores | POST | `/identity/directory/names` | Registered | 200; body `{ participanteIds: [guid], equipoIds: [guid] }` (ambas opcionales, default `[]`); 400 si `participanteIds.length + equipoIds.length > 200`; 401 sin token |

Respuesta:

```json
{
  "participantes": [{ "participanteId": "guid", "nombre": "string" }],
  "equipos": [{ "equipoId": "guid", "nombreEquipo": "string" }]
}
```

- `participanteId` es el **sub de Keycloak** (la identidad dual slice-E del `competidorId` en
  modalidad `Individual`), resuelto contra `Usuario.KeycloakId`. `equipoId` es `Equipo.EquipoId`.
- **Un id que no resuelve se omite de la respuesta** — no se devuelve `""`. Esto difiere de
  `GET /identity/teams`, que sí usa `""`: aquí la omisión deja que el cliente caiga al GUID corto.
- Los nombres son siempre los **actuales**; este endpoint no consulta el historial de nombres de
  equipo (BR-E11).

### Admin team management (policy `AdminOnly` — role `Administrador`) (SP-Bloque4A, HU-09)

Base path `identity/admin/teams`. Auth: `401` without a token; `403` without role `Administrador` (same policy as `GovernanceController`). The admin does **not** compose membership (BR-E05 intact): create = name + a valid leader (sole initial member); edit = rename + reassign leadership among existing members. `EquipoAdminResponse = { equipoId, nombreEquipo, estado, liderUserId?, integrantes:[{ usuarioId, esLider }] }`.

> **Leader identity on create:** the `liderUserId` in the create body is the leader's **local `Usuario.UsuarioId`** (the id the admin user directory `GET /identity/users` exposes). Identity resolves it to the Keycloak-subject membership key server-side, so the created team's leader can access it from mobile. Reassign-leadership's `nuevoLiderUserId` is a current member's `usuarioId` (already in subject space).

| Capability | Method | Path | Status | Notes |
|---|---|---|---|---|
| List teams | GET | `/identity/admin/teams` | Registered (SP-Bloque4A) | 200 `EquipoAdminResponse[]` — **all** states (Activo/Desactivado/Eliminado); 401/403 per above |
| Get team detail | GET | `/identity/admin/teams/{id}` | Registered (SP-Bloque4A) | 200 `EquipoAdminResponse`; 404 if not found; 401/403 per above |
| Create team | POST | `/identity/admin/teams` | Registered (SP-Bloque4A) | 201 + Location `/identity/admin/teams/{equipoId}`; body `{ nombreEquipo, liderUserId }` (see leader-identity note); 404 if leader user not found; 409 if leader already in an active team; 400 on validation; 401/403 per above |
| Rename team | PATCH | `/identity/admin/teams/{id}/name` | Registered (SP-Bloque4A) | 200 `EquipoAdminResponse`; body `{ nombreEquipo }` (records one name-history row per current member, BR-E11); 404 if not found; 400 on validation; 401/403 per above |
| Reassign leadership | PATCH | `/identity/admin/teams/{id}/leadership` | Registered (SP-Bloque4A) | 200 `EquipoAdminResponse`; body `{ nuevoLiderUserId }` (an existing member; publishes `LiderazgoEquipoModificado`, notifies both leaders); 404 if not found; 409 if the new leader is not a member / equals the current leader; 400 on validation; 401/403 per above |
| Change state | PATCH | `/identity/admin/teams/{id}/estado` | Registered (SP-Bloque4A) | 200 `EquipoAdminResponse`; body `{ estado }` ∈ `"Activo"`\|`"Desactivado"` (Activo↔Desactivado only; a `Desactivado` team cannot be inscribed in new partidas, BR-E10; publishes `EquipoDesactivado`/`EquipoReactivado`); 404 if not found; 400 on validation; 401/403 per above |
| Delete team | DELETE | `/identity/admin/teams/{id}` | Registered (SP-Bloque4A) | 204 (soft-delete + delete pending invitations + `EquipoEliminado` `origen:"Admin"` + notify members); 404 if not found; **409 if the team has an active participation in a `Lobby`/`Iniciada` partida (BR-E10)**; 401/403 per above |
