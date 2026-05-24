---
name: rabbitmq-events
description: Design and implement RabbitMQ integration events for UMBRAL cross-service asynchronous workflows.
compatibility: opencode
---

# RabbitMQ Events

Based on:

- `docs/00-professor-source/skills/rabbitmq-events-skill.md`
- `docs/03-microservices/events-catalog.md`

Use RabbitMQ for:

- Audit events
- Score recalculation events
- Ranking updates
- Internal notifications
- Cross-service domain events

Rules:

- Events must be versioned.
- Events must be defined under `contracts/events/`.
- Services publish only events they own.
- Consumers must be idempotent where possible.
- Avoid using RabbitMQ for direct user-facing queries.