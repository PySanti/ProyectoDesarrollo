# SP-3b — Partida start (manual/automatic) + sequential game lifecycle (Operaciones de Sesión) — Design

- **Date:** 2026-06-26
- **Branch:** `feature/code-migration-SP-3`
- **Slice:** SP-3b, second sub-slice of SP-3 (runtime migration) of the four-service code-structure migration (`docs/superpowers/specs/2026-06-22-code-structure-doctrine-migration-design.md`).
- **Depends on:** SP-3a (publish → Lobby + Individual inscriptions; `SesionPartida` aggregate + `umbral_operaciones_sesion` + No-Op events seam + JWT identity), merged on this branch.
- **Service:** Operaciones de Sesión (`services/operaciones-sesion`, namespace `Umbral.OperacionesSesion.*`, DB `umbral_operaciones_sesion`, local port 5020, gateway route `/operaciones-sesion/*` — ADR-0009).
- **Canonical structure reference:** the SP-3a code already in this service (mirror its `ValidationBehavior`, middleware, controller-dispatch, fakes, and InMemory test pattern).

## 1. Goal

Bring the published `SesionPartida` to life: **start** a partida (manual operator action **and** an automatic time-based path), enforce **participation minimums** on start (failing them **auto-cancels** the partida), activate games **sequentially** (first game → `Activo`), and advance the lifecycle through `FinalizarJuegoActual` until the last game finishes and the partida reaches `Terminada`. All state lives in Operaciones as transient session state (R1 / ADR-0010); domain events flow through the existing No-Op publisher port.

This is **not** gameplay. Answering questions / validating QR (the *real* trigger that finalizes a game) is SP-3c/3d. SP-3b ships the **state machine** and a neutral domain transition (`FinalizarJuegoActual`) that 3c/3d will reuse internally, plus an operator endpoint to drive it so the full lifecycle is exercisable end-to-end now.

## 2. Locked decisions (from brainstorming — not re-litigated here)

1. **Full state machine in 3b.** Start + auto-cancel + sequential advance + `Terminada`. The class diagram's `EstadoPartida ∈ {Lobby, Iniciada, Cancelada, Terminada}` is fully materialized at runtime inside Operaciones (`SesionPartida.Estado`), per R1/ADR-0010.
2. **Automatic start = domain rule + idempotent endpoint, no scheduler.** The mode/`TiempoInicio`/minimums rule lives in the domain (`TimeProvider`-driven, deterministic). An idempotent `POST /inicio-automatico` ("start if due") is exposed for a future poller/scheduler/operator to call. The background worker that polls due partidas is **deferred** (SP-3f or its own slice) — 3b builds the seam, not the timer.
3. **Game advance = neutral domain transition + operator endpoint.** `FinalizarJuegoActual` finalizes the active game and activates the next (or terminates the partida). SP-3c/3d reuse the **same** domain method internally when their win/timeout condition fires. An operator endpoint (`POST /juego-actual/finalizacion`) drives it in 3b. Operator force-finish is slightly beyond strict doctrine (games normally close by gameplay) but is a useful operational control and the only way to exercise sequencing before gameplay exists.
4. **Auto-cancellation is not an error.** Minimums not met on start is a **valid outcome**: the endpoint returns `200` with `estado = Cancelada` (and emits `PartidaCancelada`), not a 4xx.
5. **Events via the existing No-Op publisher port.** New event payloads are emitted through `ISesionEventsPublisher` / `NoOpSesionEventsPublisher`. The real RabbitMQ backbone remains its own dedicated slice before SP-4.

## 3. Scope

