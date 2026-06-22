# Current Implementation Scope

> Status: Current derived documentation. Source: `docs/01-project-source/` and `CLAUDE.md`.

## State

**No active implementation slice is defined under the current target doctrine yet.**

The current source set (`docs/01-project-source/` and `CLAUDE.md`) does not define a distinct "first delivery" slice. The legacy first-sprint SDDs that previously drove implementation were archived during the documentation migration (see `docs/04-sdd/_legacy-implementation-evidence/`). New SDDs will be created per slice under the SDD workflow before any new implementation begins.

This document therefore records the **scope rules** that any future slice must follow, not a fixed list of active stories.

## How scope will be defined

1. New work is planned **per slice**, one service/slice at a time, following the SDD workflow under `docs/04-sdd/`.
2. Each user story (`HU-xx`) selected for a slice must have its spec registered in `docs/04-sdd/SPECS-LIST.md` and a folder under `docs/04-sdd/specs/<HU>/` before implementation.
3. The four-service migration itself is driven the same way, governed by its ADR under `docs/05-decisions/`.

## Client routing rule (from the SRS)

- Stories whose principal actor is `Administrador` / `Operador` → **web** (React).
- Stories whose principal actor is `Participante` (including `Líder de equipo` acting as participant) → **mobile** (React Native).
- Stories whose principal actor is `Sistema` → **backend**.

`Líder de equipo` is a business attribute, not a Keycloak role. Do not implement participant gameplay in web, or admin/operator screens in mobile, unless an SDD explicitly says so.

## Owning-service rule (target topology)

Each story's owning service is one of the four target services — **Identity**, **Partidas**, **Operaciones de Sesion**, **Puntuaciones** — and never the obsolete `Team Service`, `Trivia Game Service`, or `BDT Game Service`:

- User/role/governance and team/invitation/team-history stories → **Identity**.
- `Partida`/`Juego` configuration, Trivia `Pregunta`s, BDT `EtapaBDT`s → **Partidas**.
- Live runtime, inscriptions, and convocatorias → **Operaciones de Sesion**.
- Scoring, native and consolidated ranking, team-performance queries, audit/history materialization → **Puntuaciones**.

## Rule

No HU may be implemented unless it is registered in `docs/04-sdd/SPECS-LIST.md` and has completed (TODO-free) `spec.md` → `design.md` → `tasks.md` before coding.
