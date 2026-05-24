# Gateway Context

## Responsibility

The gateway coordinates frontend access to backend microservices.

## Routes to

- Identity Service
- Team Service
- Trivia Service
- Treasure Hunt Service
- Scoring Service
- Audit Service

## Rules

- The gateway does not own domain logic.
- The gateway does not own persistence.
- The gateway must not bypass service ownership.
- The gateway may centralize routing, authentication forwarding and API composition.
- The gateway must not access service databases directly.

## Allowed responsibilities

- Route HTTP requests.
- Forward authentication context.
- Aggregate read responses when useful for frontend screens.
- Expose a stable frontend-facing API.

## Forbidden responsibilities

- Validate business rules.
- Calculate scores.
- Change session state by itself.
- Write directly to service databases.