**IN:**
- New enum `EstadoJuego{Pendiente, Activo, Finalizado}`; `JuegoResumen` gains `Estado` (+ internal `Activar()`/`Finalizar()` transitions).
- `SesionPartida` gains `FechaInicio`, `FechaFin` and the methods `Iniciar(DateTime now)`, `IntentarInicioAutomatico(DateTime now)`, `FinalizarJuegoActual(DateTime now)`, each returning a small result record for the handler to publish the right events.
- Minimums enforcement → auto-cancellation (`Cancelada`) on start.
- Sequential game activation (first game `Activo` on start; next on each `FinalizarJuegoActual`; `Terminada` when the last finishes).
- Commands `IniciarPartidaCommand`, `IntentarInicioAutomaticoCommand`, `FinalizarJuegoActualCommand`; query `ObtenerEstadoSesionQuery`; handlers, validators, DTOs.
- Endpoints: `POST /partidas/{id}/inicio`, `POST /partidas/{id}/inicio-automatico`, `POST /partidas/{id}/juego-actual/finalizacion`, `GET /partidas/{id}/estado`.
- Events (No-Op port): `PartidaIniciada`, `JuegoActivado`, `PartidaFinalizada`, **`PartidaCancelada` (new in the registry)**.
- `TimeProvider` for automatic-start time gating + lifecycle timestamps.
- EF migration adding `EstadoJuego` column + `FechaInicio`/`FechaFin` columns. InMemory fallback unchanged.
- Contracts: register the 4 endpoints + DTOs; register the 4 event payloads (incl. the new `PartidaCancelada`).
- Tests: domain-invariant unit, application (handlers/validators with fakes), controller unit (graded), middleware, contract (`WebApplicationFactory<Program>` + InMemory) covering the full lifecycle.
- R1 structural gate + green suite.

**OUT (later slices):**
- Trivia runtime: question sync, `RespuestaTrivia` validation — the **real** trigger of `FinalizarJuegoActual` (**SP-3c**).
- BDT runtime: stage sync, `TesoroQR` validation, clues, geolocation (**SP-3d**).
- The background scheduler/worker that polls due partidas and calls `/inicio-automatico` (**SP-3f** or its own slice).
- Reconnection / transient-state recovery (**SP-3e**).
- SignalR/WebSockets live push of state/timers (**SP-3f**).
- Real RabbitMQ backbone (its own slice before SP-4).
- `Equipo` modality + `Convocatoria` inscriptions (**SP-3a-E**). An `Equipo` partida has no inscriptions until 3a-E, so starting it always finds `0 < Mínimos` and auto-cancels — honest and consistent, no special handling.
- Manual operator cancellation of a `Lobby` partida (not required by doctrine; YAGNI).
- Scoring/ranking/audit (**SP-4**); client repoint + old-service retirement (**SP-5**).

## 4. Domain model changes

### 4.1 New enum `EstadoJuego`

`EstadoJuego{Pendiente, Activo, Finalizado}` (CLAUDE.md domain essentials). Games are `Pendiente` at publish, `Activo` one at a time, `Finalizado` once done.

### 4.2 `JuegoResumen` (existing entity, extended)

- New field `EstadoJuego Estado` (default `Pendiente` for new instances; persisted).
- Internal transitions (callable only from the `SesionPartida` aggregate root):
  - `Activar()` — `Pendiente → Activo` (defensive guard: only from `Pendiente`).
  - `Finalizar()` — `Activo → Finalizado` (defensive guard: only from `Activo`).
- Construction at publish (SP-3a path) still produces `Pendiente`.

### 4.3 `SesionPartida` (aggregate root, extended)

New fields: `DateTime? FechaInicio`, `DateTime? FechaFin`.

**`Iniciar(DateTime now) → ResultadoInicio`** (manual start):
1. `Estado == Lobby` else `SesionNoEnLobbyException`.
2. mode allows manual (`Manual` or `ManualYAutomatico`) else `ModoInicioNoCompatibleException`.
3. compute `inscritosActivos = Inscripciones.Count(i => i.EsActiva)` (own state).
4. if `inscritosActivos < MinimosParticipacion`: `Estado = Cancelada`, `FechaFin = now` → return `ResultadoInicio.Cancelada`.
5. else: `Estado = Iniciada`, `FechaInicio = now`, first game (lowest `Orden`) `.Activar()` → return `ResultadoInicio.Iniciada(juegoActivado)`.

