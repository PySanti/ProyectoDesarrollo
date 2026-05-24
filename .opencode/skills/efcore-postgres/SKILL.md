---
name: efcore-postgres
description: Apply EF Core and PostgreSQL conventions for persistence inside each UMBRAL microservice.
compatibility: opencode
---

# EF Core + PostgreSQL

Based on:

- `docs/00-professor-source/skills/efcore-postgres-skill.md`

Rules:

- Each microservice owns its own database schema or database.
- Do not access another service's tables directly.
- Repositories are implemented in infrastructure.
- DbContext belongs to infrastructure.
- Domain entities should remain persistence-ignorant as much as possible.
- Migrations belong to the owning service.
- Use explicit configurations for entities.