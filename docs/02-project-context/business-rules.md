# Business Rules

> Status: Current derived documentation. Source: `docs/01-project-source/` and `CLAUDE.md`.

Normalized from the SRS (`docs/01-project-source/srs.md`). A `Partida` contains one or more `Juego`s in sequential order, each a `JuegoTrivia` or `JuegoBDT`. Lobby, inscription, modality, start mode, lifecycle, and the consolidated ranking are partida-level. Ownership in the target topology: Identity (users/roles/teams), Partidas (configuration), Operaciones de Sesion (runtime + inscriptions/convocatorias), Puntuaciones (scoring/ranking/audit).

## Global rules

| ID | Rule |
|---|---|
| BR-G01 | The system supports only two game types inside a partida: Trivia and Búsqueda del Tesoro. No additional game types. |
| BR-G02 | A `Partida` may only be in `Lobby`, `Iniciada`, `Cancelada`, or `Terminada`; each `Juego` has its own sub-state `Pendiente`/`Activo`/`Finalizado`. Every transition must be validated. |
| BR-G03 | A `Partida` has a single `Modalidad` (`Individual`/`Equipo`) fixed once for all its games, a `ModoInicioPartida`, and min/max participation. |
| BR-G04 | On start, games activate sequentially in their defined order; when the last game finishes, the partida becomes `Terminada`. Cancellation applies to the whole partida (`Lobby` or `Iniciada` only). |
| BR-G05 | The system differentiates behavior by authenticated role and by domain conditions (e.g. team leadership). |
| BR-G06 | Keycloak handles authentication and base roles; UMBRAL stores no passwords, only local references and domain state. |
| BR-G07 | User-visible real-time updates reflect state already accepted by the backend; RabbitMQ events represent facts that already happened. |
| BR-G08 | A service must never read or write another service's database. |
| BR-G09 | A participant or team may have only one active participation at a time (active individual inscription, or accepted team convocatoria while the partida is in `Lobby`/`Iniciada`). |

## Roles, permissions and governance (Identity)

| ID | Rule |
|---|---|
| BR-R01 | Exactly three base roles exist: `Administrador`, `Operador`, `Participante`. No new roles are ever created. |
| BR-R02 | Two authorization levels — governance privileges and functional permissions (`GestionarPartidas`, `GestionarEquipos`, `ParticiparEnPartidas`) — managed per role from the governance panel, never per user. |
| BR-R03 | Defaults: Administrador → governance privileges; Operador → `GestionarPartidas`; Participante → `GestionarEquipos` + `ParticiparEnPartidas`. |
| BR-R04 | The admin may change the role of operators/participants, including promotion to admin, but never an admin's role; the change propagates to Keycloak. Admin governance privileges are protected. |
| BR-R05 | A temporary password is generated at creation and emailed asynchronously (RabbitMQ); changing the email while the credential is still temporary re-issues a new one. UMBRAL stores no passwords. |
| BR-R06 | Team leadership is a UMBRAL business attribute, not a Keycloak role. |

## Team rules (Identity)

| ID | Rule |
|---|---|
| BR-E01 | A participant can belong to at most one active team. |
| BR-E02 | A team can exist with 1 to 5 members; do not enforce a minimum of 2. |
| BR-E03 | The team creator is automatically the first member and leader. |
| BR-E04 | A team must not exceed 5 members. |
| BR-E05 | Members join **only via `InvitacionEquipo`** sent by the leader from a dynamic participant list that excludes anyone already in a team and is blocked when the team is full. There is no team access code. |
| BR-E06 | Invitations do not expire; deleting a team deletes its pending invitations but preserves history. |
| BR-E07 | A non-leader participant can leave the team directly. |
| BR-E08 | If the leader leaves and other members exist, leadership must be transferred first. |
| BR-E09 | If the leader leaves and is the only member, the team is deleted. |
| BR-E10 | A disabled team cannot be registered in new partidas; a team cannot be deleted while inscribed in a `Lobby` partida or participating in an `Iniciada` one. |
| BR-E11 | Per-participant team-name history is preserved across deletions. Teams are global and usable in both Trivia and BDT. |

## Trivia rules (`JuegoTrivia`)

| ID | Rule |
|---|---|
| BR-T01 | Only an operator can create `JuegoTrivia` games and their questions. |
| BR-T02 | A `Pregunta` is created with the game and has options, one correct answer, `PuntajeAsignado`, and a time limit. No question bank, no reuse. |
| BR-T03 | A partida cannot be published if a `JuegoTrivia` lacks at least one complete question. Partida-level data (name, modality, min/max, start mode/time) is set on the partida. |
| BR-T04 | In `Individual`, one answer per participant per active question; in `Equipo`, one answer per team — the first option sent by any active member is definitive. |
| BR-T05 | Repeated, late, or out-of-state answers are rejected; an incorrect answer cannot be retried for the same question. |
| BR-T06 | A question closes for everyone on the first correct answer or on timeout, then shows the correct answer and advances or finishes. |
| BR-T07 | A correct answer adds the question's `PuntajeAsignado` directly. The timer controls availability/closing/late-answer rejection only and never modifies score. |
| BR-T08 | Trivia native ranking is ordered by `PuntajeAcumulado` descending; ties broken by lowest accumulated answer time. |

## BDT rules (`JuegoBDT`)

| ID | Rule |
|---|---|
| BR-B01 | Only an operator can create `JuegoBDT` games. |
| BR-B02 | `AreaBusqueda` is descriptive text (no coordinates/polygons). A game has one or more `EtapaBDT`. |
| BR-B03 | Each `EtapaBDT` has expected QR **text**, a per-stage `Puntaje`, and a time limit; a partida cannot be published with a stage missing any of these. |
| BR-B04 | A QR treasure submission is valid only if its decoded text matches the active stage's expected text. A participant/team may make multiple attempts until correct or until the stage closes. |
| BR-B05 | A stage closes on first correct validation or on timeout; in `Equipo`, any active member's correct upload wins it for the whole team. On close, it advances to the next stage or finishes the game. |
| BR-B06 | The operator may send clues (`Pista`) to specific participants/teams during an active BDT game; clues are recorded. |
| BR-B07 | Geolocation is mandatory for an active BDT game; the participant must authorize location on mobile, which updates every 2 seconds for the operator map. |
| BR-B08 | A won `EtapaBDT` grants its `Puntaje`; stages nobody wins grant nothing. |
| BR-B09 | BDT native ranking is ordered by accumulated points (sum of the `Puntaje` of won stages) descending; ties broken by lowest accumulated time of the won stages only. The count of stages won is informative data, not the sort key. |

## Consolidated ranking (Puntuaciones)

| ID | Rule |
|---|---|
| BR-C01 | On finish, the consolidated partida ranking orders participants/teams by (1) number of games won, then (2) total accumulated points across all games (Trivia points + BDT won-stage points), then (3) lowest total time. |
| BR-C02 | A game's winner is whoever has the most points in it; tie-break by lowest time in that game; if still tied, the game has no winner. |
| BR-C03 | Each game keeps its native ranking; the consolidated ranking does not replace it — both coexist. |