**`IntentarInicioAutomatico(DateTime now) → ResultadoInicio`** (idempotent automatic path):
1. mode allows automatic (`Automatico` or `ManualYAutomatico`) else `ModoInicioNoCompatibleException`.
2. if `Estado != Lobby` → return `ResultadoInicio.NoCorresponde` (idempotent no-op; already started/cancelled/terminated).
3. if `TiempoInicio is null || now < TiempoInicio` → return `ResultadoInicio.NoCorresponde` (not due yet).
4. else apply the same minimums logic as `Iniciar` steps 3–5 (`Iniciada` or `Cancelada`).

**`FinalizarJuegoActual(DateTime now) → ResultadoAvance`** (neutral advance; reused by 3c/3d):
1. `Estado == Iniciada` else `SesionNoIniciadaException`.
2. current = the single `Activo` game; finalize it (`.Finalizar()`).
3. next = lowest-`Orden` `Pendiente` game. If present: `.Activar()` → return `ResultadoAvance.Avanzado(juegoFinalizado, juegoActivado)`.
4. else: `Estado = Terminada`, `FechaFin = now` → return `ResultadoAvance.Terminada(juegoFinalizado)`.

**Result records (Domain):**
- `ResultadoInicio` — discriminates `Iniciada` (carries activated game) / `Cancelada` / `NoCorresponde`. Modeled as a small sealed type or an enum + nullable activated-game reference (implementer's choice; must let the handler publish the correct events without re-reading state).
- `ResultadoAvance` — discriminates `Avanzado` (carries finalized + activated game) / `Terminada` (carries finalized game).

**Existing methods unchanged.** `Inscribir`/`CancelarInscripcion` already guard `Estado == Lobby`, so once `Iniciada`/`Cancelada`/`Terminada` they reject naturally — no change needed. `Inscribir` keeps its existing `inscritosActivos` parameter (not refactored; out of scope), while the new start methods read own state. This asymmetry is intentional and noted in §10.

### 4.4 New exceptions (Domain)

- `ModoInicioNoCompatibleException(Guid partidaId)` → 409 (manual start on `Automatico`-only, or automatic start on `Manual`-only).
- `SesionNoIniciadaException(Guid partidaId)` → 409 (`FinalizarJuegoActual` when not `Iniciada`).

Auto-cancellation by minimums is **not** an exception — it is a valid `ResultadoInicio.Cancelada` outcome, surfaced as `200` with `estado = Cancelada`.

## 5. Application layer (CQRS / MediatR, graded folders)

Graded `Application/` folders unchanged (`Commands/`, `Queries/`, `Interfaces/`, `Validators/`, `DTOs/`, `Handlers/Commands/`, `Handlers/Queries/`, `Exceptions/` + root `ValidationBehavior.cs`/`DependencyInjection.cs`).

- **Commands:** `IniciarPartidaCommand(Guid PartidaId)`, `IntentarInicioAutomaticoCommand(Guid PartidaId)`, `FinalizarJuegoActualCommand(Guid PartidaId)`.
- **Query:** `ObtenerEstadoSesionQuery(Guid PartidaId)`.
- **Handlers/Commands:**
  - `IniciarPartidaCommandHandler` — `GetByPartidaIdAsync` (`null` → `SesionNoEncontradaException`/404); `now = TimeProvider.GetUtcNow().UtcDateTime`; `Iniciar(now)`; `SaveChanges`; publish per `ResultadoInicio` (`Iniciada` → `PartidaIniciada` **and** `JuegoActivado`; `Cancelada` → `PartidaCancelada`).
  - `IntentarInicioAutomaticoCommandHandler` — same load + `IntentarInicioAutomatico(now)`; `NoCorresponde` → **no** save, **no** event (pure idempotent no-op); `Iniciada`/`Cancelada` → save + publish as above.
  - `FinalizarJuegoActualCommandHandler` — load (`null` → 404); `FinalizarJuegoActual(now)`; save; publish (`Avanzado` → `JuegoActivado`; `Terminada` → `PartidaFinalizada`).
- **Handlers/Queries:** `ObtenerEstadoSesionQueryHandler` — load (`null` → 404) → map to `EstadoSesionDto` (single pass over games).
- **Validators (FluentValidation):** one per command (`PartidaId` non-empty). Business-rule violations are domain exceptions, not validation.
- **DTOs:**
  - `InicioPartidaResponse { partidaId, estado, juegoActivadoId?, juegoActivadoOrden? }` (estado may be `Iniciada`, `Cancelada`, or — for the automatic no-op — `Lobby`).
  - `AvanceJuegoResponse { partidaId, estado, juegoFinalizadoOrden?, juegoActivadoOrden?, terminada }`.
  - `EstadoSesionDto { partidaId, sesionPartidaId, estado, modalidad, juegos[]{ juegoId, orden, tipoJuego, estado }, juegoActualOrden? }`.
- **Interfaces:** extend `ISesionEventsPublisher` with `PublicarPartidaIniciadaAsync`, `PublicarJuegoActivadoAsync`, `PublicarPartidaFinalizadaAsync`, `PublicarPartidaCanceladaAsync` (+ the event records). Repository interface stays in `Domain/`.
- **Events publishing style:** handlers publish **explicitly** based on the domain `Resultado*` (the SP-3a pattern). No domain-events collection on the aggregate — YAGNI until the backbone slice gives events a real consumer. Recorded as a micro-decision (§10).
- **Clock:** `TimeProvider` (already registered in SP-3a DI) injected into the three command handlers; never `DateTime.UtcNow` inline. The domain methods receive `now` as a parameter (stay pure / deterministic).

Commands mutate; queries never mutate. Controllers dispatch via `_mediator.Send(...)` only.

## 6. Persistence (`umbral_operaciones_sesion`, EF Core + Npgsql)

- `JuegoResumen.Estado` mapped as `int` (mirroring the existing enum-as-int mapping) → new column on `sesion_juegos` (default `Pendiente`/0).
- `SesionPartida.FechaInicio`, `FechaFin` mapped as nullable timestamps → new columns on `sesiones_partida`.
- **No repository changes required.** Start/advance handlers load via the existing `GetByPartidaIdAsync` (includes inscriptions + games) and compute minimums from the loaded aggregate; no new repo method.
- **New EF migration** adding the three columns. `dotnet-ef` local tool (added in SP-3a) generates it.
- **InMemory fallback** unchanged — application/contract tests run without Postgres (SP-0/SP-2/SP-3a pattern).

## 7. Api layer (graded)

Extend the existing `SesionesController` (native `ControllerBase`, MediatR dispatch, no business logic) — no new controller.

| Capability | Verb | Route | Actor | Success |
|---|---|---|---|---|
| Start (manual) | POST | `/partidas/{id}/inicio` | Operador | 200 + `InicioPartidaResponse` |
| Start-if-due (automatic, idempotent) | POST | `/partidas/{id}/inicio-automatico` | Operador/Sistema | 200 + `InicioPartidaResponse` |
| Finalize current game (advance) | POST | `/partidas/{id}/juego-actual/finalizacion` | Operador | 200 + `AvanceJuegoResponse` |
| Session state | GET | `/partidas/{id}/estado` | Operador/Participante | 200 + `EstadoSesionDto` / 404 |

- **200, not 201** — these are state transitions, not resource creation.
- **Auth unchanged from SP-3a:** JWT identity validation is wired; functional-permission authorization and gateway coarse-role policy remain deferred to SP-5 (no `[Authorize]` attributes). These are operator/system actions; the `sub` claim is not consumed here (unlike inscriptions).
- `Program.cs` registration unchanged (already registers controllers via `MapControllers`, the typed `HttpClient`, the InMemory/Npgsql switch, MediatR + `ValidationBehavior`, and `ISesionEventsPublisher → NoOpSesionEventsPublisher`).
- **Every controller has unit tests** (graded, required) — extend `SesionesControllerTests` with the 4 new endpoints.

## 8. Error handling

Central `ExceptionHandlingMiddleware` (existing) gains two arms; no try/catch in controllers or handlers.

| Exception | Status |
|---|---|
| `ModoInicioNoCompatible` | 409 |
| `SesionNoIniciada` | 409 |
| `SesionNoEncontrada` (existing) | 404 |

All SP-3a arms (`PartidaConfigNoEncontrada` 404, `PartidasConfigInaccesible` 502, `SesionYaPublicada` 409, `PartidaNoPublicable` 409, `SesionNoEnLobby` 409, `ParticipanteYaInscrito`/`InscripcionNoEncontrada` 409/404, `ParticipacionActivaExistente` 409, `CupoLleno` 409, `ModalidadNoSoportada` 409, `ParticipanteNoIdentificado` 401, `ValidationException` 400) remain unchanged.

Auto-cancellation produces **no** exception — it is `200` with `estado = Cancelada`.

## 9. Contracts

- `contracts/http/operaciones-sesion-api.md` — register the 4 new endpoints (verbs/routes/`:guid`, request/response shapes, status codes) and the 3 new DTOs (`InicioPartidaResponse`, `AvanceJuegoResponse`, `EstadoSesionDto`). Authored as source-of-truth, asserted by contract tests.
- `contracts/events/operaciones-sesion-events.md` — register the payloads for `PartidaIniciada`, `JuegoActivado`, `PartidaFinalizada`, and **add a new `PartidaCancelada` row + payload**. Emitted through the No-Op port; exchange/queue/routing-key/idempotency remain "defined by the backbone slice." Payloads:
  - `PartidaIniciada { partidaId, sesionPartidaId, fechaInicio, primerJuegoId, primerJuegoOrden }`
  - `JuegoActivado { partidaId, sesionPartidaId, juegoId, orden, tipoJuego }`
  - `PartidaFinalizada { partidaId, sesionPartidaId, fechaFin }` (consolidated ranking is computed by Puntuaciones in SP-4; this payload only signals finish)
  - `PartidaCancelada { partidaId, sesionPartidaId, motivo, fechaCancelacion }` (`motivo = "MinimosNoAlcanzados"` in 3b)

## 10. Micro-decisions (recorded, low-risk)

- **Explicit handler-side event publishing** per domain `Resultado*` (no aggregate domain-events collection). Alternative considered: a domain-events collection drained after `SaveChanges` — deferred as speculative infra while the backbone is No-Op.
- **Start reads own state for minimums** (`Inscripciones.Count(EsActiva)`), no count parameter and no new repo method, whereas the SP-3a `Inscribir` takes an `inscritosActivos` parameter. Both are own-state; `Inscribir` is **not** refactored (out of scope). The asymmetry is intentional.
- **`Equipo` partidas always auto-cancel on start** until SP-3a-E (no inscriptions exist), which is the honest, consistent outcome — no modality-specific start handling.
- **Auto-cancellation is `200 + Cancelada`**, not a 4xx (a failed-minimums start is a valid, expected business outcome).
- **No manual operator cancellation** of a `Lobby` partida (doctrine only mandates auto-cancellation by minimums). Revisit only if a story requires it.
- **`200` (not `201`)** for start/advance — state transitions on an existing resource, no new resource created.
- **`PartidaCancelada` is new** in the event registry (SP-3a registered only `PartidaPublicadaEnLobby`).
- **Automatic-start idempotency:** `/inicio-automatico` is safe to call repeatedly — once not in `Lobby`, or before `TiempoInicio`, it is a no-op returning the current state with no event and no save.

## 11. R1 structural gate

Before close: `Api/Controllers/` present, no minimal-API routes (only `MapControllers`); exact graded `Application/` folder set; `Infrastructure/` has `Persistence/` + `Services/`; repository interface in `Domain/`; exception middleware registered with the two new arms; controller unit tests cover the 4 new endpoints; full suite green on InMemory. Mirrors the SP-0/SP-2/SP-3a gate.

## 12. Testing

- **Unit (Domain):**
  - `Iniciar` — minimums met → `Iniciada` + first game `Activo` + `FechaInicio` set; minimums not met → `Cancelada` + `FechaFin` set; not `Lobby` → `SesionNoEnLobby`; mode `Automatico`-only → `ModoInicioNoCompatible`.
  - `IntentarInicioAutomatico` — due + met → `Iniciada`; due + not met → `Cancelada`; `now < TiempoInicio` → `NoCorresponde`; not `Lobby` → `NoCorresponde` (idempotent); mode `Manual`-only → `ModoInicioNoCompatible`.
  - `FinalizarJuegoActual` — advances to next `Pendiente` (current → `Finalizado`, next → `Activo`); last game → `Terminada` + `FechaFin`; not `Iniciada` → `SesionNoIniciada`.
  - `JuegoResumen.Activar/Finalizar` guards.
- **Application (handlers, with fakes):** each handler happy + error paths; assert the **correct events** are published per outcome via an extended `FakeSesionEventsPublisher` (records each event kind). `IntentarInicioAutomatico` `NoCorresponde` → no save, no event. Reuse the SP-3a fakes (`FakeSesionPartidaRepository`, `FakeSesionEventsPublisher`, `FakeConfiguracionPartidaClient`).
- **Validators (unit):** each new `*CommandValidator`.
- **Controller unit (graded, required):** the 4 new endpoints dispatch the right command/query via mocked `IMediator` and return the right status (200/200/200/200·404). Pure dispatch.
- **Middleware (unit):** `ModoInicioNoCompatible` → 409, `SesionNoIniciada` → 409.
- **Contract (`WebApplicationFactory<Program>`, InMemory):** full lifecycle — publish → inscribe to meet minimums → `POST /inicio` → `GET /estado` shows `Iniciada` + game 1 `Activo` → `POST /juego-actual/finalizacion` repeatedly until `Terminada`; start with minimums-not-met → `200 + Cancelada`; start when not `Lobby` → 409; `/inicio-automatico` not due → no-op `200`; finalize when not `Iniciada` → 409.
- Whole Operaciones suite green on InMemory (no Postgres needed), following SP-0/SP-2/SP-3a.

## 13. Risks / open points

- **"Single active game" assumption** in `FinalizarJuegoActual` — relies on exactly one `Activo` game while `Iniciada`. The state machine guarantees it (start activates exactly one; advance finalizes one and activates at most one). Cover with a multi-game sequencing test.
- **EF column defaults on existing rows** — the new `EstadoJuego` column needs a sensible default (`Pendiente`/0) so any pre-existing `sesion_juegos` rows remain valid; the migration sets it. InMemory is unaffected.
- **Automatic-start without a scheduler** — `/inicio-automatico` is a seam, not a live timer. Until the scheduler slice lands, automatic start only happens when something calls the endpoint. Documented as deferred (§3 OUT); the domain rule is fully tested so the worker is purely mechanical when it arrives.
- **Operator force-finish vs gameplay-driven finish** — the operator `/juego-actual/finalizacion` endpoint is the 3b driver; in 3c/3d the same domain method is called by the gameplay win/timeout path. The endpoint may remain as an operator override or be re-scoped when gameplay lands; noted for the 3c/3d design.
