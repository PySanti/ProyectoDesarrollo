# Definition of Done

A feature is done only when:

- The implementation matches `spec.md`.
- The implementation matches `design.md`.
- All completed tasks are checked in `tasks.md`.
- Acceptance criteria are checked in `acceptance.md`.
- Unit tests exist for business rules.
- Application handler tests exist for commands/queries.
- Integration or contract tests exist when services communicate.
- HTTP contracts are updated.
- Event contracts are updated.
- Traceability matrix is updated.
- The feature respects microservice ownership.
- No business rule is implemented in controllers, hubs or gateway.
- No service accesses another service database directly.