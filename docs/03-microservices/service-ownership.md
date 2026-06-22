# Service Ownership

This file defines what each current target service owns and explicitly does not own, derived from `CLAUDE.md` and `docs/01-project-source/microservicios.md`.

## Current target services

- Identity
- Partidas
- Operaciones de Sesion
- Puntuaciones

Behind the mandatory YARP gateway.

## Obsolete physical services

The following are **obsolete** and must not be reintroduced as active physical services:

- `Team Service` — its responsibilities are absorbed entirely by **Identity**.
- `Trivia Game Service` — its configuration moves to **Partidas** and its runtime to **Operaciones de Sesion**.
- `BDT Game Service` (a.k.a. Treasure Hunt Service) — its configuration moves to **Partidas** and its runtime to **Operaciones de Sesion**.

The following must also never be created as separate physical services:

- `Scoring Service` — scoring/ranking is **Puntuaciones**.
- `Audit Service` — audit/history is a cross-cutting capability materialized in **Puntuaciones** and **Operaciones de Sesion**.
- `Notification Service` — async email notification lives inside **Identity**.

## Identity

Owns:

- users, local user references and Keycloak mapping;
- roles, functional permissions and governance privileges **per role** (never per user);
- role modification for operators/participants — including promotion to admin — propagated to Keycloak (the Administrador role and its governance privileges are protected);
- temporary-credential state (no passwords stored);
- teams, team membership, leadership and transfer;
- team invitations (`InvitacionEquipo`) and per-participant team-name history;
- async email notification of the temporary password over RabbitMQ (welcome on creation; re-issue on email change while the credential is still temporary).

Does not own:

- partida or game configuration;
- live runtime, answer/QR validation, clues, geolocation;
- scoring or ranking.

DB: `umbral_identity`.

## Partidas

Owns:

- creation and configuration of a `Partida` and its `Juego`s, including sequential order, modality, min/max participation, start mode and time;
- Trivia `Pregunta` content — options, correct answer, `PuntajeAsignado` and time limit — created **with** the game (no question bank, no reuse);
- BDT `EtapaBDT` content — expected QR **text**, per-stage `Puntaje` and time limit.

Does not own:

- running the live session;
- scoring or ranking;
- identity, teams or governance;
- inscriptions and convocatorias.

DB: `umbral_partidas`.

## Operaciones de Sesion

Owns:

- the live experience: publishing a partida (→ `Lobby`), manual/automatic start, question/stage synchronization, answer and QR validation, sequential advance of games and stages, clue delivery, geolocation, reconnection, real-time session communication;
- inscriptions (`InscripcionPartida`) and team convocatorias (`Convocatoria`) — partida-level;
- only **transient** session state; emits domain events via RabbitMQ;
- materialization of its own audit/history.

Does not own:

- partida/game configuration (Partidas);
- scoring or ranking (Puntuaciones);
- identity, teams or governance.

DB: `umbral_operaciones_sesion`.

## Puntuaciones

Owns:

- scores and won stages;
- each game's native ranking during and at end of play;
- the consolidated partida ranking;
- team-performance queries;
- materialization of audit/history.

It is a **read/projection model** fed by RabbitMQ domain events, broadcasting updates via SignalR.

Does not own:

- partida/game configuration;
- the live runtime;
- identity, teams or governance.

DB: `umbral_puntuaciones`.

## Gateway (YARP)

Owns:

- routing of all client ↔ backend traffic, including real-time (WebSockets/SignalR);
- Keycloak JWT validation;
- coarse, route-level authorization by base role (`Administrador`/`Operador`/`Participante`) using token claims, without querying Identity per request;
- extensibility to edge concerns (rate limiting, load balancing, TLS termination).

Does not own:

- any domain logic, scores, rankings or database;
- fine-grained functional-permission authorization, which stays inside each service.

## Ranking ownership note

Ranking computation is owned by **Puntuaciones** at both levels:

- **Trivia native**: `PuntajeAcumulado` descending; tie-break lowest accumulated answer time.
- **BDT native**: accumulated points = sum of `Puntaje` of won stages; tie-break lowest accumulated time of won stages only; stages-won count is informative only. The old "rank by number of stages won" rule is obsolete.
- **Consolidated** (on finish): games won, then total points, then lowest total time.

Partidas configures the points (`PuntajeAsignado` for Trivia questions, `Puntaje` for BDT stages); Operaciones de Sesion emits the runtime events that carry them; Puntuaciones projects scores and rankings.
