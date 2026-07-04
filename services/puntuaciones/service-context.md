# Puntuaciones — service context

Tracks scores and won stages, computes each game's native ranking and the consolidated partida
ranking, team-performance queries, and materializes audit/history. A read/projection model fed by
RabbitMQ domain events, broadcasting via SignalR. Owns neither configuration nor runtime.

Status: SP-4a — real projection consumer (queue `puntuaciones.operaciones-sesion.proyecciones`,
7 bindings, dedup by `eventId`, ADR-0012 best-effort) + projections (`partidas_proyectadas`,
`juegos_proyectados`, `marcadores`, `eventos_procesados` → `umbral_puntuaciones`) + native
per-game ranking and own-marcador HTTP queries (points DESC, time ASC; `unidadesGanadas`
informative only). Pending: consolidated ranking + team performance (SP-4b), live ranking
SignalR (SP-4c), audit/history projection (SP-4d).
