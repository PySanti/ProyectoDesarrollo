# Puntuaciones Service

## Status

Current target service.

## Responsibility

Puntuaciones materializes scoring + ranking + the Auditoría/Historial cross-cutting capability. It is a **read/projection model** fed by RabbitMQ domain events: it tracks scores and won stages, computes each game's native ranking during and at end of play, computes the consolidated partida ranking, answers team-performance queries, and materializes audit/history. It broadcasts updates via SignalR and owns neither configuration nor runtime. DB: `umbral_puntuaciones`.

## Owns

- Scores and won stages per participant/team.
- **Trivia native ranking** (per `JuegoTrivia`): `PuntajeAcumulado` descending (time never modifies points); tie-break by lowest accumulated answer time.
- **BDT native ranking** (per `JuegoBDT`): accumulated points = sum of the `Puntaje` of the **won stages**; tie-break by lowest accumulated time of the **won stages only**; stages-won count kept as informative data only.
- **Consolidated partida ranking** (`RankingConsolidado`, on finish): games won → total points → lowest total time.
- Team-performance queries.
- Materialization of audit/history (`RegistroAuditoria` / `EventoHistorial`).

## Does Not Own

- Partida/game configuration (Partidas).
- The live runtime, validation, clues or geolocation (Operaciones de Sesion).
- Identity, teams, roles or governance (Identity).
- The `PuntajeAsignado`/`Puntaje` definitions (those are configured in Partidas; Puntuaciones only consumes them via events).

## Communication

- HTTP through the YARP gateway (ranking, score, team-performance and history queries).
- RabbitMQ for cross-service domain events: consumes runtime events (e.g. `PuntajeTriviaIncrementado`, `EtapaBDTGanada` carrying `Puntaje`, `PartidaFinalizada`) and emits ranking events (`RankingTriviaActualizado`, `RankingBDTActualizado`, `RankingConsolidadoCalculado`).
- SignalR/WebSockets through the gateway for user-visible ranking updates.
