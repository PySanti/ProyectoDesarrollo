# Domain Business Rules

> Status: Current derived documentation. Source: `docs/01-project-source/` and `CLAUDE.md`.

Rules placed inside the aggregates and domain services of the four target services. The central structure is a **`Partida`** of **sequential `Juego`s**, each a **`JuegoTrivia`** or **`JuegoBDT`**.

## Identity (users, roles, governance, teams)

### IDN-001 — Base roles only

Exactly three base roles exist: `Administrador`, `Operador`, `Participante`. No new roles are ever created.

### IDN-002 — Per-role authorization

Governance privileges and functional permissions (`GestionarPartidas`, `GestionarEquipos`, `ParticiparEnPartidas`) are managed per role, never per user. Administrador governance privileges are protected and cannot be withdrawn.

### IDN-003 — Role modification

The admin may change the role of operators/participants (including promotion to admin) but never an admin's role; the change propagates to Keycloak.

### IDN-004 — No passwords / temporary credential

UMBRAL stores no passwords; it keeps a local Keycloak reference. A temporary password is emailed asynchronously (RabbitMQ) at creation; an email change while the credential is still `TemporalPendiente` re-issues a new one.

### TEAM-001 — Team cardinality

A team has 1 to 5 members. Do not enforce a minimum of 2.

```txt
1 <= Equipo.Participantes.Count <= 5
```

### TEAM-002 — Creator as leader

When a team is created: it is active, the creator is the first member, and the creator is the leader.

### TEAM-003 — Maximum members

Identity must reject attempts to add a sixth member.

### TEAM-004 — One active team per participant

A participant cannot belong to more than one active team.

### TEAM-005 — Join only by invitation

Members join **only via `InvitacionEquipo`** sent by the leader from a dynamic list excluding those already in a team; invitations cannot be created when the team is full and do not expire. There is no team access code.

### TEAM-006 — Leader exit / deletion

A non-leader leaves directly; a leader with other members must transfer leadership first; a leader who is the only member deletes the team. A team cannot be deleted while inscribed in a `Lobby` partida or participating in an `Iniciada` one; deletion removes pending invitations but preserves team-name history.

## Partidas (Partida/Juego configuration, Trivia & BDT content)

### PART-001 — Partida structure

A `Partida` contains 1..* `Juego` in sequential order, each `JuegoTrivia` or `JuegoBDT`. A single `Modalidad` is fixed once for all its games; `ModoInicioPartida` and min/max participation are partida-level.

### PART-002 — Sequential lifecycle

States are `Lobby`/`Iniciada`/`Cancelada`/`Terminada`; each `Juego` has its own `Pendiente`/`Activo`/`Finalizado`. Games activate sequentially; the partida becomes `Terminada` when the last game finishes. Cancellation applies to the whole partida.

### TRIVIA-CONF-001 — Questions created with the game

A `JuegoTrivia` owns its `Pregunta`s, created with the game: options, one correct answer, `PuntajeAsignado`, time limit. No question bank, no reuse. A partida cannot be published if a `JuegoTrivia` lacks at least one complete question.

### BDT-CONF-001 — Stages created with the game

A `JuegoBDT` defines `AreaBusqueda` (descriptive text) and one or more `EtapaBDT`, each with expected QR **text**, a per-stage `Puntaje`, and a time limit. A partida cannot be published with a stage missing any of these.

## Operaciones de Sesion (runtime, inscriptions, convocatorias)

### RUN-001 — Publish and start

Publishing moves the partida to `Lobby` and enables a single inscription per participant (individual) or per team (team). Start (manual/automatic) requires the configured minimums; a time-based start that does not meet them cancels the partida automatically.

### RUN-002 — One active participation

A participant/team has only one active participation at a time (active individual inscription, or accepted team convocatoria while the partida is in `Lobby`/`Iniciada`). A `Convocatoria` affects only that partida, never team membership.

### TRIVIA-ANSWER-001 — One definitive answer

One answer per participant per active question (individual). In team modality the active unit is the team, so the first option submitted by any active member is definitive.

### TRIVIA-ANSWER-002 — Reject invalid answers / close

Repeated, late, or out-of-state answers are rejected; an incorrect answer cannot be retried. A question closes for everyone on first correct answer or timeout, then shows the correct answer and advances or finishes.

### BDT-QR-001 — Expected QR & validation

Each active stage has expected QR text. A treasure submission is valid only when the decoded text matches the active stage's expected text. Multiple attempts are allowed until correct or until the stage closes.

### BDT-STAGE-001 — Stage close & geolocation

A stage closes on first correct validation or timeout; in `Equipo`, any active member's correct upload wins it for the team. On close it advances to the next stage or finishes the game. Geolocation is mandatory for an active BDT game (authorized on mobile, every 2 seconds).

## Puntuaciones (scoring, ranking, audit/history)

### TRIVIA-SCORE-001 — Direct score, no time weighting

A correct answer adds the question's `PuntajeAsignado` directly:

```txt
if respuesta.EsCorrecta:
    participante.PuntajeAcumulado += pregunta.PuntajeAsignado
```

Remaining/elapsed/total/accumulated time must not modify the score; the timer is for availability, closing, late-answer rejection, and client synchronization only.

### TRIVIA-RANKING-001 — Trivia native ranking

Order by `PuntajeAcumulado` descending; tie-break by **lowest accumulated answer time**.

### BDT-SCORE-001 — Won stages grant points

A won `EtapaBDT` grants its `Puntaje`; stages nobody wins grant nothing. BDT points accumulate per `JuegoBDT`.

### BDT-RANKING-001 — BDT native ranking

Order by accumulated points (sum of the `Puntaje` of **won stages**) descending; tie-break by **lowest accumulated time of the won stages only**. The count of stages won (`EtapasGanadas`) is informative data, never the sort key.

### CONSOLIDATED-001 — Consolidated ranking

On finish, order by (1) number of games won, then (2) total accumulated points across all games, then (3) lowest total time. A game's winner has the most points in it (tie-break lowest time; if still tied, no winner). The consolidated ranking coexists with native rankings.

### AUDIT-001 — History materialization

Audit/history is cross-cutting, materialized in Puntuaciones and Operaciones de Sesion (no separate Audit Service). History is preserved even when a partida is cancelled or a team deleted; a cancelled partida keeps partial events but does not count as a final result.

## Cross-context rules

### CROSS-001 — No shared database

No service may read or write another service's database. Cross-service async workflows use RabbitMQ; user-visible real-time updates use SignalR/WebSockets through the gateway.

### CROSS-002 — Gateway is routing only

The gateway validates the JWT and authorizes by base role at the route level; it owns no domain logic. Fine-grained functional-permission checks stay inside each service.
