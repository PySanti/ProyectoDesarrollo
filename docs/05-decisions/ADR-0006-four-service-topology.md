# ADR-0006 - Four Physical Backend Services

> Superseded by `ADR-0008-documentation-doctrine-replacement.md`. This ADR is retained as historical decision context for the previous service topology.

## Status

Accepted

## Context

UMBRAL is implemented as a distributed backend using physical microservices.

The project currently standardizes the backend topology around four valid backend services:

1. Identity Service
2. Team Service
3. Trivia Game Service
4. BDT Game Service

The SRS and domain model include scoring, ranking, history, audit-style event records, asynchronous events and real-time updates. However, these concerns are not implemented as separate physical microservices in the current topology.

## Decision

UMBRAL will use only these four physical backend microservices:

- Identity Service
- Team Service
- Trivia Game Service
- BDT Game Service

The following names must not be used as active physical backend services:

- Audit Service
- Scoring Service
- Trivia Service
- Treasure Hunt Service
- Notification Service

Scoring, ranking and history are owned by the service responsible for the corresponding business flow:

- Trivia scoring, ranking and history belong to Trivia Game Service.
- BDT scoring, ranking and history belong to BDT Game Service.
- Team-related history belongs to Team Service.
- Identity/user-related history belongs to Identity Service.

RabbitMQ may still be used for asynchronous events, but all publishers and consumers must belong to one of the four approved services.

## Consequences

- The backend remains physically separated into microservices.
- The service topology is simpler and aligned with the current project decision.
- No standalone Audit Service or Scoring Service will be created.
- Contracts must be organized by the four approved services.
- Docker Compose must not define databases or containers for Audit Service or Scoring Service.
- Gateway routing, if used, must point only to the four approved backend services.
- Future changes to the service topology require a new ADR.
