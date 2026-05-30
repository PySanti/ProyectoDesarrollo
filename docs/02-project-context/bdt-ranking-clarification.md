# BDT Ranking Clarification

## Decision

Búsqueda del Tesoro ranking is not based on numeric accumulated score.

The active BDT ranking rule is:

```txt
1. More stages won ranks higher.
2. If tied, lower accumulated time across won stages ranks higher.
```

## Authoritative concepts

Use:

- `EtapasGanadas`
- `TiempoAcumuladoEtapasGanadas`
- `TiempoResolucionEtapa`
- `RankingBDT`
- `RankingBDTActualizado`

## Deprecated or forbidden as active BDT ranking concepts

Do not use these concepts as active BDT ranking rules:

- `PuntajeEtapa`
- `PuntajeAcumulado` for BDT ranking
- `PuntajeBDTIncrementado`
- `BDT scoring` as numeric score accumulation

## Allowed usage of puntaje wording

The word `puntaje` may appear in generic academic wording or historical traceability, but BDT implementation must not calculate ranking from numeric score.

If a BDT feature needs ranking, the SDD must explicitly use:

```txt
Ranking BDT = EtapasGanadas DESC + TiempoAcumuladoEtapasGanadas ASC
```

## Related files to keep consistent

- `AGENTS.md`
- `.opencode/skills/ddd-modeling/SKILL.md`
- `.opencode/skills/rabbitmq-events/SKILL.md`
- `services/bdt-game-service/service-context.md`
- `docs/03-microservices/microservices-map.md`
- `docs/03-microservices/services/bdt-game-service.md`
- `contracts/events/bdt-game-events.md`
- `contracts/http/bdt-game-api.md`
