# Class Design Summary

This file summarizes the approved class design.

## Layers expected in backend services

Each microservice should follow Clean Architecture / Hexagonal Architecture:

- Domain
- Application
- Infrastructure
- API

## Application style

Each service should use CQRS + MediatR:

- Commands for write use cases.
- Queries for read use cases.
- Handlers for application flow.
- Validators for input and precondition validation.
- Repositories as ports.
- EF Core implementations inside Infrastructure.

## Source

Place the original class diagram file under:

- `docs/01-project-source/diagrama-clases.puml`
