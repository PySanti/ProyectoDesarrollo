# Service Model Impact

> Status: Current derived documentation. Source: `docs/01-project-source/` and `CLAUDE.md`.

The UML/domain model is global, but implementation uses four physical backend microservices behind a mandatory YARP gateway.

## Active physical services

- Identity
- Partidas
- Operaciones de Sesion
- Puntuaciones

## Explicit non-services

> Obsolete (superseded): the following are not physical backend microservices in the target topology — `Team Service`, `Trivia Game Service`, `BDT/Treasure Hunt Service`, `Audit Service`, `Scoring Service` (separate from Puntuaciones), `Notification Service`. Teams live inside Identity; Trivia/BDT split across Partidas/Operaciones de Sesion/Puntuaciones; audit/history is cross-cutting.

## Resolved domain decisions

### Team cardinality

Owned by Identity.

```txt
1 <= members <= 5
```

A team can validly exist with one member; the creator is the first member and leader. Members join only via `InvitacionEquipo` (no access code).

### Trivia scoring

Owned by Puntuaciones (fed by Operaciones de Sesion runtime events).

```txt
scoreEarned = question.PuntajeAsignado
```

Time does not modify score. Trivia native ranking: `PuntajeAcumulado` desc, tie-break lowest accumulated answer time.

### BDT ranking

Owned by Puntuaciones. Order by accumulated points = sum of the `Puntaje` of won stages; tie-break by lowest accumulated time of won stages only. `EtapasGanadas` is informative, not the sort key.

## Rules

- Do not implement all UML classes in a single backend.
- Do not create one global database or one global DbContext.
- A service must never read or write another service's database.
- Use HTTP only for direct service queries justified by an SDD.
- Use RabbitMQ for asynchronous cross-service facts/events.
- Use SignalR/WebSockets for user-visible real-time updates, routed through the gateway.

## Impact by service

### Identity

Implements: local user references and Keycloak mapping; roles, permissions, governance per role; role modification propagated to Keycloak; temporary-credential state and async email; teams, membership, leadership/transfer, `InvitacionEquipo`, team-name history.

Does not implement: partida configuration, live session, scoring/ranking.

### Partidas

Implements: `Partida` and sequential `Juego` configuration (order, modality, min/max, start mode/time); Trivia `Pregunta`s (created with the game; no bank, no reuse); BDT `EtapaBDT`s (expected QR text, per-stage `Puntaje`, time limit), `AreaBusqueda`.

Does not implement: running the live session, computing scores/ranking, inscriptions/convocatorias.

### Operaciones de Sesion

Implements: publish→Lobby; manual/automatic start; question/stage synchronization; answer & QR validation; sequential advance of games and stages; clue delivery; geolocation (mandatory in active BDT, every 2s); reconnection; real-time session communication; inscriptions and convocatorias (partida-level). Stores only transient state; emits RabbitMQ events.

Does not implement: configuration (Partidas) or scoring/ranking (Puntuaciones).

### Puntuaciones

Implements: scores and won stages; native Trivia and BDT rankings (during and at end); consolidated partida ranking; team-performance queries; audit/history materialization. Read/projection model fed by RabbitMQ; broadcasts via SignalR.

Does not implement: configuration or runtime.

## Gateway

The YARP gateway is mandatory and the single entry point for all client↔backend traffic, including real-time. It validates the Keycloak JWT and applies route-level role authorization from token claims. It does not own domain logic, scores, rankings, or DB access; fine-grained functional-permission authorization stays inside each service.
