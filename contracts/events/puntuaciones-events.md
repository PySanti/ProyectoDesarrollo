# Puntuaciones Events

## Status

Current event contract index. Concrete payloads require a current-doctrine SDD before implementation.

## Publisher

`Puntuaciones`

## Event Registry

| Event | Trigger | Consumers | Status |
|---|---|---|---|
| `PuntajeTriviaIncrementado` | A participant or team receives direct Trivia score from a correct answer. | Defined by SDD | Payload not registered |
| `RankingTriviaActualizado` | The Trivia native ranking changes. | Defined by SDD | Payload not registered |
| `RankingBDTActualizado` | The BDT native ranking changes by accumulated points from won stages. | Defined by SDD | Payload not registered |
| `RankingConsolidadoCalculado` | The consolidated partida ranking is computed on finish. | Defined by SDD | Payload not registered |

## Rule

Concrete exchange names, queue names, routing keys, payloads, versions and idempotency rules are documented only after a current-doctrine SDD defines them.
