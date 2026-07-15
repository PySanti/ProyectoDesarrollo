# Puntuaciones Events

## Status

Current event contract index. The SignalR payloads for the four ranking/score events below were
registered by SP-4c in `contracts/http/puntuaciones-api.md` §"SignalR — ranking en vivo (SP-4c)"
(single source of truth; not duplicated here).

## Publisher

`Puntuaciones`

## Event Registry

| Event | Trigger | Consumers | Status |
|---|---|---|---|
| `PuntajeTriviaIncrementado` | A participant or team receives direct Trivia score from a correct answer. | Clientes web/mobile vía SignalR | Registrado — ver `contracts/http/puntuaciones-api.md` §"SignalR — ranking en vivo (SP-4c)" |
| `RankingTriviaActualizado` | The Trivia native ranking changes. | Clientes web/mobile vía SignalR | Registrado — ídem |
| `RankingBDTActualizado` | The BDT native ranking changes by accumulated points from won stages. | Clientes web/mobile vía SignalR | Registrado — ídem |
| `RankingConsolidadoCalculado` | The consolidated partida ranking is computed on finish. | Clientes web/mobile vía SignalR | Registrado — ídem |

## Rule

The SignalR payloads for these four events are documented once, in
`contracts/http/puntuaciones-api.md` §"SignalR — ranking en vivo (SP-4c)" — do not duplicate them
here. Concrete RabbitMQ exchange names, queue names, routing keys, versions and idempotency rules
(if these events are ever also published as integration events) remain documented only after a
current-doctrine SDD defines them.
