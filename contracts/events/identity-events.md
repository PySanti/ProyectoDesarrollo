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
| `EquipoEliminado` (SP-Bloque4A) | A team is deleted by its leader (HU-06) or by an admin (HU-09). | `{ equipoId, nombreEquipo, origen, miembros[], occurredOnUtc }` — `origen` ∈ `"Lider"`\|`"Admin"` | Defined by SDD | Published to the broker (best-effort, ADR-0012) — routing key `identity.equipo-eliminado.v1` |
| `LiderazgoEquipoModificado` (SP-Bloque4A) | An admin reassigns a team's leadership among existing members (HU-09). | `{ equipoId, liderAnteriorUserId, nuevoLiderUserId, origen, occurredOnUtc }` — `origen` = `"Admin"` | Defined by SDD | Published to the broker (best-effort, ADR-0012) — routing key `identity.liderazgo-equipo-modificado.v1` |
| `EquipoDesactivado` (SP-Bloque4A) | An admin deactivates a team (HU-09; a `Desactivado` team cannot be inscribed in new partidas, BR-E10). | `{ equipoId, occurredOnUtc }` | Defined by SDD | Published to the broker (best-effort, ADR-0012) — routing key `identity.equipo-desactivado.v1` |
| `EquipoReactivado` (SP-Bloque4A) | An admin reactivates a previously deactivated team (HU-09). | `{ equipoId, occurredOnUtc }` | Defined by SDD | Published to the broker (best-effort, ADR-0012) — routing key `identity.equipo-reactivado.v1` |

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
| `EquipoEliminado` | `identity.equipo-eliminado.v1` |
| `LiderazgoEquipoModificado` | `identity.liderazgo-equipo-modificado.v1` |
| `EquipoDesactivado` | `identity.equipo-desactivado.v1` |
| `EquipoReactivado` | `identity.equipo-reactivado.v1` |

## Consumers (SP-Bloque4A)

Identity runs its **first consumer** (`OperacionesInscripcionesConsumer`, a `BackgroundService`) to maintain the local `participaciones_activas_equipo` projection that backs the BR-E10 delete guard.

- **Queue:** `identity.operaciones-sesion.participaciones` (durable), bound to exchange `umbral.operaciones-sesion` (topic, durable) on 4 routing keys: `operaciones-sesion.inscripcion-equipo-creada.v1`, `operaciones-sesion.inscripcion-equipo-cancelada.v1`, `operaciones-sesion.partida-finalizada.v1`, `operaciones-sesion.partida-cancelada.v1`.
- **Projection:** `InscripcionEquipoCreada` → upsert `(equipoId, partidaId)`; `InscripcionEquipoCancelada` → remove `(equipoId, partidaId)`; `PartidaFinalizada`/`PartidaCancelada` → remove all rows of that `partidaId`.
- **Semantics:** best-effort ack-always (ADR-0012) — malformed envelope / unknown event / handler failure (incl. `DbUpdateException`) is logged and acked, never requeued/poison-looped; the projection is reconstructible. Idempotent by the composite key. Does not start when RabbitMQ is disabled. Enabled via the `RabbitMqConsumer` config section (see `GUIA-LEVANTAMIENTO.md`).
- **Caveat (eventual consistency):** the guard is eventually consistent — an inscription made instants before a delete may not yet be projected. Accepted trade-off (chosen over synchronous cross-service HTTP) recorded in the slice design.

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
