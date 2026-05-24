# Service Model Impact

The UML model is global, but the implementation uses physical microservices.

## Rules

- Do not implement all UML classes in a single backend.
- Do not create one global database.
- Do not create one global DbContext.
- Do not directly access another service's database.
- Use HTTP for direct service queries when needed.
- Use RabbitMQ for asynchronous cross-service events.
- Use SignalR/WebSockets for user-visible real-time updates.

## Impact by service

### Identity Service

Implements users, roles, permissions and Keycloak mapping.

### Team Service

Implements teams, team members and active/inactive validation.

### Trivia Service

Implements quizzes, questions, options, trivia sessions and answers.

### Treasure Hunt Service

Implements missions, stages, clues, evidence and treasure hunt sessions.

### Scoring Service

Implements scores, score movements and rankings.

### Audit Service

Implements immutable session event history and audit logs.

### Gateway

Does not own domain logic. It only routes, composes or forwards requests.