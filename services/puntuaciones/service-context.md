# Puntuaciones — service context

Tracks scores and won stages, computes each game's native ranking and the consolidated partida
ranking, team-performance queries, and materializes audit/history. A read/projection model fed by
RabbitMQ domain events, broadcasting via SignalR. Owns neither configuration nor runtime.

Status: SP-0 shell — graded structure + `/health` + empty `PuntuacionesDbContext`
(→ `umbral_puntuaciones`). Scoring/ranking projections arrive in SP-4.
