# Operaciones de Sesion Events

## Status

Current event contract index. Concrete payloads require a current-doctrine SDD before implementation.

## Publisher

`Operaciones de Sesion`

## Event Registry

| Event | Trigger | Consumers | Status |
|---|---|---|---|
| `PartidaPublicadaEnLobby` | A partida is published and moved to lobby. | Defined by SDD | Payload not registered |
| `PartidaIniciada` | A partida starts manually or automatically. | Defined by SDD | Payload not registered |
| `JuegoActivado` | The next sequential game becomes active. | Defined by SDD | Payload not registered |
| `RespuestaTriviaValidada` | A Trivia answer is validated. | Defined by SDD | Payload not registered |
| `TesoroQRValidado` | A BDT QR treasure is decoded and validated. | Defined by SDD | Payload not registered |
| `EtapaBDTGanada` | A BDT stage is won and carries the configured stage score. | Defined by SDD | Payload not registered |
| `PartidaFinalizada` | A partida finishes. | Defined by SDD | Payload not registered |

## Rule

Concrete exchange names, queue names, routing keys, payloads, versions and idempotency rules are documented only after a current-doctrine SDD defines them.
