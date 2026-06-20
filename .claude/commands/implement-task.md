# Implement Task

Use this command to implement exactly one pending task from a feature `tasks.md`.

## Required input

- Feature folder under `docs/04-sdd/specs/`
- Task number or task title

## Mandatory validation before coding

1. Use `umbral-context`.
2. Use `sdd-workflow`.
3. Open `docs/04-sdd/SPECS-LIST.md`.
4. Confirm the feature folder appears in the active specs list.
5. Confirm the feature folder is not inside `_deprecated`.
6. Confirm `spec.md`, `design.md`, `tasks.md`, and `acceptance.md` exist.
7. Confirm none of those files contains unresolved TODO.
8. Confirm the owning service is one of:
   - Identity Service
   - Team Service
   - Trivia Game Service
   - BDT Game Service.
9. Confirm required contracts exist before implementing endpoints/events.
10. If any validation fails, stop and report the blocker.

## Process

1. Read the selected task.
2. Implement only the selected task.
3. Modify only the owning service unless the SDD explicitly requires a supporting service change.
4. Add or update tests required by the task.
5. Update `acceptance.md` only for acceptance criteria affected by the task.
6. Update `docs/04-sdd/traceability-matrix.md` if the task changes feature status.

## Rules

- Do not implement multiple tasks.
- Do not modify unrelated microservices.
- Do not invent endpoints or events.
- Do not move business rules into controllers, hubs, or EF configurations.
- Do not access another service database.
- Stop if service ownership or contract responsibility is unclear.
