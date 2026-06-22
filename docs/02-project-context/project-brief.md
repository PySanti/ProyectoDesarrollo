# Project Brief — UMBRAL

> Status: Current derived documentation. Source: `docs/01-project-source/` and `CLAUDE.md`.

## Project identity

**UMBRAL** is a platform for operating real-time interactive experiences under exactly two game modes:

1. **Trivia**
2. **Búsqueda del Tesoro** (BDT)

No additional game modes, generic workflows, missions, or immersive dynamics may be created, modeled, or implemented.

## Core organizing concept: Partida and Juego

A **`Partida`** is the unit that is published, joined, and ranked. It contains **one or more `Juego`** played in **sequential order**, and each `Juego` is exactly one of two specializations: **`JuegoTrivia`** or **`JuegoBDT`** (by `TipoJuego`).

- Lobby, inscription, modality, start mode, lifecycle, and the consolidated ranking are **partida-level**.
- Each `Juego` has its own internal sub-state (`Pendiente` / `Activo` / `Finalizado`) and activates sequentially when the partida starts.

## Objective

Centralize and control real-time interactive partidas, supporting: creation of Trivia/BDT partidas with one or more sequential games; team management; individual or team inscription; lobby operation; synchronized execution; validation of Trivia answers or BDT QR treasures; score accumulation; native and consolidated ranking; operational geolocation in BDT; history; real-time communication; and asynchronous event publication.

## Actors

| Actor | Responsibility |
|---|---|
| Administrador | Manages users, initial roles, general data, user deactivation, per-role permission/governance, and administrative team management (web client). |
| Operador | Creates and configures partidas and their sequential games (Trivia questions, BDT stages), publishes, starts, cancels, and supervises live operation (web client). |
| Participante | Plays individual partidas, creates or joins teams via invitation, answers Trivia, uploads BDT QR, receives clues, shares geolocation (mobile client). |
| Líder de equipo | Business attribute of a participant (team creator or leadership transferee), not a Keycloak role. May inscribe the team in team partidas and invite members. |
| Sistema | Runs automatic validations, sequential game activation, event publication, ranking updates, and real-time communication (backend). |

## Game modes

### Trivia (`JuegoTrivia`)

Synchronous mode. A `JuegoTrivia` owns its `Pregunta`s (created with the game — no question bank, no reuse), each with options, a correct answer, `PuntajeAsignado`, and time limit. All participants see the same question with a synchronized timer; a question closes for everyone on the first correct answer or on timeout. In `Equipo` modality the valid answer is the first option sent by any active member.

### Búsqueda del Tesoro (`JuegoBDT`)

Stage-based mode. A `JuegoBDT` owns its `EtapaBDT`s, each with expected QR **text**, a per-stage `Puntaje`, and a time limit. `AreaBusqueda` is descriptive text. The participant uploads a QR photo; the backend decodes it and compares it to the expected text. A stage closes on first correct validation or timeout; in `Equipo` any active member's correct upload wins it for the whole team. **Geolocation is mandatory** for an active BDT game (location ~every 2 seconds).

## Target architecture

Four independent .NET 8 microservices (Clean Architecture + CQRS via MediatR) behind a **mandatory YARP gateway**:

- **Identity** — users/Keycloak references, roles+permissions+governance per role, temporary credentials and async email; teams, membership, leadership/transfer, `InvitacionEquipo`, team-name history.
- **Partidas** — `Partida`+`Juego` configuration (sequential order, modality, min/max participation, start mode/time); Trivia `Pregunta`s and BDT `EtapaBDT`s.
- **Operaciones de Sesion** — live runtime (publish→Lobby, manual/automatic start, question/stage sync, answer & QR validation, sequential advance, clue delivery, geolocation, reconnection, real-time comms) plus inscriptions and convocatorias.
- **Puntuaciones** — scores, won stages, native rankings (during and at end), consolidated ranking, team-performance queries, audit/history materialization (read/projection model fed by RabbitMQ, broadcasts via SignalR).

Each service has its own PostgreSQL database (`umbral_identity`, `umbral_partidas`, `umbral_operaciones_sesion`, `umbral_puntuaciones`); a service never touches another's DB.

## Mandatory technology

- Web client: React + Vite + TypeScript (Administrador / Operador).
- Mobile client: React Native + Expo (Participante).
- Backend: .NET 8.
- Persistence: PostgreSQL + Entity Framework Core.
- Use cases: CQRS + MediatR.
- Real-time: WebSockets / SignalR (through the gateway).
- Async messaging: RabbitMQ.
- Authentication and base authorization: Keycloak (realm `UMBRAL-UCAB`).
- Local execution: Docker Compose.
- Architecture: Hexagonal / Clean Architecture.
- Tests: unit, integration, contract and E2E where applicable.

## Mandatory principles

- The domain does not depend on infrastructure.
- Controllers contain no business rules.
- Commands mutate state; queries do not.
- Business rules live in aggregates, domain services, or application handlers as appropriate.
- Cross-service async workflows use RabbitMQ; user-visible real-time updates use SignalR/WebSockets through the gateway.
- The gateway validates the Keycloak JWT and authorizes by base role at the route level; fine-grained functional-permission authorization stays inside the services.
- A service must never read or write another service's database.
- The backend is authoritative for business rules; clients validate for usability only.
