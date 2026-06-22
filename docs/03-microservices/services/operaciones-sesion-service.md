# Operaciones de Sesion Service

## Status

Current target service.

## Responsibility

Operaciones de Sesion materializes the Trivia/BDT **runtime** and the Participación (Support) context. It runs the live experience — from publishing a partida to the lobby through to its finish — and owns partida-level inscriptions and convocatorias. It stores only **transient** session state and emits domain events via RabbitMQ. It owns no configuration and no scoring/ranking. DB: `umbral_operaciones_sesion`.

## Owns

- Publishing a partida (→ `Lobby`) and manual/automatic start (a start failing the minimums triggers automatic cancellation).
- Question/stage synchronization and sequential advance of games and stages.
- Answer validation (a Trivia question closes on first correct answer or timeout; in `Equipo`, the first option sent by any active member) and QR validation (decode the uploaded image, compare to the expected text; a stage closes on first correct validation or timeout; in `Equipo`, a correct upload by any active member wins it for the team).
- Clue delivery, geolocation (mandatory for an active BDT game; location ~every 2 seconds to the operator), reconnection and real-time session communication.
- Inscriptions (`InscripcionPartida`) and team convocatorias (`Convocatoria`) — partida-level, once per partida, one active participation at a time.
- Only **transient** session state; emits domain events via RabbitMQ.
- Materialization of its own audit/history.

## Does Not Own

- Partida/game configuration (Partidas).
- Scoring and ranking (Puntuaciones).
- Identity, teams, roles or governance (Identity).
- Persistent game configuration or final ranking storage.

## Communication

- HTTP through the YARP gateway.
- RabbitMQ for cross-service domain events where required (e.g. `PartidaPublicadaEnLobby`, `PartidaIniciada`, `JuegoActivado`, `RespuestaTriviaValidada`, `TesoroQRValidado`, `EtapaBDTGanada`, `PartidaFinalizada`) feeding Puntuaciones and audit.
- SignalR/WebSockets through the gateway for user-visible updates (lobby, states, timers, stages, clues, geolocation, results, device synchronization).
