# SDD Workflow for UMBRAL

Every feature must follow this workflow:

1. Select one user story.
2. Identify the owning microservice.
3. Read the service context.
4. Read the business rules.
5. Create or update `spec.md`.
6. Create or update `design.md`.
7. Create or update `tasks.md`.
8. Implement one task at a time.
9. Add or update tests.
10. Update `acceptance.md`.
11. Update `traceability-matrix.md`.

## Required SDD files per feature

Each feature folder must contain:

- `spec.md`
- `design.md`
- `tasks.md`
- `acceptance.md`

## Rule

No implementation should be done if the related SDD files do not exist.
