---
name: sdd-workflow
description: Enforce UMBRAL Spec-Driven Development before planning, coding, reviewing or documenting any feature.
compatibility: opencode
---

# SDD Workflow

Use this skill before any implementation.

## Required process

1. Identify the user story.
2. Identify the owning microservice.
3. Read the related service context.
4. Read the related SDD folder.
5. Ensure `spec.md`, `design.md`, `tasks.md` and `acceptance.md` exist.
6. Implement only one task at a time.
7. Update tests.
8. Update acceptance status.
9. Update traceability matrix.

## Required output before coding

State:

- User story
- Owning microservice
- Files to read
- Task to implement
- Tests to update
- Traceability row to update

## When SDD files contain TODO

If any related SDD file contains TODO, do not start coding.

Instead:

1. Read the requirement sources.
2. Complete the missing SDD content.
3. Ask for user review or approval.
4. Only then proceed to implementation.

## Required source files for SDD generation

When generating SDD files, read:

- `docs/01-project-source/Documento_SRS_Grupo_4.md`
- `docs/01-project-source/Historias_de_usuario_primera_entrega_Grupo_4 (1).md`
- `docs/02-project-context/business-rules.md`
- `docs/02-project-context/first-delivery-scope.md`
- `docs/02-project-context/design/*.md`
- `docs/03-microservices/service-ownership.md`
- The related file under `docs/03-microservices/services/`