---
name: testing
description: Apply UMBRAL testing strategy for unit, integration and E2E tests with acceptance criteria and traceability.
compatibility: opencode
---

# Testing

Based on:

- `docs/00-professor-source/skills/testing-skill.md`
- `docs/00-professor-source/specs/umbral-quality-spec.md`

Rules:

- Every business rule needs at least one test.
- Domain rules should be unit tested.
- Application handlers should be tested.
- Cross-service flows should have integration or contract tests.
- Frontend critical flows should have E2E tests where applicable.
- Completed features must update acceptance evidence.