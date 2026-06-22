# SDD Workflow for UMBRAL

Every feature must follow this workflow:

1. Select one user story.
2. Identify the owning microservice from the four target services: `Identity`, `Partidas`, `Operaciones de Sesion`, or `Puntuaciones`.
3. Answer the four gateway-aware contract questions:
   - Does the feature use HTTP through the gateway?
   - Does it require RabbitMQ events?
   - Does it require SignalR/WebSockets through the gateway?
   - Which target service owns the command/query?
4. Read the service context under `docs/03-microservices/`.
5. Read the business rules under `docs/02-project-context/`.
6. Create or update `spec.md`.
7. Create or update `design.md`.
8. Create or update `tasks.md`.
9. Implement one task at a time.
10. Add or update tests.
11. Update `acceptance.md`.
12. Update `traceability-matrix.md`.

## Required SDD files per feature

Each feature folder must contain:

- `spec.md`
- `design.md`
- `tasks.md`
- `acceptance.md`

## Client routing

- Web (`frontend/`): `Administrador` and `Operador` stories only.
- Mobile (`mobile/`): `Participante` (incl. `Líder de equipo` acting as participant) stories only.
- Backend: `Sistema` stories.

Do not implement participant gameplay in web, or admin/operator screens in mobile, unless an SDD explicitly says otherwise.

## Gateway rule

All client traffic passes through the mandatory YARP gateway. No feature may establish direct client-to-service contact. Real-time updates (SignalR/WebSockets) also route through the gateway.

## Rule

No implementation should be done if the related SDD files do not exist.
