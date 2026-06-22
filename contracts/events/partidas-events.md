# Partidas Events

## Status

Current event contract index. Concrete payloads require a current-doctrine SDD before implementation.

## Publisher

`Partidas`

## Event Registry

| Event | Trigger | Consumers | Status |
|---|---|---|---|
| Configuration events | Defined by SDD when a configuration fact must be published. | Defined by SDD | Not registered |

## Rule

Partidas owns configuration. Runtime publication, game activation and scoring events belong to their owning current target services, not to Partidas.
