# Domain Model Summary — UMBRAL

> Status: Current derived documentation. Source: `docs/01-project-source/` and `CLAUDE.md`.

Derived from `docs/01-project-source/modelo-de-dominio.md`. The model is organized around a **`Partida`** that contains **one or more `Juego`s in sequential order**, each a **`JuegoTrivia`** or **`JuegoBDT`**. Logical bounded contexts materialize onto four target services: **Identity** (Identidad + Equipos), **Partidas** (Partidas + Trivia/BDT configuration), **Operaciones de Sesion** (Trivia/BDT runtime + Participación), **Puntuaciones** (scoring/ranking + Auditoría/Historial).

## Actors

- Administrador, Operador, Participante (base roles).
- Líder de equipo (business attribute, not a Keycloak role).
- Miembro de equipo.
- Sistema (automatic processes).

## Subdomains

| Type | Subdomain | Responsibility |
|---|---|---|
| Core | Partidas y Juegos | Creation/structure of `Partida` and its sequential `Juego`s, lifecycle/states, sequential activation, consolidated ranking. |
| Core | Trivia | `Pregunta`s (created with the game), synchronization, single answers, question close, direct score, native ranking inside `JuegoTrivia`. |
| Core | Búsqueda del Tesoro | `EtapaBDT`, expected QR, per-stage `Puntaje`, treasure upload, QR validation, geolocation, clues, stage close, BDT native ranking inside `JuegoBDT`. |
| Support | Gestión de Equipos | Team creation, membership, leadership, `InvitacionEquipo`, team-name history. |
| Support | Inscripciones y Convocatorias | Partida-level inscription and convocatoria, transversal to a partida's games. |
| Support | Auditoría e Historial | Recording relevant events and operational traceability (cross-cutting). |
| Generic | Identidad, Acceso y Gobernanza | Keycloak integration, base roles, local user references, per-role permission/governance, role modification. |

## Bounded contexts and target services

| Logical context (or portion) | Target service |
|---|---|
| Identidad | Identity |
| Equipos (incl. InvitacionEquipo and team history) | Identity |
| Partidas (Partida/Juego structure and configuration) | Partidas |
| Trivia / BDT — configuration (questions, stages, QR, puntaje) | Partidas |
| Trivia / BDT — runtime (live session) | Operaciones de Sesion |
| Participación (inscription + convocatoria) | Operaciones de Sesion |
| Score, native rankings, and consolidated ranking | Puntuaciones |
| Auditoría (cross-cutting) | Materialized in Puntuaciones and Operaciones de Sesion |

> Note on `Participante`: the logical name appears in several contexts but is not the same class — in Equipos it is `ParticipanteEquipo`; in Trivia it is `ParticipanteTrivia`; in BDT it is `ParticipanteBDT` (competing units that map to a `UsuarioId` or `EquipoId`). Do not share one physical class across contexts.

## Main aggregates

### Identity (service: Identity)

| Aggregate | Entities / VO / Enums | Invariants |
|---|---|---|
| Usuario | UsuarioId, KeycloakId, Correo; RolUsuario, EstadoUsuario, EstadoCredencial | No password stored; role set at creation, modifiable later (operators/participants, incl. promotion to admin) except an admin's, propagated to Keycloak; deactivated user cannot act. Credential born `TemporalPendiente`; email-change while temporary re-issues; becomes `Definitiva` on user change. |
| Rol | RolId, NombreRol; PrivilegiosGobernanza, PermisosFuncionales | Only Administrador/Operador/Participante; no new roles; permissions/privileges managed per role; Administrador governance privileges protected. Defaults: Admin→governance; Operador→`GestionarPartidas`; Participante→`GestionarEquipos`+`ParticiparEnPartidas`. |

### Equipos (service: Identity)

| Aggregate | Entities / VO / Enums | Invariants |
|---|---|---|
| Equipo | ParticipanteEquipo; EquipoId, NombreEquipo; EstadoEquipo | 1–5 members; creator is first member and leader; one active team per user. Members join only via `InvitacionEquipo` (no access code). Cannot delete while inscribed in a `Lobby`/`Iniciada` partida; deletion preserves history. |
| InvitacionEquipo | InvitacionEquipoId; EstadoInvitacion (Pendiente/Aceptada/Rechazada) | Only the leader invites, from a dynamic list excluding those already in a team; cannot invite when full (5); does not expire; deleting the team deletes pending invitations. Independent of `Convocatoria`. |
| HistorialEquipoUsuario | UsuarioId, NombreEquipo | Stores only the names of teams a participant has belonged to; not erased by team deletion. |

### Participación (service: Operaciones de Sesion)

| Aggregate | Entities / VO / Enums | Invariants |
|---|---|---|
| InscripcionPartida | Convocatoria; InscripcionId, ConvocatoriaId, PartidaId, UsuarioId, EquipoId; EstadoInscripcion, EstadoConvocatoria, Modalidad | Partida-level, once per partida (per participant in `Individual`; per team in `Equipo`). Team preinscription confirms on start only if it meets the operator's minimum of accepted members. Only one active participation at a time. `Convocatoria` affects only that partida, never team membership. |

### Partidas (service: Partidas)

