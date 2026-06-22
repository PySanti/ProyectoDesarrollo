# Feature Template

## User story

As a [actor], I want [goal], so that [benefit].

## Source

- HU:
- Requirement:
- Owning service: <!-- Identity | Partidas | Operaciones de Sesion | Puntuaciones -->
- Supporting services:
- Client target: <!-- web (Administrador/Operador) | mobile (Participante) | backend (Sistema) -->

## Gateway-aware contract questions

- Does the feature use HTTP through the gateway?
- Does it require RabbitMQ events?
- Does it require SignalR/WebSockets through the gateway?
- Which target service owns the command/query?

## Scope

Included:

-

Excluded:

-

## Business rules

-

## API / Events

HTTP endpoints (all routed through YARP gateway):

-

Events published (RabbitMQ):

-

Events consumed (RabbitMQ):

-

SignalR/WebSockets (through gateway):

-

## Acceptance criteria

-

## Tests

- Unit:
- Integration:
- Contract:
- E2E:
