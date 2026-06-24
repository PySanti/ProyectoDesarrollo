# Operaciones de Sesión — service context

Owns the live experience: publishing a partida (→ Lobby), start, question/stage synchronization,
answer/QR validation, sequential advance, clues, geolocation, reconnection, inscriptions & team
convocatorias; stores only transient session state and emits domain events via RabbitMQ.

Status: SP-0 shell — graded structure + `/health` + empty `OperacionesSesionDbContext`
(→ `umbral_operaciones_sesion`). Runtime extracted from Trivia/BDT in SP-3.