| Aggregate | Entities / VO / Enums | Invariants |
|---|---|---|
| Partida | Juego (1..*); PartidaId, NombrePartida, TiempoInicio, MinimosParticipacion, MaximosParticipacion, RankingConsolidado; EstadoPartida, Modalidad, ModoInicioPartida, TipoJuego, EstadoJuego | Contains 1..* `Juego` in sequential order; single lobby phase; one `Modalidad` for all games; start requires meeting minimums (else automatic cancellation for time-based start). Computes `RankingConsolidado` on finish. |
| Juego (base) | JuegoId; Orden, TipoJuego, EstadoJuego, PartidaId | Belongs to one partida with a unique `Orden`; sub-state independent of partida state; specialized as `JuegoTrivia` or `JuegoBDT`. Games activate sequentially. |

### Trivia (config in Partidas, runtime in Operaciones de Sesion, scoring in Puntuaciones)

| Aggregate | Entities / VO | Invariants |
|---|---|---|
| JuegoTrivia | Pregunta, ParticipanteTrivia, RespuestaTrivia; Opcion, PuntajeAsignado, TiempoLimite, RankingTrivia | Owns its questions directly, created with the game (no bank/reuse); at least one complete question to publish. One answer per participant (or per team, first option from any active member). Question closes on first correct answer or timeout. Correct answer adds `PuntajeAsignado` directly; time never modifies score; tie-break by lowest accumulated answer time. |

### BDT (config in Partidas, runtime in Operaciones de Sesion, scoring in Puntuaciones)

| Aggregate | Entities / VO | Invariants |
|---|---|---|
| JuegoBDT | EtapaBDT, ParticipanteBDT, TesoroQR, Pista; AreaBusqueda, CodigoQREsperado, UbicacionGeografica, TiempoLimite, RankingBDT | One or more valid stages, each with expected QR text, `Puntaje`, and time limit. `AreaBusqueda` is text. QR validated by decoding the uploaded image vs expected text; multiple attempts allowed. Stage closes on first correct validation or timeout; in `Equipo` any active member's correct upload wins for the team. Geolocation mandatory. Native ranking by accumulated points (sum of won-stage `Puntaje`); tie-break by lowest accumulated time of won stages only; stages won is informative. |

### Auditoría (cross-cutting; materialized in Puntuaciones and Operaciones de Sesion)

| Aggregate | Entities / VO | Invariants |
|---|---|---|
| RegistroAuditoria | EventoHistorial; TipoEventoHistorial | Not a separate physical service. History is preserved even when a partida is cancelled or a team deleted; a cancelled partida keeps partial events/scores but does not count as a final result. |

## Domain events (selected)

| Event | When it occurs |
|---|---|
| UsuarioCreado / CredencialTemporalEmitida | User created / temporary password issued (creation or email change). |
| RolDeUsuarioModificado / PermisosDeRolModificados | Admin modifies a user's role / a role's permissions. |
| EquipoCreado / InvitacionEquipoCreada / InvitacionEquipoRespondida / LiderazgoTransferido / EquipoEliminado | Team and invitation lifecycle. |
| PartidaCreada / PartidaPublicadaEnLobby / PartidaIniciada / JuegoActivado / JuegoFinalizado / PartidaFinalizada / PartidaCancelada | Partida and sequential `Juego` lifecycle. |
| EquipoPreinscritoEnPartida / ConvocatoriaCreada / ConvocatoriaRespondida / InscripcionConfirmada | Participation lifecycle. |
| RespuestaTriviaValidada / PuntajeTriviaIncrementado / PreguntaTriviaCerrada / RankingTriviaActualizado | Trivia runtime and scoring. |
| TesoroQRValidado / EtapaBDTGanada (carries `Puntaje`) / EtapaBDTCerrada / RankingBDTActualizado / UbicacionParticipanteActualizada / PistaEnviada | BDT runtime and scoring. |
| RankingConsolidadoCalculado | Partida finishes and the consolidated ranking is computed. |

## Domain services (selected)

| Service | Responsibility |
|---|---|
| ValidadorJuegoTriviaService | Validate that a `JuegoTrivia` has at least one complete question. |
| GestorPermisosRolService / ValidadorCambioRolService | Apply per-role permission changes; validate role modification and Keycloak propagation. |
| ValidadorInscripcionService / ValidadorConvocatoriaService | Validate partida-level inscription/convocatoria and one-active-participation rule. |
| ValidadorInvitacionEquipoService / ValidadorEliminacionEquipoService | Team-invitation and team-deletion rules. |
| CalculadorRankingTriviaService | Order Trivia ranking by `PuntajeAcumulado` desc, tie-break lowest accumulated answer time. |
| ValidadorRespuestaTriviaService / ValidadorQRService | Validate Trivia answers / compare decoded QR to expected text. |
| CalculadorRankingBDTService | Order BDT ranking by accumulated won-stage points, tie-break lowest accumulated time of won stages only. |
| CalculadorRankingConsolidadoService | Determine each game's winner and compute the consolidated ranking (games won → total points → lowest total time). |
| ValidadorGeolocalizacionBDTService | Require authorized geolocation for an active BDT game. |
| ValidadorTransicionEstadoPartidaService | Validate `Lobby`/`Iniciada`/`Cancelada`/`Terminada` transitions. |
| GestorCredencialTemporalService | Decide temporary-credential issuance and mark it definitive. |
