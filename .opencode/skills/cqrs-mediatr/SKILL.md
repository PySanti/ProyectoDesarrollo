---
name: cqrs-mediatr
description: Apply CQRS and MediatR conventions for UMBRAL .NET microservices.
compatibility: opencode
---

# CQRS + MediatR

Based on:

- `docs/00-professor-source/skills/cqrs-mediatr-skill.md`

Rules:

- Commands change state.
- Queries never change state.
- Controllers call MediatR.
- Handlers coordinate application flow.
- Domain enforces business invariants.
- Validators validate input and application preconditions.
- Integration events are published after successful state changes.

Every command/query folder should include:

- Request object
- Handler
- Validator when needed
- Response/DTO
- Tests