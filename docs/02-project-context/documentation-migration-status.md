# Documentation Migration Status

## Status

Current phase: documentation doctrine replacement complete.

## Authority

Active doctrine is derived only from:

- `docs/01-project-source/srs.md`
- `docs/01-project-source/diagrama-de-clases.md`
- `docs/01-project-source/modelo-de-dominio.md`
- `docs/01-project-source/microservicios.md`
- `CLAUDE.md`

## Source Snapshot

- Target services: `Identity`, `Partidas`, `Operaciones de Sesion`, `Puntuaciones`.
- Gateway: YARP is mandatory and is the single backend entry point for clients.
- Teams: owned by `Identity` in the target doctrine.
- BDT ranking: accumulated points from won stages, tie-break by lower accumulated time across won stages.
- SDD: old implementation SDDs are archived as legacy evidence before new specs are planned.

## Migration Checklist

| Area | State | Evidence |
|---|---|---|
| Source files | Captured | `docs/01-project-source/` and `CLAUDE.md` reviewed |
| Legacy SDD archive | Completed | `docs/04-sdd/_legacy-implementation-evidence/` created with archived specs, spec list, and traceability matrix |
| Project context docs | Completed | `docs/02-project-context/` regenerated for target doctrine |
| Microservice and gateway docs | Completed | `docs/03-microservices/` rewritten for target services and gateway |
| SDD workspace reset | Completed | `docs/04-sdd/` recreated as current-doctrine workspace |
| ADR supersession | Completed | `ADR-0008-documentation-doctrine-replacement.md` added and conflicting ADRs marked |
| Contract reset | Completed | Active contracts reset around gateway and target services; old contracts archived in `_legacy/` |
| Component contexts and guides | Completed | Root, client, gateway, service-context, and guide docs aligned to current doctrine |
| Consistency pass | Completed | Obsolete-doctrine search performed; remaining hits are legacy, superseded, or migration-debt notes |

## Remaining Work Outside This Migration

- Code/service-folder migration to target service names and databases.
- Runtime gateway and route implementation review.
- Regeneration of feature SDDs for the next selected implementation slice.
- Cleanup of remaining `.tsx`, `.ts`, and test copy/comments that still reference obsolete service names, legacy team access-code flows, or BDT ranking wording that reflects stages/time instead of point-based ranking.

## Legacy Policy

Old documentation that describes `Team Service`, `Trivia Game Service`, `BDT Game Service`, or BDT ranking by stages won can remain only when it is clearly marked as historical implementation evidence.
