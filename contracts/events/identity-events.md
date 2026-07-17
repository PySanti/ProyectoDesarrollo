# Identity Events

## Status

Current event contract index. Concrete payloads require a current-doctrine SDD before implementation.

## Publisher

`Identity`

## Event Registry

| Event | Trigger | Payload (key fields) | Consumers | Status |
|---|---|---|---|---|
| `UsuarioCreado` | A local user is created after successful identity provisioning. | Defined by SDD | Defined by SDD | Payload not registered — diferido al slice de audit/notificaciones (SP-5b no los emite) |
| `CredencialTemporalEmitida` (7f, RNF-23 / BR-R05) | A local user is created (`POST /identity/users`) with an initial temporary password. Also emitted when a user's email is changed (`PATCH /identity/users/{userId}`) while they still have a pending temporary password (BR-R05 re-issue). | `{ nombre, correo, rol, passwordTemporal, occurredOnUtc }` | Defined by SDD (async welcome-email sender, RNF-23) | Published to the broker since 7f (best-effort, ADR-0012) — routing key `identity.credencial-temporal-emitida.v1` |
| `EquipoCreado` | A participant creates a new team and becomes its leader. | `{ equipoId, liderUserId, occurredOnUtc }` — **no `codigoAcceso`** | Defined by SDD | Published to the broker since SP-5b (best-effort, ADR-0012) — routing key `identity.equipo-creado.v1` |
| `InvitacionEquipoCreada` | The team leader sends an invitation to an eligible participant. | `{ invitacionEquipoId, equipoId, invitadoUserId, invitadoPorUserId, occurredOnUtc }` | Defined by SDD | Published to the broker since SP-5b (best-effort, ADR-0012) — routing key `identity.invitacion-equipo-creada.v1` |
| `InvitacionEquipoAceptada` | An invited participant accepts the invitation and joins the team. | `{ invitacionEquipoId, equipoId, invitadoUserId, liderUserId, occurredOnUtc }` | Defined by SDD | Published to the broker since SP-5b (best-effort, ADR-0012) — routing key `identity.invitacion-equipo-aceptada.v1` |
| `InvitacionEquipoRechazada` | An invited participant rejects the invitation. | `{ invitacionEquipoId, equipoId, invitadoUserId, occurredOnUtc }` | Defined by SDD | Published to the broker since SP-5b (best-effort, ADR-0012) — routing key `identity.invitacion-equipo-rechazada.v1` |
| `RolUsuarioModificado` (SP-5b) | An admin changes a user's role (never an admin's own role). | `{ usuarioId, rolAnterior, rolNuevo, occurredOnUtc }` | Defined by SDD | Published to the broker since SP-5b (best-effort, ADR-0012) — routing key `identity.rol-usuario-modificado.v1` |
| `PermisosRolActualizados` (SP-5b) | An admin replaces the functional-permission set of a role via the governance panel. | `{ rol, permisos[], occurredOnUtc }` | Defined by SDD | Published to the broker since SP-5b (best-effort, ADR-0012) — routing key `identity.permisos-rol-actualizados.v1` |
| `EquipoEliminado` (SP-Bloque4A) | A team is deleted by its leader (HU-06) or by an admin (HU-09). | `{ equipoId, nombreEquipo, origen, miembros[], occurredOnUtc }` — `origen` ∈ `"Lider"`\|`"Admin"`; `miembros[]` are **Keycloak subject ids** | **Identity itself** (`CredencialesTemporalesConsumer` → member email, RB-E15) | Published to the broker (best-effort, ADR-0012) — routing key `identity.equipo-eliminado.v1` |
| `LiderazgoEquipoModificado` (SP-Bloque4A) | An admin reassigns a team's leadership among existing members (HU-09). | `{ equipoId, liderAnteriorUserId, nuevoLiderUserId, origen, occurredOnUtc }` — `origen` = `"Admin"`; both leader ids are **Keycloak subject ids** | **Identity itself** (`CredencialesTemporalesConsumer` → email to both leaders, HU-09) | Published to the broker (best-effort, ADR-0012) — routing key `identity.liderazgo-equipo-modificado.v1` |
| `EquipoEliminado` (SP-Bloque4A) | A team is deleted by its leader (HU-06) or by an admin (HU-09). | `{ equipoId, nombreEquipo, origen, miembros[], occurredOnUtc }` — `origen` ∈ `"Lider"`\|`"Admin"` | Defined by SDD | Published to the broker (best-effort, ADR-0012) — routing key `identity.equipo-eliminado.v1` |
| `LiderazgoEquipoModificado` (SP-Bloque4A) | An admin reassigns a team's leadership among existing members (HU-09). | `{ equipoId, liderAnteriorUserId, nuevoLiderUserId, origen, occurredOnUtc }` — `origen` = `"Admin"` | Defined by SDD | Published to the broker (best-effort, ADR-0012) — routing key `identity.liderazgo-equipo-modificado.v1` |
| `EquipoDesactivado` (SP-Bloque4A) | An admin deactivates a team (HU-09; a `Desactivado` team cannot be inscribed in new partidas, BR-E10). | `{ equipoId, occurredOnUtc }` | Defined by SDD | Published to the broker (best-effort, ADR-0012) — routing key `identity.equipo-desactivado.v1` |
| `EquipoReactivado` (SP-Bloque4A) | An admin reactivates a previously deactivated team (HU-09). | `{ equipoId, occurredOnUtc }` | Defined by SDD | Published to the broker (best-effort, ADR-0012) — routing key `identity.equipo-reactivado.v1` |

