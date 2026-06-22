# Events Catalog

> **Authority:** derived from `CLAUDE.md`, `docs/01-project-source/modelo-de-dominio.md` and `docs/01-project-source/diagrama-de-clases.md`. Only **canonical** event names already present in those sources are listed here. Exchange/queue/routing-key names, payload schemas, versions, idempotency and outbox policy are **not** invented; they are defined per HU during SDD and recorded under `contracts/events/`.

## Messaging model

Domain events flow over **RabbitMQ** between services; user-facing real-time updates flow over **SignalR/WebSockets** through the gateway. Events feed **Puntuaciones** (scoring/ranking/audit) and audit materialization in **Operaciones de Sesion**. No service reads another service's database — cross-context state is carried by events.

## Canonical domain events by producing service

These names are taken verbatim from the source domain model. Their concrete payloads and routing keys are defined per HU in SDD.

### Identity

| Event | Description |
|---|---|
| `UsuarioCreado` | A user was created (drives the async temporary-credential email). |
| `CredencialTemporalEmitida` | A temporary credential was issued / re-issued. |

Team, invitation and convocatoria-adjacent membership events also originate here; their canonical names are defined alongside the team SDDs.

### Partidas

Configuration is owned here; runtime publication/activation events are emitted by Operaciones de Sesion. Partidas exposes its configuration via gateway-routed queries rather than runtime events.

### Operaciones de Sesion

| Event | Description |
|---|---|
| `PartidaPublicadaEnLobby` | A partida was published and moved to `Lobby`. |
| `PartidaIniciada` | A partida started (manual/automatic). |
| `JuegoActivado` | The next `Juego` in sequence became `Activo`. |
| `RespuestaTriviaValidada` | A Trivia answer was validated. |
| `TesoroQRValidado` | An uploaded QR was decoded and validated against the expected text. |
| `EtapaBDTGanada` | A BDT stage was won; **carries the stage `Puntaje`**. |
| `PartidaFinalizada` | A partida finished. |

### Puntuaciones

| Event | Description |
|---|---|
| `PuntajeTriviaIncrementado` | A participant's Trivia accumulated points increased. |
| `RankingTriviaActualizado` | The Trivia native ranking changed. |
| `RankingBDTActualizado` | The BDT native ranking changed (by accumulated points of won stages). |
| `RankingConsolidadoCalculado` | The consolidated partida ranking was computed on finish. |

## Audit/history event types

`RegistroAuditoria` aggregates `EventoHistorial` entries per partida; audit/history is a cross-cutting capability materialized in **Puntuaciones** and **Operaciones de Sesion**. The `TipoEventoHistorial` enum (from `diagrama-de-clases.md`) classifies historical events:

`CambioEstado`, `Inscripcion`, `Convocatoria`, `InvitacionEquipo`, `JuegoActivado`, `JuegoFinalizado`, `RespuestaTrivia`, `TesoroSubido`, `ValidacionQR`, `PistaEnviada`, `Ubicacion`, `Ranking`, `RankingConsolidado`, `Puntaje`, `Cancelacion`, `Resultado`, `EquipoEliminado`, `CambioRol`, `PermisosRol`.

## Ranking event doctrine

- **Trivia native**: `PuntajeTriviaIncrementado` (points up; time never modifies points) → `RankingTriviaActualizado`.
- **BDT native**: `EtapaBDTGanada` carries the won stage `Puntaje` → `RankingBDTActualizado`. Ranking is by accumulated points; stages-won count is informative only. The old "rank by stages won" rule is obsolete.
- **Consolidated**: `RankingConsolidadoCalculado` on finish (games won → total points → lowest total time).

## Rule for event contracts

Before implementing an event in an HU, the SDD must complete:

```md
| Field | Value |
|---|---|
| Event | <canonical name> |
| Producer | Identity / Operaciones de Sesion / Puntuaciones |
| Consumer | Defined in SDD |
| Reason | <RF/RB/HU> |
| Payload | Defined in SDD |
| Real-time effect | Yes / No |
| Recorded in history | Yes / No |
```
