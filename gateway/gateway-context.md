# Gateway Context

## Purpose

The gateway is mandatory. It is the YARP entry-point/routing component for all web and mobile client traffic, including SignalR/WebSocket traffic. It does not own domain logic and must not implement business rules.

## Active backend services

The gateway may route requests only to these backend services:

- Identity
- Partidas
- Operaciones de Sesion
- Puntuaciones

The previous `Team Service`, `Trivia Game Service` and `BDT Game Service` names are legacy implementation boundaries, not active gateway targets.

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
- validate the Keycloak JWT;
- apply coarse route-level authorization by base role (`Administrador`, `Operador`, `Participante`) without querying Identity on every request;
- centralize cross-cutting concerns such as request correlation, logging or CORS configuration;
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
| `/api/identity/*` | Identity |
| `/api/partidas/*` | Partidas |
| `/api/operaciones-sesion/*` | Operaciones de Sesion |
| `/api/puntuaciones/*` | Puntuaciones |
| `/hubs/*` | Gateway-routed SignalR/WebSockets |

Final endpoint paths must be confirmed in the related SDD and `contracts/http/*.md` before implementation.
