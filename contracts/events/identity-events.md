# Identity Events

## Status

Current event contract index. Concrete payloads require a current-doctrine SDD before implementation.

## Publisher

`Identity`

## Event Registry

| Event | Trigger | Payload (key fields) | Consumers | Status |
|---|---|---|---|---|
| `UsuarioCreado` | A local user is created after successful identity provisioning. | Defined by SDD | Defined by SDD | Payload not registered — diferido al slice de audit/notificaciones (SP-5b no los emite) |
| `CredencialTemporalEmitida` | A temporary credential is issued or re-issued. | Defined by SDD | Defined by SDD | Payload not registered — diferido al slice de audit/notificaciones (SP-5b no los emite) |
| `EquipoCreado` | A participant creates a new team and becomes its leader. | `{ equipoId, liderUserId, occurredOnUtc }` — **no `codigoAcceso`** | Defined by SDD | Published to the broker since SP-5b (best-effort, ADR-0012) — routing key `identity.equipo-creado.v1` |
| `InvitacionEquipoCreada` | The team leader sends an invitation to an eligible participant. | `{ invitacionEquipoId, equipoId, invitadoUserId, invitadoPorUserId, occurredOnUtc }` | Defined by SDD | Published to the broker since SP-5b (best-effort, ADR-0012) — routing key `identity.invitacion-equipo-creada.v1` |
| `InvitacionEquipoAceptada` | An invited participant accepts the invitation and joins the team. | `{ invitacionEquipoId, equipoId, invitadoUserId, liderUserId, occurredOnUtc }` | Defined by SDD | Published to the broker since SP-5b (best-effort, ADR-0012) — routing key `identity.invitacion-equipo-aceptada.v1` |
| `InvitacionEquipoRechazada` | An invited participant rejects the invitation. | `{ invitacionEquipoId, equipoId, invitadoUserId, occurredOnUtc }` | Defined by SDD | Published to the broker since SP-5b (best-effort, ADR-0012) — routing key `identity.invitacion-equipo-rechazada.v1` |
| `RolUsuarioModificado` (SP-5b) | An admin changes a user's role (never an admin's own role). | `{ usuarioId, rolAnterior, rolNuevo, occurredOnUtc }` | Defined by SDD | Published to the broker since SP-5b (best-effort, ADR-0012) — routing key `identity.rol-usuario-modificado.v1` |
| `PermisosRolActualizados` (SP-5b) | An admin replaces the functional-permission set of a role via the governance panel. | `{ rol, permisos[], occurredOnUtc }` | Defined by SDD | Published to the broker since SP-5b (best-effort, ADR-0012) — routing key `identity.permisos-rol-actualizados.v1` |

## Transport (SP-5b)

Events are published to RabbitMQ (best-effort, after `SaveChanges`; see ADR-0012). Delivery to
the broker is enabled per environment via `RabbitMq__Enabled`. Mirrors the pattern established
by `operaciones-sesion-events.md` §Transport (SP-3i).

- **Exchange:** `umbral.identity` — type `topic`, durable. Convention: one exchange per producing service.
- **Routing key:** `identity.<event-kebab>.v1` (explicit map, table below — no algorithmic kebab-case; see `IdentityEventRouting`). Incompatible payload changes bump to `.v2` (new key; `v1` consumers keep working).
- **Envelope** (JSON camelCase, `content_type: application/json`): `{ "eventId": "guid", "eventType": "PascalCase name", "version": 1, "occurredAt": "datetime (UTC)", "payload": { …documented shape… } }`. Producers do not guarantee exactly-once; **consumers deduplicate by `eventId`**.
- **Publication:** best-effort post-save — a broker failure is logged and never surfaces to the caller or blocks the HTTP response (ADR-0012).
- **Governance propagation (ADR-0013):** `PermisosRolActualizados` and `RolUsuarioModificado` are emitted only after the corresponding Keycloak change (composites / realm role) has already succeeded and the local DB write has committed — the event reflects state already durable in both systems, never a pending or rolled-back change.

| Event | Routing key |
|---|---|
| `EquipoCreado` | `identity.equipo-creado.v1` |
| `InvitacionEquipoCreada` | `identity.invitacion-equipo-creada.v1` |
| `InvitacionEquipoAceptada` | `identity.invitacion-equipo-aceptada.v1` |
| `InvitacionEquipoRechazada` | `identity.invitacion-equipo-rechazada.v1` |
| `RolUsuarioModificado` | `identity.rol-usuario-modificado.v1` |
| `PermisosRolActualizados` | `identity.permisos-rol-actualizados.v1` |

## Payloads (registered, SP-5b)

### `RolUsuarioModificado` (SP-5b)

Emitted after `PATCH /identity/users/{userId}/role` changes a user's role (Keycloak-first; no-op same-role does not emit).

```json
{ "usuarioId": "guid", "rolAnterior": "Administrador | Operador | Participante", "rolNuevo": "Administrador | Operador | Participante", "occurredOnUtc": "datetime" }
```

### `PermisosRolActualizados` (SP-5b)

Emitted after `PUT /identity/governance/roles/{rol}/permisos` persists a new permission set for a role (Keycloak-first; empty diff does not emit). `permisos` is the final desired set, not a delta.

```json
{ "rol": "Administrador | Operador | Participante", "permisos": ["GestionarPartidas", "GestionarEquipos", "ParticiparEnPartidas"], "occurredOnUtc": "datetime" }
```
