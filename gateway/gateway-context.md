# Gateway Context

## Purpose

The gateway, if included in the implementation, is an entry-point/routing component. It does not own domain logic and must not implement business rules.

## Active backend services

The gateway may route requests only to these backend services:

- Identity Service
- Team Service
- Trivia Game Service
- BDT Game Service

## Explicit non-services

The gateway must not route to or reference these as active backend services:

- Audit Service
- Scoring Service
- Trivia Service
- Treasure Hunt Service
- Notification Service

## Routing responsibility

The gateway may:

- forward HTTP requests to the owning service;
- expose a unified frontend-facing base URL;
- centralize cross-cutting concerns such as authentication forwarding, request correlation, logging or CORS configuration;
- route WebSocket/SignalR connections when the approved design requires it.

The gateway must not:

- validate domain rules;
- calculate scores;
- decide rankings;
- mutate another service's data directly;
- access service databases;
- create or consume domain events on behalf of a service unless explicitly defined by SDD.

## Suggested route ownership

| Path family | Owning service |
|---|---|
| `/api/identity/*` | Identity Service |
| `/api/users/*` | Identity Service |
| `/api/teams/*` | Team Service |
| `/api/trivia/*` | Trivia Game Service |
| `/api/bdt/*` | BDT Game Service |

Final endpoint paths must be confirmed in the related SDD and `contracts/http/*.md` before implementation.
