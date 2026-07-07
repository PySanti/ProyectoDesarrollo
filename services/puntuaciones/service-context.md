# Puntuaciones — service context

Tracks scores and won stages, computes each game's native ranking and the consolidated partida
ranking, team-performance queries, and materializes audit/history. A read/projection model fed by
RabbitMQ domain events, broadcasting via SignalR. Owns neither configuration nor runtime.

Status: SP-4a — real projection consumer (queue `puntuaciones.operaciones-sesion.proyecciones`,
7 bindings, dedup by `eventId`, ADR-0012 best-effort) + projections (`partidas_proyectadas`,
`juegos_proyectados`, `marcadores`, `eventos_procesados` → `umbral_puntuaciones`) + native
per-game ranking and own-marcador HTTP queries (points DESC, time ASC; `unidadesGanadas`
informative only). SP-4b — consolidated partida ranking on-read (RF-45) + team historical
performance (RF-44), both over the same SP-4a projections via a single reused
`CalculadorRankingConsolidado`; 2 new HTTP endpoints. `xmin` as concurrency token on
`marcadores` (`Property<uint>("xmin").IsRowVersion()`), with a single retry in the worker on
`DbUpdateException` (base class — covers both an `xmin` UPDATE conflict and a unique-key INSERT
race). SP-4c — live ranking hub `puntuaciones/hubs/ranking` (`RankingHub`, `[Authorize]`) with the
3 messages `RankingTriviaActualizado`/`RankingBDTActualizado`/`RankingConsolidadoCalculado`,
best-effort broadcast orchestrated by the worker after each successful projection
(`ProyeccionPipeline` → `RankingBroadcastDispatcher` → `IRankingRealtimePublisher`); `[Authorize]`
on the read endpoints (`RankingsController`, `EquiposController`) and on the hub, `/health`
anonymous. SP-4d — segunda cola dedicada de historial
(`puntuaciones.operaciones-sesion.historial`, durable, binding catch-all `operaciones-sesion.#`,
`HistorialEventsConsumer` con el mismo esqueleto que el consumidor de proyecciones) proyectando
**los 17** eventos del contrato en la tabla nueva `eventos_historial` (`EventId` único como dedup
propio, `UbicacionActualizada` muestreada a 1/min/participante al escribir); dos HTTP nuevos:
`GET /puntuaciones/partidas/{partidaId}/historial` (HU-43, solo `Operador`/`Administrador`, 404 si
la partida no está proyectada) y `GET /puntuaciones/participantes/{participanteId}/historial-partidas`
(HU-27/RF-24, cualquier rol autenticado, reusa `CalculadorRankingConsolidado` de SP-4b). Con esto
la **serie SP-4 queda completa**.

Deuda anotada (review final SP-4a, actualizada en SP-4b, SP-4c y SP-4d): ramas warn+ack del worker
de la era SP-4a sin unit tests (cubiertas por el round-trip opt-in; la rama de difusión SP-4c sí
está cubierta por los unit tests del dispatcher + el E2E). **Retirada en SP-4b:** `marcadores` sin
token de concurrencia (saldada con `xmin` + reintento único del worker ante `DbUpdateException`).
**Retirada en SP-4c:** `[Authorize]`/hardening del servicio (saldada: `[Authorize]` en endpoints de
lectura y en el hub, `/health` anónimo). **Retiradas en SP-4d:** retención e índice temporal de
`eventos_procesados` (saldada: índice en `ProcesadoAt` + `PurgaEventosProcesadosService`, timer
cada 24 h, retención configurable `Retencion:EventosProcesadosDias`, default 30 días — nunca toca
`eventos_historial`); `ArgumentException`→400 sin log en el middleware (saldada: `LogWarning` con
mensaje + path antes de responder 400). Pending de la serie SP-4: **vacío** — el cableado de
clientes web/móvil a HTTP + SignalR de Puntuaciones queda para SP-5.
