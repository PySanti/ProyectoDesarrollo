# Glossary — UMBRAL

> Status: Current derived documentation. Source: `docs/01-project-source/` and `CLAUDE.md`.

## Actors

| Term | Definition |
|---|---|
| Administrador | Manages users, initial roles, per-role permission/governance, and administrative team management. |
| Operador | Creates, configures, publishes, starts, cancels, and supervises partidas. |
| Participante | Plays individual partidas or as part of a team. |
| Líder de equipo | Participant who created a team or received leadership transfer. Business attribute, not a Keycloak role. May inscribe the team and invite members. |
| Miembro de equipo | Participant who belongs to a team. |
| Sistema | Logical actor for automatic processes (validations, sequential activation, ranking, real-time). |

## Partida and games

| Term | Definition |
|---|---|
| Partida | Aggregate root that is published, joined, and ranked. Contains 1..* `Juego` in sequential order. States `Lobby`/`Iniciada`/`Cancelada`/`Terminada`; one `Modalidad`, one `ModoInicioPartida`, min/max participation. Computes `RankingConsolidado` on finish. |
| Juego | Sequential unit inside a `Partida`, of `TipoJuego` `Trivia` or `BusquedaDelTesoro`, with sub-state `Pendiente`/`Activo`/`Finalizado`. Activates sequentially. |
| JuegoTrivia | Specialization of `Juego` that owns its `Pregunta`s directly (created with the game). |
| JuegoBDT | Specialization of `Juego` based on `EtapaBDT`s and QR codes. |
| TipoJuego | `Trivia` or `BusquedaDelTesoro`. Distinct from `Modalidad`. |
| Modalidad | `Individual` or `Equipo`. Fixed once for the whole partida; applies to all its games. |
| EstadoPartida | `Lobby`, `Iniciada`, `Cancelada`, `Terminada`. |
| EstadoJuego | `Pendiente`, `Activo`, `Finalizado` — internal sub-state of a `Juego`. |
| ModoInicioPartida | `Manual`, `Automatico`, `ManualYAutomatico`. |

## Participation

| Term | Definition |
|---|---|
| InscripcionPartida | Partida-level registration, once per partida (one per participant in `Individual`; one per team in `Equipo`). |
| Convocatoria | Child of `InscripcionPartida`: call to a team's members to participate in a team partida; affects only that partida, never team membership. |
| InvitacionEquipo | Request for a participant to join a team, sent by the leader; accepted/rejected; does not expire. Distinct from `Convocatoria`. Replaces the obsolete team access code. |
| Lobby | State of a published partida that admits inscriptions. |
| Participación activa | Active individual inscription, or accepted team convocatoria while the partida is in `Lobby`/`Iniciada`. Only one at a time. |

## Teams

| Term | Definition |
|---|---|
| Equipo | Global group of 1–5 participants with a leader, usable in Trivia and BDT. Lives inside Identity. |
| ParticipanteEquipo | Team member (with `UsuarioId`, join date, `EsLider`). |
| HistorialEquipoUsuario | Per-participant record of the names of teams they have belonged to. |
| EstadoEquipo | `Activo`, `Desactivado`, `Eliminado`. |
| Transferencia de liderazgo | Action by which the leader designates another member before leaving. |

> Obsolete: the old "código de acceso" (team access code) no longer exists; members join only via `InvitacionEquipo`.

## Trivia

| Term | Definition |
|---|---|
| Pregunta | Element of a `JuegoTrivia`: text, options, correct answer, `PuntajeAsignado`, time limit. Created with the game. |
| Opcion | Possible answer of a `Pregunta`. |
| PuntajeAsignado | Points granted directly when the answer is correct (time never modifies it). |
| TiempoLimite | Time controlling answer availability; not part of score. |
| RespuestaTrivia | Answer sent by a participant/team for an active question. |
| ParticipanteTrivia | Competing unit in a `JuegoTrivia`; maps to a `UsuarioId` (individual) or `EquipoId` (team). |
| RankingTrivia | Native ranking by `PuntajeAcumulado` descending, tie-break by lowest accumulated answer time. |

> Obsolete: there is no `FormularioTrivia`/"Trivia form" anymore; questions belong directly to the `JuegoTrivia`.

## Búsqueda del Tesoro

| Term | Definition |
|---|---|
| BDT | Búsqueda del Tesoro. Stage-based mode with QR validation. |
| EtapaBDT | Stage of a `JuegoBDT` with expected QR text, a `Puntaje`, and a time limit. |
| TesoroQR | QR image submission by a participant/team. |
| CodigoQREsperado | Expected QR text content for the active stage. |
| ResultadoValidacionQR | `Valido`, `Invalido`, `NoLegible`, `NoCorrespondeEtapaActiva`. |
| Pista | Message sent by the operator to participants/teams during BDT. |
| AreaBusqueda | Descriptive text of the search area (no coordinates). |
| UbicacionGeografica | Latitude/longitude/date for a participant during an active BDT game. |
| ParticipanteBDT | Competing unit in a `JuegoBDT`; maps to a `UsuarioId` or `EquipoId`. |
| RankingBDT | Native ranking by accumulated points (sum of `Puntaje` of won stages), tie-break by lowest accumulated time of won stages only. Stages won is informative data. |

## Scoring and ranking

| Term | Definition |
|---|---|
| Ranking nativo | Per-game ranking ordered by points accumulated in that game. |
| Ganador de un juego | Participant/team with the most points in a game (tie-break lowest time; if still tied, no winner). |
| Ranking consolidado | Final partida ranking: by number of games won, then total accumulated points, then lowest total time. Coexists with native rankings. |
| EtapasGanadas | Count of stages a participant/team won in a `JuegoBDT`. Informative data, not a sort key. |

## Identity, access and governance

| Term | Definition |
|---|---|
| Rol | One of `Administrador`, `Operador`, `Participante`. No new roles are created. |
| Privilegio de gobernanza | System-administration capability associated with a role; Administrador's are protected. |
| Permiso funcional | Functional capability associated with a role: `GestionarPartidas`, `GestionarEquipos`, `ParticiparEnPartidas`. |
| EstadoCredencial | `TemporalPendiente` or `Definitiva`. |
| Keycloak | External identity provider for authentication, JWT tokens, and base roles. |
| API Gateway (YARP) | Single mandatory backend entry point; validates the Keycloak JWT and applies route-level role authorization. Owns no domain logic or DB. |

## Architecture

| Term | Definition |
|---|---|
| Clean Architecture | Organization where domain and application do not depend on infrastructure. |
| Arquitectura Hexagonal | Ports-and-adapters architecture. |
| CQRS | Separation of state-mutating commands from read-only queries. |
| MediatR | .NET mediator library. |
| EF Core | ORM used for PostgreSQL persistence. |
| RabbitMQ | Broker for asynchronous cross-service messaging. |
| SignalR | .NET real-time communication over WebSockets (routed through the gateway). |
