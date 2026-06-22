# Domain Entities by Context

> Status: Current derived documentation. Source: `docs/01-project-source/` and `CLAUDE.md`.

## Target service topology

The backend is exactly four physical microservices behind a mandatory YARP gateway:

- **Identity**
- **Partidas**
- **Operaciones de Sesion**
- **Puntuaciones**

> Obsolete (superseded): the earlier split into `Team Service`, `Trivia Game Service`, and `BDT Game Service` is no longer a valid topology and must not be used. Teams live inside Identity; Trivia/BDT split across Partidas (config), Operaciones de Sesion (runtime), and Puntuaciones (scoring). Audit/history is cross-cutting; there is no separate Audit, Scoring, or Notification service.

## Identity

Owns:

- Usuario; UsuarioId, KeycloakId, Correo; RolUsuario, EstadoUsuario, EstadoCredencial; local user references.
- Rol; RolId, NombreRol; Privilegio, PermisoFuncional (per-role permissions/governance).
- Equipo, ParticipanteEquipo; EquipoId, NombreEquipo, EstadoEquipo; team membership and leadership.
- InvitacionEquipo; InvitacionEquipoId, EstadoInvitacion (members join only via invitation — no access code).
- HistorialEquipoUsuario (per-participant team-name history).
- Async email of temporary credentials (RabbitMQ).

Main team invariant:

```txt
1 <= Equipo.Participantes.Count <= 5
```

The creator of a team is the first participant and leader.

Does not own: partida configuration, live session/runtime, scoring/ranking.

## Partidas

Owns:

- Partida; PartidaId, NombrePartida, TiempoInicio, MinimosParticipacion, MaximosParticipacion; EstadoPartida, Modalidad, ModoInicioPartida.
- Juego (base) and its specializations' **configuration**; JuegoId, Orden, TipoJuego, EstadoJuego.
- Trivia configuration: Pregunta, Opcion, PuntajeAsignado, TiempoLimite (questions created with the `JuegoTrivia`; no bank, no reuse).
- BDT configuration: EtapaBDT with CodigoQREsperado (expected QR text), per-stage Puntaje, TiempoLimite, AreaBusqueda (descriptive text).

Does not own: running the live session, computing scores or ranking, inscriptions/convocatorias.

## Operaciones de Sesion

Owns:

- The live experience: publishing (→ `Lobby`), manual/automatic start, question/stage synchronization, answer & QR validation, sequential advance of games and stages, clue delivery (`Pista`), geolocation (`UbicacionGeografica`), reconnection, real-time session communication.
- Runtime competing units: ParticipanteTrivia, RespuestaTrivia (transient); ParticipanteBDT, TesoroQR (transient).
- Participación: InscripcionPartida and child Convocatoria; InscripcionId, ConvocatoriaId, EstadoInscripcion, EstadoConvocatoria (partida-level, once per partida).

Stores only transient session state; emits domain events via RabbitMQ; materializes part of audit/history.

Does not own: partida/game configuration (Partidas) or scoring/ranking (Puntuaciones).

## Puntuaciones

Owns:

- Scores and won stages; native rankings during and at end of play: RankingTrivia (by `PuntajeAcumulado` desc, tie-break lowest accumulated answer time) and RankingBDT (by accumulated won-stage `Puntaje`, tie-break lowest accumulated time of won stages only; `EtapasGanadas` informative).
- RankingConsolidado (on finish): games won → total accumulated points → lowest total time.
- Team-performance queries.
- Audit/history materialization: RegistroAuditoria, EventoHistorial, TipoEventoHistorial.

Read/projection model fed by RabbitMQ domain events; broadcasts updates via SignalR (through the gateway). Owns neither configuration nor runtime.

## Transversal concepts

### InscripcionPartida / Convocatoria

Partida-level (not per game). Owned by **Operaciones de Sesion**.

### RegistroAuditoria / EventoHistorial

Cross-cutting. No physical Audit Service. Materialized in **Puntuaciones** and **Operaciones de Sesion**.

### Puntaje / Ranking

No physical Scoring Service separate from Puntuaciones. All scoring and ranking (native and consolidated) belong to **Puntuaciones**.

## Gateway

The YARP gateway is the single entry point and a routing/entry-point concern only. It validates the Keycloak JWT and authorizes by base role at the route level; it owns no entities, domain logic, scores, rankings, or DB access.
