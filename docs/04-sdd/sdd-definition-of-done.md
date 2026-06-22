# Definition of Done

A feature is done only when:

- The implementation matches `spec.md`.
- The implementation matches `design.md`.
- All completed tasks are checked in `tasks.md`.
- Acceptance criteria are checked in `acceptance.md`.
- Unit tests exist for business rules.
- Application handler tests exist for commands/queries.
- Integration or contract tests exist when services communicate.
- HTTP contracts are updated in `contracts/http/`.
- Event contracts are updated in `contracts/events/`.
- Traceability matrix is updated.
- The feature respects microservice ownership (`Identity`, `Partidas`, `Operaciones de Sesion`, or `Puntuaciones`).
- No business rule is implemented in controllers, hubs, or gateway.
- No service accesses another service database directly.
- Cross-service communication uses RabbitMQ (async) or SignalR/WebSockets (real-time user-facing), not direct calls.
- All client traffic passes through the YARP gateway; no direct client-to-service contact exists.
