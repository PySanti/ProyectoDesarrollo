# Source Priority — UMBRAL

> Status: Current derived documentation. Source: `docs/01-project-source/` and `CLAUDE.md`.

This file defines how to resolve documentation priority when the source documents differ.

## Recommended priority

1. `CLAUDE.md` for operational target doctrine and repository rules.
2. `docs/01-project-source/srs.md` for requirements, rules, actors, and scope.
3. `docs/01-project-source/modelo-de-dominio.md` for domain concepts and invariants.
4. `docs/01-project-source/diagrama-de-clases.md` for tactical classes and relationships.
5. `docs/01-project-source/microservicios.md` for target service ownership.
6. Derived docs under `docs/02-project-context/`.
7. Accepted ADRs under `docs/05-decisions/` refine the above when present.

The target service boundaries are **Identity**, **Partidas**, **Operaciones de Sesion**, and **Puntuaciones** behind a mandatory YARP gateway. Where a source still uses the older `Equipos` / `Trivia` / `BDT` bounded-context naming, those are **logical** contexts that materialize onto the four target services (teams inside Identity; Trivia/BDT split across Partidas configuration, Operaciones de Sesion runtime, and Puntuaciones scoring).

## Rule on contradiction

When a functional rule of the SRS contradicts a design in the domain model or class diagram:

1. Do not write code yet.
2. Record the conflict in the SDD of the story.
3. Consult `known-ambiguities-and-decisions.md`.
4. Choose an explicit decision before implementing.
5. If a decision is taken, update the SRS, the domain model, or an ADR as appropriate.

## Derived-source rule

Files under `docs/02-project-context/` are not primary sources. They are operational guides. If a context file is found to contradict the project source or `CLAUDE.md`, the context file must be corrected.
