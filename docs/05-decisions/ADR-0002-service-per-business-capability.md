# ADR 0002 service per business capability

> Superseded where conflicting by `ADR-0008-documentation-doctrine-replacement.md`.

## Status

Accepted

## Decision

Each microservice maps to a business capability: Identity, Team, Trivia, Treasure Hunt, Scoring and Audit.

## Consequences

- OpenCode must respect this decision.
- SDD documents must reference this decision when relevant.
- Implementations that contradict this decision must be reviewed before proceeding.
