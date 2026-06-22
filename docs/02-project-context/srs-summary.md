# SRS Summary — UMBRAL

> Status: Current derived documentation. Source: `docs/01-project-source/` and `CLAUDE.md`.

## General functional scope

UMBRAL operates real-time interactive **`Partida`s** built from **one or more `Juego`s in sequential order**, each exactly a **`JuegoTrivia`** or a **`JuegoBDT`**. The system covers: authentication and roles via Keycloak; user management; per-role permission/governance; team management with invitations; partida creation and configuration; publishing to lobby; individual or team inscription; team convocatorias; live execution; Trivia answer validation; BDT QR-treasure validation; score accumulation; native and consolidated ranking in real time; history/audit; operational BDT geolocation; and separation of commands from queries.

These capabilities materialize across four services behind a **mandatory YARP gateway**: **Identity**, **Partidas** (configuration), **Operaciones de Sesion** (runtime + inscriptions/convocatorias), and **Puntuaciones** (scoring/ranking/audit).

## Functional requirements by group

### Identity and access

- Integration with Keycloak; base roles `Administrador`, `Operador`, `Participante` (no new roles ever).
- Initial role assigned at user creation; the admin may later change the role of operators/participants (including promotion to admin) but never an admin's role, propagating the change to Keycloak.
- UMBRAL stores no passwords; it keeps a local reference keyed by the Keycloak identifier.
- A temporary password is generated at creation and emailed asynchronously (RabbitMQ); first-login change is enforced by Keycloak. Changing the email while the credential is still temporary re-issues a new temporary password.
- Deactivated users cannot operate in the system.
- Two authorization levels managed **per role** from the governance panel: governance privileges and functional permissions (`GestionarPartidas`, `GestionarEquipos`, `ParticiparEnPartidas`). Admin governance privileges are protected.
- `Líder de equipo` is a business attribute, not a Keycloak role.

### Game modes

- Only `Trivia` and `Búsqueda del Tesoro` exist; each `Juego` of a `Partida` is exactly one of them. No additional modes.
- `Partida` states: `Lobby`, `Iniciada`, `Cancelada`, `Terminada`. Each `Juego` has its own sub-state `Pendiente` / `Activo` / `Finalizado`.
- A `Partida` has a single `Modalidad` (`Individual` / `Equipo`) fixed once for all its games, a `ModoInicioPartida` (`Manual` / `Automatico` / `ManualYAutomatico`), and min/max participation. Games activate sequentially when the partida starts; when the last game finishes, the partida becomes `Terminada`.

### Teams (inside Identity)

- A participant may create a team only if not already in one; the creator is the first member and leader.
- 1 to 5 members; a participant belongs to only one active team at a time.
- Members join **only via `InvitacionEquipo`** sent by the leader from a dynamic participant list that excludes anyone already in a team and is blocked when the team is full. There is **no team access code**.
- Invitations do not expire; deleting a team deletes its pending invitations but preserves history.
- Leadership is transferable; a leader leaving with no other members deletes the team.
- Per-participant team-name history is preserved. Teams are global and usable in both Trivia and BDT.

### Listing, filters and access (mobile)

- A single `Partidas` panel lists all published partidas regardless of game type, with a filter by modality (`Individual` / `Equipo`).
- A participant may play individual partidas even while belonging to a team.
- Only a leader can inscribe a team in a team partida; a non-leader is shown "Debes ser líder de un equipo para entrar en esta partida".
- Only one active participation at a time (active individual inscription or accepted team convocatoria while the partida is in `Lobby`/`Iniciada`).

### Partidas and Trivia configuration (Partidas)

- The operator creates a `Partida` as a sequence of `Juego`s (Juego 1, Juego 2, …), fixing a single modality, min/max participation, start mode and time.
- For `JuegoTrivia`, the operator creates the `Pregunta`s at game-creation time (options, correct answer, `PuntajeAsignado`, time limit). No question bank, no reuse; a game without at least one complete question cannot be published.
- For `JuegoBDT`, the operator defines the `AreaBusqueda` (descriptive text) and one or more `EtapaBDT`, each with expected QR text, a `Puntaje`, and a time limit; the partida cannot be published without valid stages.