> **Scope note (7e, HU-43):** `InvitacionEquipoCreada`/`InvitacionEquipoAceptada`/`InvitacionEquipoRechazada` are team lifecycle, not partida lifecycle — they are published to the `umbral.identity` exchange, which Puntuaciones does not consume. They are **not** archived in the partida historial (`HistorialEventMapper` only maps `umbral.operaciones-sesion` events). This is a documented scope cut, not a gap: no new Identity consumer is planned for the historial.

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
| `CredencialTemporalEmitida` | `identity.credencial-temporal-emitida.v1` |

## Consumers (SP-Bloque4A)

Identity runs its **first consumer** (`OperacionesInscripcionesConsumer`, a `BackgroundService`) to maintain the local `participaciones_activas_equipo` projection that backs the BR-E10 delete guard.

- **Queue:** `identity.operaciones-sesion.participaciones` (durable), bound to exchange `umbral.operaciones-sesion` (topic, durable) on 4 routing keys: `operaciones-sesion.inscripcion-equipo-creada.v1`, `operaciones-sesion.inscripcion-equipo-cancelada.v1`, `operaciones-sesion.partida-finalizada.v1`, `operaciones-sesion.partida-cancelada.v1`.
- **Projection:** `InscripcionEquipoCreada` → upsert `(equipoId, partidaId)`; `InscripcionEquipoCancelada` → remove `(equipoId, partidaId)`; `PartidaFinalizada`/`PartidaCancelada` → remove all rows of that `partidaId`.
- **Semantics:** best-effort ack-always (ADR-0012) — malformed envelope / unknown event / handler failure (incl. `DbUpdateException`) is logged and acked, never requeued/poison-looped; the projection is reconstructible. Idempotent by the composite key. Does not start when RabbitMQ is disabled. Enabled via the `RabbitMqConsumer` config section (see `GUIA-LEVANTAMIENTO.md`).
- **Caveat (eventual consistency):** the guard is eventually consistent — an inscription made instants before a delete may not yet be projected. Accepted trade-off (chosen over synchronous cross-service HTTP) recorded in the slice design.

Identity runs a **second consumer** (`CredencialesTemporalesConsumer`, a `BackgroundService`, 7f/RNF-23) — the **Identity email queue**: Identity **self-consumes** its own events to send SMTP mail asynchronously, decoupled from the domain operation that publishes them.

- **Queue:** `identity.correo-credenciales` (durable), bound to Identity's **own** exchange `umbral.identity` (topic, durable) on 3 routing keys: `identity.credencial-temporal-emitida.v1`, `identity.equipo-eliminado.v1` and `identity.liderazgo-equipo-modificado.v1`. The queue/class names predate the extra bindings and are kept for compatibility with the deployed durable queue and config section.
- **Dispatch by `eventType`:**
  - `CredencialTemporalEmitida` → payload `{ nombre, correo, rol, passwordTemporal }` → `UserWelcomeEmailMessage` → `IUserWelcomeEmailSender.SendWelcomeEmailAsync` (`SmtpUserWelcomeEmailSender`). Decoupled from both triggers that publish it (`POST /identity/users` creation and `PATCH /identity/users/{userId}` email-change re-issue, BR-R05).
  - `EquipoEliminado` → payload `{ nombreEquipo, miembros[] }` → `ITeamLifecycleNotifier.NotificarEquipoEliminadoAsync` (`SmtpTeamLifecycleNotifier`), which notifies every member (RB-E15). This is what keeps the member email off the `DELETE /identity/teams/mine` and `DELETE /identity/admin/teams/{id}` request paths.
  - `LiderazgoEquipoModificado` → payload `{ liderAnteriorUserId, nuevoLiderUserId }` → `ITeamLifecycleNotifier.NotificarLiderazgoModificadoAsync`, which notifies both leaders (HU-09). Keeps the email off the `PATCH /identity/admin/teams/{id}/leadership` request path.
  - Recipient ids in both team events are already **Keycloak subject ids**, the space the notifier resolves recipients in — no id translation in the consumer.
