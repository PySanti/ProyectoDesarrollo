# Identity Events

## Status

Current event contract index. Concrete payloads require a current-doctrine SDD before implementation.

## Publisher

`Identity`

## Event Registry

| Event | Trigger | Consumers | Status |
|---|---|---|---|
| `UsuarioCreado` | A local user is created after successful identity provisioning. | Defined by SDD | Payload not registered |
| `CredencialTemporalEmitida` | A temporary credential is issued or re-issued. | Defined by SDD | Payload not registered |

## Rule

Concrete exchange names, queue names, routing keys, payloads, versions and idempotency rules are documented only after a current-doctrine SDD defines them.
