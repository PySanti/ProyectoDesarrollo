# SRS Summary

## Mandatory stack

- Frontend: React
- Backend: .NET Core
- Persistence: PostgreSQL
- ORM: Entity Framework Core
- Application flow: CQRS + MediatR
- Real-time communication: WebSockets / SignalR
- Async messaging: RabbitMQ
- Local execution: Docker Compose
- Architecture: Clean Architecture / Hexagonal Architecture

## Supported roles

- Administrador
- Operador
- Participante

## Supported game modes

- Trivia
- Búsqueda del Tesoro

No other game modes are allowed.

## Quality requirements

- Logging.
- Exception handling.
- Business validations.
- Unit tests.
- Integration tests.
- E2E tests where applicable.
- Academic target of 90% backend coverage.
