# Review Boundaries

Use this command when a feature appears to touch more than one microservice.

## Mandatory validation

1. Use `umbral-context`.
2. Confirm the HU/folder appears in `docs/04-sdd/SPECS-LIST.md`.
3. Read `docs/03-microservices/service-ownership.md`.
4. Read `docs/03-microservices/communication-map.md`.
5. Read related contracts under `contracts/`.

## Process

1. Decide which of the four approved services owns the behavior.
2. Decide which services, if any, are supporting services.
3. Decide whether supporting services are called through HTTP, RabbitMQ, or SignalR.
4. Report required contract changes before coding.
5. Report any boundary violation.

## Valid owning services

- Identity
- Partidas
- Operaciones de Sesion
- Puntuaciones

## Rules

- Do not code.
- Do not allow direct database access across services.
- Do not move another service's business rule into the current service.
- Do not create Team Service, Trivia Game Service, BDT Game Service, Treasure Hunt Service, Audit Service, Scoring Service or Notification Service as physical services (obsolete / superseded).
