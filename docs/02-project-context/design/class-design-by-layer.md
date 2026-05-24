# Class Design by Layer

## Domain layer

Contains:

- Entities
- Aggregates
- Value objects
- Domain services
- Domain events
- Repository interfaces when treated as domain ports

Must not depend on:

- ASP.NET
- EF Core
- RabbitMQ
- SignalR
- PostgreSQL

## Application layer

Contains:

- Commands
- Queries
- Handlers
- DTOs
- Read models
- Validators
- Application ports

Uses:

- CQRS
- MediatR

## Infrastructure layer

Contains:

- EF Core DbContext
- Entity configurations
- Repository implementations
- RabbitMQ publishers and consumers
- SignalR notification adapters
- External service clients

## API layer

Contains:

- Controllers or minimal endpoints
- Authentication and authorization
- Request/response mapping
- Dependency injection
- Middleware

## Microservice rule

Do not create one global DbContext.

Use one persistence model per service:

- IdentityDbContext
- TeamDbContext
- TriviaDbContext
- TreasureHuntDbContext
- ScoringDbContext
- AuditDbContext