# Partidas Service

## Status

Current target service.

## Responsibility

Partidas materializes the Partidas (Core) bounded context plus Trivia/BDT **configuration**. It owns the creation and configuration of a `Partida` and its `Juego`s — including their sequential order — and the content of each game: Trivia questions (created with the game) and BDT stages. It does not run the live session or compute scores/ranking. DB: `umbral_partidas`.

## Owns

- Creation and configuration of a `Partida`: modality (`Individual`/`Equipo`, fixed once), min/max participation, start mode (`Manual`/`Automatico`/`ManualYAutomatico`) and start time.
- Its `Juego`s and their sequential `Orden`, each specialized as `JuegoTrivia` or `JuegoBDT`.
- Trivia `Pregunta` content — options, correct answer, `PuntajeAsignado` and time limit — created **with** the game (no question bank, no reuse).
- BDT `EtapaBDT` content — expected QR **text** (área de búsqueda is descriptive text, not coordinates), per-stage `Puntaje` and time limit.
- The configuration data model for partidas and games.

## Does Not Own

- Running the live session: publication, start, synchronization, validation, advance, clues, geolocation (Operaciones de Sesion).
- Inscriptions (`InscripcionPartida`) and convocatorias (Operaciones de Sesion).
- Scoring and ranking (Puntuaciones).
- Identity, teams, roles or governance (Identity).
- Transient session state.

## Communication

- HTTP through the YARP gateway.
- RabbitMQ for cross-service domain events where required.
- SignalR/WebSockets through the gateway for user-visible updates where required.
