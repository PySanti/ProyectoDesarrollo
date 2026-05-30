# UMBRAL Microservices Map

UMBRAL is implemented using four physical backend microservices.

| Service | Responsibility | Persistence |
|---|---|---|
| Identity Service | Authentication integration, local user references, roles and Keycloak mapping | Own PostgreSQL database/schema for user metadata |
| Team Service | Teams, team members, access codes, leadership, team status and membership rules | Own PostgreSQL database/schema for teams and members |
| Trivia Game Service | Trivia/schema for teams and members forms, questions, options, trivia sessions, lobby, answers, scoring, ranking, history and real-time trivia updates | Own PostgreSQL database/schema for trivia data |
| BDT Game Service | BDT sessions, areas, stages, QR expected values, clues, evidence uploads, QR validation, scoring, ranking, history, geolocation and real-time BDT updates | Own PostgreSQL database/schema for BDT data |

## Explicit non-services

The following are not physical microservices in the current architecture:

- Scoring Service
- Audit Service
- Notification Service

Scoring, ranking and history are implemented inside the owning game service unless a future ADR changes the service topology.