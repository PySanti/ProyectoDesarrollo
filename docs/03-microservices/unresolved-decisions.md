# Unresolved Decisions

## Resolved microservice decision

The microservice topology is no longer unresolved.

UMBRAL uses four physical backend microservices:

- Identity Service
- Team Service
- Trivia Game Service
- BDT Game Service

This decision is formalized in:

```txt
docs/05-decisions/ADR-0006-four-service-topology.md
```

## Explicit non-services

The following are not active physical backend microservices:

- Audit Service
- Scoring Service
- Trivia Service
- Treasure Hunt Service
- Notification Service

Scoring, ranking, history and audit-style traces are responsibilities inside the owning service of the corresponding business flow.

## Remaining non-microservice decisions

The following issues are not microservice topology issues. They must be resolved in the relevant SDD before implementation.

### Trivia scoring formula

There is a conflict between:

- the SRS formula for Trivia score calculation; and
- the domain/class model description of direct accumulation of assigned points.

This must be resolved before implementing Trivia scoring features, especially HU-29.

### Minimum team size

The sources include team creation by a single participant and also mention team cardinality/invariants. The exact minimum team size must be confirmed before implementing team invariant enforcement.

### SDD regeneration

Some existing SDD folders appear to come from an older mission/session model. They must not be used for implementation until regenerated or reviewed against the current four-service topology.
