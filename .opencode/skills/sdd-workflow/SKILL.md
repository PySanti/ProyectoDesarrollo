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
2. Identify the owning microservice.
3. Read required context.
4. Read the related service context.
5. Read or create the related SDD folder.
6. Complete or refine `spec.md`.
7. Complete or refine `design.md`.
8. Complete or refine `tasks.md`.
9. Implement exactly one task at a time.
10. Update tests.
11. Update `acceptance.md`.
12. Update `docs/04-sdd/traceability-matrix.md`.

## Required output before coding

State explicitly:

- User story
- Owning microservice
- Supporting services
- Files read
- Current SDD status
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
3. Ask for user approval before implementation.

## Required source files for SDD generation

Use actual filenames present in the project:

- `docs/01-project-source/srs.md`
- `docs/01-project-source/historias de usuario.md`
- `docs/01-project-source/modelo de dominio.md`
- `docs/01-project-source/diagrama de clases.md`
- `docs/01-project-source/microservicios.md`
- `docs/01-project-source/enunciado-proyecto.md`
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
