# UMBRAL Project Context

This directory contains current derived context for the target UMBRAL doctrine.

## Authority

The source of truth is `docs/01-project-source/` plus `CLAUDE.md`. Files here are derived and must be corrected when they contradict the source files.

## Target Architecture

- `Identity`
- `Partidas`
- `Operaciones de Sesion`
- `Puntuaciones`
- Mandatory YARP gateway

## Current Files

| File | Purpose |
|---|---|
| `project-brief.md` | Product and architecture summary |
| `srs-summary.md` | SRS-derived requirements summary |
| `business-rules.md` | Normalized business rules |
| `first-delivery-scope.md` | Current implementation-scope summary if defined by the source docs |
| `glossary.md` | Ubiquitous language |
| `domain-model-summary.md` | Aggregates, entities, events, and services |
| `class-design-summary.md` | Class design summary |
| `source-priority.md` | Source priority and contradiction rules |
| `known-ambiguities-and-decisions.md` | Current decisions and open questions |
| `documentation-migration-status.md` | Migration checklist |
| `adaptation-to-academic-brief.md` | Academic-brief vocabulary mapping (Mission → Partida, etc.) |
| `bdt-ranking-clarification.md` | Point-based BDT ranking clarification (supersedes stages-won rule) |
| `mobile-participant-context.md` | Mobile client ownership and participant-scope boundaries |
| `patch-snippets.md` | Historical meta migration artifact — not active planning input |
| `SETUP-PERTINENCE-PATCH-SUMMARY.md` | Historical pre-migration setup-patch summary — not active planning input |

## Design Files

| File | Purpose |
|---|---|
| `design/design-index.md` | Design index for SDD work |
| `design/domain-business-rules.md` | Rules placed inside aggregates and domain services |
| `design/domain-entities-by-context.md` | Entities, aggregates, value objects, and enums per bounded context, mapped to the target services |
| `design/class-design-by-layer.md` | Class design translated to Clean/Hexagonal layers per service |
| `design/service-model-impact.md` | Impact of the domain model on the four target services |
| `design/design-patterns-catalog.md` | Design-pattern policy for features |

## Reading Rule

Before creating or modifying code, read at minimum:

1. `project-brief.md`
2. `srs-summary.md`
3. `business-rules.md`
4. `first-delivery-scope.md`
5. `domain-model-summary.md`
6. `design/domain-entities-by-context.md`
7. `design/class-design-by-layer.md`
8. `design/service-model-impact.md`
9. `known-ambiguities-and-decisions.md`

No user story may be implemented directly from a prompt: an SDD must exist under `docs/04-sdd/specs/<HU>/` first.
