# Known Ambiguities and Decisions

> Status: Current derived documentation. Source: `docs/01-project-source/` and `CLAUDE.md`.

This file records the decisions adopted under the current target doctrine and the remaining open questions.

## Source priority

When sources conflict, use this priority:

1. `CLAUDE.md` for operational target doctrine and repository rules.
2. `docs/01-project-source/srs.md`.
3. `docs/01-project-source/modelo-de-dominio.md`.
4. `docs/01-project-source/diagrama-de-clases.md`.
5. `docs/01-project-source/microservicios.md`.
6. Derived docs under `docs/02-project-context/`.
7. Accepted ADRs under `docs/05-decisions/` refine the above when present.

## Resolved decisions

### 1. Target service topology

**Status:** Resolved.

The backend is exactly **four** physical microservices behind a **mandatory YARP gateway**:

- Identity
- Partidas
- Operaciones de Sesion
- Puntuaciones

The following are **not** physical services: Team Service, Trivia Game Service, BDT/Treasure Hunt Service, Audit Service, Scoring Service (separate from Puntuaciones), Notification Service. The earlier four-way split (Identity / Team / Trivia Game / BDT Game) is **obsolete (superseded)** and must not be reintroduced.

### 2. Mandatory gateway

**Status:** Resolved.

YARP is the single entry point for all client↔backend traffic, including real-time (WebSockets/SignalR). It validates the Keycloak JWT and applies coarse, route-level authorization by base role from token claims, without querying Identity per request. Fine-grained functional-permission authorization stays inside the services. The gateway owns no domain logic, scores, rankings, or DB access.

### 3. Teams inside Identity

**Status:** Resolved.

Teams, membership, leadership and transfer, `InvitacionEquipo`, and per-participant team-name history live **inside Identity** (the former Team Service is absorbed entirely). Members join **only via `InvitacionEquipo`**; the old team access code is **obsolete (superseded)**. A team has 1–5 members; the creator is the first member and leader; a user belongs to one active team at a time.

### 4. BDT point-based ranking

**Status:** Resolved (reverses the previous stages-won rule).

BDT native ranking orders by **accumulated points** = sum of the `Puntaje` of **won stages**, descending; tie-break by lowest accumulated time of the **won stages only**. The count of stages won (`EtapasGanadas`) is **informative data only**, never the sort key. Any prior "BDT ranks by number of stages won" statement is **obsolete (superseded)**. See `bdt-ranking-clarification.md`.

### 5. Legacy SDD archive policy

**Status:** Resolved.

The legacy first-sprint SDDs were archived as historical implementation evidence under `docs/04-sdd/_legacy-implementation-evidence/` before new specs are planned. Old documentation describing `Team Service`, `Trivia Game Service`, `BDT Game Service`, or BDT ranking by stages won may remain only when clearly marked as historical evidence; it is not active guidance.

### 6. Trivia scoring (time does not affect score)

**Status:** Resolved.

A correct Trivia answer adds the question's `PuntajeAsignado` directly to the participant's accumulated score:

```txt
scoreEarned = question.PuntajeAsignado
participant.PuntajeAcumulado += scoreEarned
```

Remaining/elapsed/total/accumulated time must not multiply or modify the score. The timer controls availability, closing, and late-answer rejection only. Trivia native ranking is ordered by `PuntajeAcumulado` descending, tie-break by **lowest accumulated answer time**.

### 7. Consolidated ranking

**Status:** Resolved.

On finish, the consolidated partida ranking orders by (1) number of games won, then (2) total accumulated points across all games, then (3) lowest total time. A game's winner has the most points in it (tie-break lowest time; if still tied, no winner). The consolidated ranking coexists with each game's native ranking.

## Remaining open questions

No microservice-topology, gateway, teams, ranking, or Trivia-scoring ambiguity remains open under the current doctrine. Future SDDs may define local details such as endpoint paths, event payload shapes, UI wording, and exact validation/error response shapes.
