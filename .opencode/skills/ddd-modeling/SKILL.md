---
name: ddd-modeling
description: Apply DDD modeling for UMBRAL entities, aggregates, value objects, domain services and business rules.
compatibility: opencode
---

# DDD Modeling

Based on:

- `docs/00-professor-source/skills/ddd-modeling-skill.md`
- `docs/02-project-context/domain-model-summary.md`
- `docs/02-project-context/business-rules.md`

Rules:

- Keep domain independent from infrastructure.
- Use aggregates to protect invariants.
- Use value objects for meaningful domain concepts.
- Use domain events for important business occurrences.
- Do not put business rules in controllers.
- Do not leak EF Core attributes into domain entities unless explicitly accepted.

Important UMBRAL concepts:

- Mission
- Quiz
- Team
- Session
- Evidence
- Score
- Ranking
- SessionEvent