### Live runtime (Operaciones de Sesion)

- Publishing a partida moves it to `Lobby` and enables a single inscription per participant (individual) or per team (team).
- Manual/automatic start, requiring the configured minimums; if the start time is reached without minimums, the partida is cancelled automatically.
- Trivia: synchronized question and timer for all; one answer per participant (individual) or per team (first option from any active member); a question closes on first correct answer or timeout, then advances or finishes.
- BDT: first stage activates with the game; participants upload QR photos; the backend decodes and compares to the expected text; a stage closes on first correct validation or timeout; in team modality any active member's correct upload wins for the team.
- Operator sends clues, sees uploaded treasures, and views the geolocation map; reconnection is supported while the partida is `Iniciada`.

### Scoring, ranking, history (Puntuaciones)

- Trivia native ranking: order by `PuntajeAcumulado` descending (a correct answer adds the question's `PuntajeAsignado` directly; time never modifies points), tie-break by lowest accumulated answer time.
- BDT native ranking: order by accumulated points = sum of the `Puntaje` of won stages; tie-break by lowest accumulated time of the won stages only. Count of stages won is informative, not the sort key.
- Consolidated partida ranking (on finish): by number of games won, then total accumulated points across all games, then lowest total time. A game's winner is whoever has the most points in it (tie-break lowest time; if still tied, no winner).
- History/audit records relevant facts (state changes, inscriptions, convocatorias, invitations, game activation/finish, answers, treasures, validations, clues, locations, score/ranking changes, cancellations, results) and is materialized in Puntuaciones and Operaciones de Sesion.
- Real-time updates cover lobby, states, games, questions, ranking, stages, timers, clues, geolocation, results.

## Non-functional requirements

| ID | Summary |
|---|---|
| RNF-01 | React web, React Native mobile, .NET Core backend. |
| RNF-02 | PostgreSQL + Entity Framework Core. |
| RNF-03 | Real-time over WebSockets. |
| RNF-04 | MediatR and CQRS. |
| RNF-05 | RabbitMQ for async processes. |
| RNF-06 | Hexagonal / clean architecture. |
| RNF-07 | Domain independent of infrastructure and web framework. |
| RNF-08 | Logging, exception handling, validations. |
| RNF-09 | Backend test coverage goal ≥ 90%. |
| RNF-10 | Local execution via Docker Compose. |
| RNF-11 | CI pipeline for build and tests. |
| RNF-12 | Clear, usable, coherent interfaces; admin/operation vs participation flows differentiated. |
| RNF-13 | Keycloak with secure tokens. |
| RNF-14 | Store no passwords or sensitive credentials. |
| RNF-15 | BDT geolocation every 2 seconds without blocking. |
| RNF-16 | Decode QR codes from images captured/uploaded on mobile. |
| RNF-17 | Real-time channel for lobby, questions, ranking, stages, clues, geolocation, states. |
| RNF-18 | Mobile camera/image use for QR treasure upload, with permissions. |
| RNF-19 | Mobile geolocation permission before sharing during active BDT. |
| RNF-20 | Mobile consumes only backend HTTP/real-time contracts; no direct DB, no authoritative rule duplication. |
| RNF-21 | Mandatory YARP gateway as single entry point; validates the Keycloak JWT and routes all traffic, including real-time. |
| RNF-22 | Gateway applies coarse role authorization by route from token claims, without querying Identity per request; fine-grained functional-permission authorization stays in the services. |
| RNF-23 | Async email capability (RabbitMQ) for temporary passwords; no fixed mail provider. |
| RNF-24 | Time-based Keycloak token refresh between client and Keycloak only (not via gateway/backend), with inactivity control. |
