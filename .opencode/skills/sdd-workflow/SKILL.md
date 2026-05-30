---
name: sdd-workflow
description: Enforce Spec-Driven Development for every UMBRAL feature before coding.
compatibility: opencode
---

# SDD Workflow

Use this skill before any feature work.

## Required SDD folder

Every feature must have:

```txt
docs/04-sdd/specs/<HU-ID>-<short-name>/
  spec.md
  design.md
  tasks.md
  acceptance.md
```

## Required process

1. Identify the user story.
2. Confirm the user story appears in `docs/04-sdd/SPECS-LIST.md`.
3. Identify the owning microservice.
4. Read required context.
5. Read the related service context.
6. Read or create the related SDD folder.
7. Complete or refine `spec.md`.
8. Complete or refine `design.md`.
9. Complete or refine `tasks.md`.
10. Implement exactly one task at a time.
11. Update tests.
12. Update `acceptance.md`.
13. Update `docs/04-sdd/traceability-matrix.md`.

## Required output before coding

State explicitly:

- User story
- Owning microservice
- Supporting services
- Files read
- Current SDD status
- Resolved business decisions applied
- Task to implement
- Tests to update
- Contracts to update
- Traceability row to update

## TODO rule

If any of these files contains unresolved TODO sections, do not code:

- `spec.md`
- `design.md`
- `tasks.md`
- `acceptance.md`

Instead:

1. Complete the missing SDD content using project sources.
2. Report what changed.
3. Ask for user review before implementation.

## Required source files for SDD generation

Use actual filenames present in the project:

- `docs/01-project-source/srs.md`
- `docs/01-project-source/historias de usuario.md`
- `docs/01-project-source/modelo de dominio.md`
- `docs/01-project-source/diagrama de clases.md`
- `docs/01-project-source/microservicios.md`
- `docs/01-project-source/enunciado-proyecto.md`
- `docs/02-project-context/known-ambiguities-and-decisions.md`
- `docs/02-project-context/business-rules.md`
- `docs/02-project-context/first-delivery-scope.md`
- `docs/02-project-context/design/*.md`
- `docs/03-microservices/service-ownership.md`
- Related file under `docs/03-microservices/services/`
- Related contracts under `contracts/`

## Service topology rule

Valid owning services are only:

- Identity Service
- Team Service
- Trivia Game Service
- BDT Game Service

Stop if a feature points to Audit Service, Scoring Service, Trivia Service or Treasure Hunt Service as owning service.

## Resolved business decisions

Before generating or implementing SDD, enforce these resolved decisions:

- Team cardinality is 1 to 5 members.
- A team with one member is valid.
- The team creator is the first member and leader.
- Trivia score does not consider time.
- A correct Trivia answer adds the assigned question score directly.
- Timers remain valid for synchronization, closing and late-answer validation, but not for score calculation.

## HU-specific reminders

### Team-related HUs

The following must include team cardinality `1..5`:

- HU-03
- HU-04
- HU-05
- HU-06
- HU-07
- HU-13
- HU-14
- HU-19
- HU-40

### Trivia scoring HUs

The following must include direct scoring without time weighting:

- HU-26
- HU-27
- HU-28
- HU-29
- HU-30
