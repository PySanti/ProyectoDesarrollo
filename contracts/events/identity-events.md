# Identity Events

## Status

Current event contract index. Concrete payloads require a current-doctrine SDD before implementation.

## Publisher

`Identity`

## Event Registry

| Event | Trigger | Payload (key fields) | Consumers | Status |
|---|---|---|---|---|
| `UsuarioCreado` | A local user is created after successful identity provisioning. | Defined by SDD | Defined by SDD | Payload not registered |
| `CredencialTemporalEmitida` | A temporary credential is issued or re-issued. | Defined by SDD | Defined by SDD | Payload not registered |
| `EquipoCreado` | A participant creates a new team and becomes its leader. | `{ equipoId, liderUserId, occurredOnUtc }` — **no `codigoAcceso`** | Defined by SDD | Registered |
| `InvitacionEquipoCreada` | The team leader sends an invitation to an eligible participant. | `{ invitacionEquipoId, equipoId, invitadoUserId, invitadoPorUserId, occurredOnUtc }` | Defined by SDD | Registered |
| `InvitacionEquipoAceptada` | An invited participant accepts the invitation and joins the team. | `{ invitacionEquipoId, equipoId, invitadoUserId, liderUserId, occurredOnUtc }` | Defined by SDD | Registered |
| `InvitacionEquipoRechazada` | An invited participant rejects the invitation. | `{ invitacionEquipoId, equipoId, invitadoUserId, occurredOnUtc }` | Defined by SDD | Registered |

## Rule

Concrete exchange names, queue names, routing keys, payloads, versions and idempotency rules are documented only after a current-doctrine SDD defines them.
