---
name: ddd-modeling
description: Apply DDD modeling for UMBRAL entities, aggregates, value objects, domain services, domain events and invariants.
compatibility: opencode
---

# DDD Modeling

Base yourself on:

- `docs/02-project-context/domain-model-summary.md`
- `docs/02-project-context/business-rules.md`
- `docs/02-project-context/design/domain-entities-by-context.md`
- `docs/02-project-context/design/domain-business-rules.md`
- `docs/03-microservices/service-ownership.md`

## Rules

- Keep domain independent from infrastructure.
- Use aggregates to protect invariants.
- Use value objects for meaningful domain concepts.
- Use domain events for important business facts.
- Do not put business rules in controllers, hubs or EF configurations.
- Do not leak EF Core attributes into domain entities unless explicitly accepted.
- Do not reuse the same `Participante` class across contexts; keep context-specific meanings separate.

## Core concepts

- Usuario
- Equipo
- CodigoAcceso
- FormularioTrivia
- Pregunta
- Opcion
- PartidaTrivia
- PartidaBDT
- EtapaBDT
- Pista
- Evidencia / Tesoro QR
- PuntajeAcumulado
- Ranking
- RegistroAuditoria
- EventoHistorial
