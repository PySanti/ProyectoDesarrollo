# UMBRAL Microservices Map

UMBRAL is implemented using four physical backend microservices.

| Service | Responsibility | Persistence |
|---|---|---|
| Identity Service | Authentication integration, local user references, roles and Keycloak mapping | Own PostgreSQL database/schema for user metadata |
| Team Service | Teams, team members, access codes, leadership, team status and membership rules | Own PostgreSQL database/schema for teams and members |
| Trivia Game Service | Trivia forms, questions, options, trivia games, lobby, answers, scoring, ranking, history and real-time trivia updates | Own PostgreSQL database/schema for trivia data |
| BDT Game Service | BDT games, areas, stages, expected QR textual values, clues, treasure uploads, QR validation, BDT ranking by stages won, history, geolocation and real-time BDT updates | Own PostgreSQL database/schema for BDT data |

## Explicit non-services

The following are not physical microservices in the current architecture:

- Scoring Service
- Audit Service
- Notification Service
- Trivia Service
- Treasure Hunt Service

Scoring, ranking, history and audit-style traces are implemented inside the owning service unless a future ADR changes the service topology.

## BDT ranking clarification

BDT ranking is not calculated from numeric accumulated score.

BDT ranking is calculated from:

1. number of stages won;
2. accumulated time only across won stages as tie-breaker.

Do not model or implement a separate Scoring Service for BDT.

Do not introduce `PuntajeEtapa`, `PuntajeAcumulado` or `PuntajeBDTIncrementado` as active BDT ranking concepts.

## Client topology

| Client | Actors | Main responsibility |
|---|---|---|
| React web | Administrador, Operador | Administration, operation, lobbies, game creation, supervision, rankings, geolocation map and history |
| React Native mobile | Participante, Líder de equipo as participant | Participant gameplay, team membership actions, joining games, convocatorias, Trivia answers, BDT QR upload, clues and geolocation sharing |
| Backend services | Sistema | Authoritative business rules and internal processing |
