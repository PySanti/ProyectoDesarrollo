---
name: efcore-postgres
description: Apply EF Core and PostgreSQL conventions for persistence inside each UMBRAL microservice.
compatibility: opencode
---

# EF Core + PostgreSQL

Base yourself on:

- `docs/02-project-context/design/class-design-by-layer.md`
- `docs/03-microservices/service-ownership.md`
- `services/<service>/service-context.md`

## Rules

- Each microservice owns its own database or schema.
- Do not create one global DbContext.
- Do not access another service's tables directly.
- Repositories are implemented in infrastructure.
- Repository interfaces live in application or domain ports according to the service design.
- DbContext belongs to infrastructure.
- Domain entities should remain persistence-ignorant where possible.
- Use explicit EF configurations.
- Migrations belong to the owning service.
- Avoid foreign keys across service databases.