- **Semantics:** best-effort ack-always (ADR-0012) — a malformed envelope, unexpected `eventType`, an incomplete payload, or SMTP failure (`EmailDeliveryException`) is logged and acked, never requeued/poison-looped. Does not start when RabbitMQ is disabled. Enabled via the `RabbitMqCredencialesConsumer` config section (see `GUIA-LEVANTAMIENTO.md`).

> **No email runs inside a request anymore.** `SmtpTeamLifecycleNotifier` is consumer-only: every team-lifecycle email is triggered by an event, so its SMTP budget (10s) is **per recipient** rather than shared across the operation — a slow SMTP with the first recipient can no longer leave the rest unnotified.
Identity runs a **second consumer** (`CredencialesTemporalesConsumer`, a `BackgroundService`, 7f/RNF-23): Identity **self-consumes** its own `CredencialTemporalEmitida` event to send the SMTP welcome email asynchronously, decoupled from both triggers that publish it (`POST /identity/users` creation and `PATCH /identity/users/{userId}` email-change re-issue, BR-R05).

- **Queue:** `identity.correo-credenciales` (durable), bound to Identity's **own** exchange `umbral.identity` (topic, durable) on 1 routing key: `identity.credencial-temporal-emitida.v1`.
- **Dispatch:** deserializes the envelope payload (`{ nombre, correo, rol, passwordTemporal }`) into `UserWelcomeEmailMessage` and calls `IUserWelcomeEmailSender.SendWelcomeEmailAsync` (implemented by `SmtpUserWelcomeEmailSender`).
- **Semantics:** best-effort ack-always (ADR-0012) — a malformed envelope, unexpected `eventType`, or SMTP failure (`EmailDeliveryException`) is logged and acked, never requeued/poison-looped. Does not start when RabbitMQ is disabled. Enabled via the `RabbitMqCredencialesConsumer` config section (see `GUIA-LEVANTAMIENTO.md`).

## Payloads (registered, SP-5b)

### `CredencialTemporalEmitida` (7f, RNF-23 / BR-R05)

Emitted from two triggers, both handled by `IIdentityEventsPublisher.PublishCredencialTemporalEmitidaAsync`
(no command handler depends on `IUserWelcomeEmailSender` directly anymore — only the consumer below does):

- **Creation** — after `POST /identity/users` (`CreateUserWithInitialRoleCommandHandler`) persists the
  new user locally, following a successful Keycloak provisioning. Replaces the previous inline SMTP
  `await` + all-or-nothing compensation: a publication failure is logged but **never** compensates
  (deletes) the already-created user — only the Keycloak↔local transactional compensation (local
  persistence failing after a successful Keycloak create) remains.
- **Email change with pending temporary credential (BR-R05)** — after `PATCH /identity/users/{userId}`
  (`UpdateUserGeneralDataCommandHandler`) detects the email changed **and** the user still has a
  pending temporary password (`HasTemporaryPasswordAsync`): the original temporary email is
  irrecoverable (RB-U03), so the handler syncs the new email to Keycloak, resets the temporary
  password, and publishes this event with the new password so it reaches the user. If the user has
  no pending temporary credential, nothing is resent/published (unchanged behavior). A Keycloak-side
  failure (email sync or password reset) still reverts the local + Keycloak email change, as before;
  a publication failure alone does not, since publishing is best-effort (ADR-0012).

```json
{ "nombre": "string", "correo": "string", "rol": "Administrador | Operador | Participante", "passwordTemporal": "string", "occurredOnUtc": "datetime" }
```

> **Security note:** the temporary password travels in the payload. This is accepted because
> `umbral.identity` is an **internal** exchange (RabbitMQ, not exposed to clients) — the async
> welcome-email consumer needs the plaintext value once, since it is never persisted (RB-U03) and
> the corresponding SMTP sender is not implemented in this slice.

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
