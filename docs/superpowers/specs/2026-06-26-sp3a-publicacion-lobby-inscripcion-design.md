# SP-3a — Publish → Lobby + Individual inscriptions (Operaciones de Sesión) — Design

- **Date:** 2026-06-26
- **Branch:** `feature/code-migration-SP-3`
- **Slice:** SP-3a, first sub-slice of SP-3 (runtime migration) of the four-service code-structure migration (`docs/superpowers/specs/2026-06-22-code-structure-doctrine-migration-design.md`).
- **Depends on:** SP-0 (foundation: YARP gateway + `operaciones-sesion` graded shell + `umbral_operaciones_sesion` DB + `/health`), merged. SP-2 (Partidas config: `Partida`/`Juego` + `GET /partidas/{id}` contract), merged.
- **Service:** Operaciones de Sesión (`services/operaciones-sesion`, namespace `Umbral.OperacionesSesion.*`, DB `umbral_operaciones_sesion`, local port 5020, gateway route `/operaciones-sesion/*` — ADR-0009).
- **Canonical structure reference:** `services/partidas` (graded-compliant after SP-2; mirror its `ValidationBehavior`, middleware, controller-dispatch, and InMemory test pattern).

## 1. Goal

Stand up the **live-session foundation** in Operaciones de Sesión: publish a configured `Partida` into a **Lobby**, and let participants **inscribe** into it under **Individual** modality. Operaciones owns the runtime lifecycle as **transient session state**, fed at publish time by a **synchronous HTTP config snapshot** from Partidas. No start, no gameplay, no scoring.

This is a **FRESH-BUILD** of the session aggregate from doctrine (`CLAUDE.md` domain essentials, `docs/01-project-source/`), **not** a port of the old `trivia-game-service`/`bdt-game-service` runtime. The old services keep running until SP-3c/3d replace their runtime and SP-5 retires them.

## 2. Locked decisions (from brainstorming — not re-litigated here)

1. **R1 — runtime estado lives in Operaciones.** The `SesionPartida` aggregate carries its own `EstadoSesion`; Partidas' `EstadoPartida` (made nullable in SP-2) **stays `null` forever** — Partidas is config-only and cannot be written across the service boundary. The class-diagram's `EstadoPartida ∈ {Lobby, Iniciada, Cancelada, Terminada}` is **materialized at runtime** inside Operaciones. A short mini-ADR records this (§13).
2. **Config handoff = synchronous internal HTTP snapshot (Option A).** On publish, Operaciones calls Partidas' `GET /partidas/{id}` (SP-2 contract), copies the partida-level config + game references into its transient store, and creates the session in `Lobby`. *This reverses an earlier RabbitMQ request/reply choice* after we surfaced that messaging is entirely No-Op/unbuilt in the repo (no MassTransit/RabbitMQ client libs in any `.csproj`; `NoOpEquipoEventsPublisher` is the only adapter).
3. **Domain events via a No-Op publisher port.** `ISesionEventsPublisher` + `NoOpSesionEventsPublisher` mirror the Identity seam. The **real RabbitMQ backbone is its own dedicated slice before SP-4** (Puntuaciones), when there is a consumer that needs the events.
4. **SignalR deferred to SP-3f.** SP-3a is HTTP commands/queries; live push (lobby/timers/state) lands in 3f once there is substance to push.
5. **Individual modality only.** Inscriptions for `Modalidad.Individual`; **`Equipo` → convocatorias → next sub-slice SP-3a-E** (which introduces the Identity dependency). A published `Equipo` partida rejects inscription with `ModalidadNoSoportada` until 3a-E.

## 3. Scope

**IN:**
- `SesionPartida` aggregate (transient runtime root) + `InscripcionPartida` child, in `umbral_operaciones_sesion`.
- Publish flow: `PublicarPartidaCommand` → HTTP snapshot from Partidas → create session in `Lobby` → emit `PartidaPublicadaEnLobby` (No-Op port).
- Individual inscription flow: `InscribirParticipanteCommand`, `CancelarInscripcionCommand` with the "one active participation at a time" invariant, capacity (Máximos), Lobby-only guards.
- Query: `ObtenerLobbyQuery` (session state + active inscriptions + capacity).
- Config-snapshot port `IConfiguracionPartidaClient` + `PartidasConfigHttpClient` (typed HttpClient), with 404-vs-upstream-failure error distinction.
- Events port `ISesionEventsPublisher` + `NoOpSesionEventsPublisher`.
- Persistence (EF Core + Npgsql), initial migration, InMemory fallback for tests (SP-2 pattern).
- Contracts: register the 4 endpoints in `contracts/http/operaciones-sesion-api.md`; register `PartidaPublicadaEnLobby` payload in `contracts/events/operaciones-sesion-events.md`.
- Tests: domain-invariant unit, application (handlers/validators with fakes), **controller unit tests** (graded), `ValidationBehavior` unit, contract (`WebApplicationFactory<Program>` + stub config client), middleware.
- Mini-ADR for R1. R1 structural gate + green suite.

**OUT (later slices):**
- Start (manual/automatic), minimums enforcement, sequential game/stage activation, lifecycle `Iniciada/Cancelada/Terminada` (**SP-3b**).
- Trivia runtime: question sync, `RespuestaTrivia` validation (**SP-3c**).
- BDT runtime: stage sync, `TesoroQR` validation, clues, geolocation (**SP-3d**).
- Reconnection / transient-state recovery (**SP-3e**).
- SignalR/WebSockets live push (**SP-3f**).
- Real RabbitMQ backbone (its own slice before SP-4).
- `Equipo` modality + `Convocatoria` (**SP-3a-E**).
- Scoring/ranking/audit (**SP-4**); client repoint + old-service retirement (**SP-5**).

## 4. Domain model

> Operaciones defines its **own** copies of the cross-cutting enums (`Modalidad`, `ModoInicioPartida`, `TipoJuego`). There is **no shared domain project** between services (hard boundary). The snapshot is a value copy taken at publish time, not a live reference into Partidas.

### 4.1 Shared primitives (Value Objects / enums)

- **Value Objects:** `SesionPartidaId{Valor}`, `InscripcionId{Valor}` (app-assigned `Guid`, mirror Partidas VO factory style); `ConfiguracionSnapshot`; `JuegoResumen`.
- **`ConfiguracionSnapshot`** (immutable, captured at publish): `Nombre`, `Modalidad`, `ModoInicioPartida`, `TiempoInicio?`, `MinimosParticipacion`, `MaximosParticipacion`, `IReadOnlyList<JuegoResumen> Juegos`. **Partida-level fields + game references only — no game content** (questions/options/stages). Later runtime slices extend the snapshot or re-fetch (YAGNI in 3a).
- **`JuegoResumen`:** `JuegoId`, `Orden`, `TipoJuego`. Enough to validate publishability (≥1 game, contiguous `Orden`).
- **Enums:** `EstadoSesion{Lobby, Iniciada, Cancelada, Terminada}`, `EstadoInscripcion{Activa, Cancelada}`, plus own copies of `Modalidad`, `ModoInicioPartida`, `TipoJuego`.

### 4.2 Aggregate `SesionPartida` (root)

Fields: `SesionPartidaId Id`, `Guid PartidaId` (reference to the Partidas config partida), `EstadoSesion Estado`, `ConfiguracionSnapshot Config`, `IReadOnlyList<InscripcionPartida> Inscripciones`.

- **`Publicar(Guid partidaId, ConfiguracionSnapshot snapshot)` (factory):** validates **publishability** on the snapshot — at least one `JuegoResumen` and `Orden` a contiguous sequence from 1 (the runtime use of SP-2's `ValidarIntegridadJuegos` semantics); empty/non-contiguous → `PartidaNoPublicable`. Sets `Estado = Lobby`. (Partidas does **not** enforce this at config time today — the M-1 finding — so Operaciones enforces it at publish.)
- **`Inscribir(Guid participanteId, bool tieneParticipacionActivaEnOtra, int inscritosActivos, DateTime fecha)`:** guards (in order) `Estado == Lobby` → `SesionNoEnLobby`; `Config.Modalidad == Individual` → `ModalidadNoSoportada`; not already active here → `ParticipanteYaInscrito`; `!tieneParticipacionActivaEnOtra` → `ParticipacionActivaExistente`; `inscritosActivos < Config.MaximosParticipacion` → `CupoLleno`. On success adds an `InscripcionPartida(Activa)`. (Cross-session "active elsewhere" and the live active count are read by the handler and passed in — the aggregate stays pure.)
- **`CancelarInscripcion(Guid participanteId)`:** marks the participant's active inscription `Cancelada` (frees capacity + the active-participation slot); absent/already-cancelled → `ParticipanteYaInscrito`-inverse (`InscripcionNoEncontrada`).
- **No `Iniciar`/activation methods in 3a** — start is SP-3b. Only `Lobby` is reachable.

### 4.3 Entity `InscripcionPartida` (child of `SesionPartida`)

Fields: `InscripcionId Id`, `Guid ParticipanteId`, `EstadoInscripcion Estado`, `DateTime FechaInscripcion`. Individual only in 3a (one participant per inscription). `Convocatoria` and team inscription arrive in 3a-E.

### 4.4 R1 SEAM — runtime estado ownership (confirmed; mini-ADR §13)

SP-2 left `Partida.EstadoPartida` nullable (`null` = configured, not published) with a note "SP-3 sets Lobby." Operaciones **cannot write Partidas' DB** (hard boundary), and publish/runtime is **not** Partidas' concern. **Decision: the published lifecycle lives in `SesionPartida.EstadoSesion` inside Operaciones; Partidas' `EstadoPartida` is never advanced and stays `null`.** Rejected alternatives: (a) Partidas exposing a publish command to flip its own estado (re-homes runtime into config-only Partidas); (b) an event that makes Partidas update its estado (needs the deferred backbone + duplicates estado in two services). R1 keeps estado single-sourced where the runtime lives.

### 4.5 Config handoff port (Option A)

- **`IConfiguracionPartidaClient.ObtenerConfiguracionAsync(Guid partidaId, CancellationToken) → ConfiguracionPartidaDto?`** (Application port). Returns the snapshot data, or `null` when Partidas answers 404.
- **`PartidasConfigHttpClient : IConfiguracionPartidaClient`** (Infrastructure/Services): typed `HttpClient`, base URL from config `PartidasApi:BaseUrl` (local `http://localhost:5010`). **Backend↔backend direct call, not through the gateway.** Forwards the caller's bearer JWT so Partidas' own authorization still applies.
- **Error distinction (robustness):** Partidas `404` → return `null` → handler throws `PartidaConfigNoEncontrada` (→404). Network failure / timeout / non-success non-404 (5xx) → `PartidasConfigInaccesible` (→502). Without this split, a down Partidas would masquerade as "partida does not exist."

### 4.6 Events seam (No-Op)

- **`ISesionEventsPublisher.PublicarPartidaPublicadaEnLobbyAsync(PartidaPublicadaEnLobbyEvent, CancellationToken)`** (Application port).
- **`NoOpSesionEventsPublisher : ISesionEventsPublisher`** (Infrastructure/Services) — `Task.CompletedTask`, mirroring `NoOpEquipoEventsPublisher`. Real adapter deferred to the backbone slice. The publish handler calls the port after a successful `SaveChanges` so the seam is exercised end-to-end.

## 5. Application layer (CQRS / MediatR, graded folders)

Graded `Application/` folders (identical to Partidas/identity): `Commands/`, `Queries/`, `Interfaces/`, `Validators/`, `DTOs/`, `Handlers/`, `Handlers/Commands/`, `Handlers/Queries/`, `Exceptions/`. Root files `ValidationBehavior.cs` + `DependencyInjection.cs` copied from Partidas (M-2 pattern: validation runs in the MediatR pipeline, not the controller).

- **Commands:** `PublicarPartidaCommand(Guid PartidaId, string? BearerToken)`, `InscribirParticipanteCommand(Guid PartidaId, Guid ParticipanteId)`, `CancelarInscripcionCommand(Guid PartidaId, Guid ParticipanteId)`.
- **Handlers/Commands:** `PublicarPartidaCommandHandler` (existence check → snapshot via port → `SesionPartida.Publicar` → save → publish event), `InscribirParticipanteCommandHandler` (loads session, reads active-elsewhere + active-count from repo, calls `Inscribir`, saves), `CancelarInscripcionCommandHandler`.
- **Queries:** `ObtenerLobbyQuery(Guid PartidaId)`. **Handlers/Queries:** `ObtenerLobbyQueryHandler` → `LobbyDto`.
- **Validators (FluentValidation):** one per command — non-empty `PartidaId`/`ParticipanteId`. (Business-rule violations are domain exceptions, not validation.)
- **DTOs:** `ConfiguracionPartidaDto` (port response), `PublicarPartidaResponse`, `InscripcionResponse`, `LobbyDto` (estado, modalidad, min/max, inscritos activos + count).
- **Interfaces:** `IConfiguracionPartidaClient`, `ISesionEventsPublisher`. Repository interfaces live in **Domain/** (graded rule).
- **Clock:** `InscribirParticipanteCommandHandler` stamps `FechaInscripcion` via an injected `TimeProvider` (.NET 8 built-in) — never `DateTime.UtcNow` inline — so the timestamp is deterministic in tests. The domain `Inscribir(...)` receives the timestamp as a parameter (stays pure).
- **Exceptions:** application-layer exceptions if any; domain exceptions mapped centrally by Api middleware.

Commands mutate; queries never mutate. Controllers dispatch via `_mediator.Send(...)` only.

## 6. Persistence (`umbral_operaciones_sesion`, EF Core + Npgsql)

- **One repository** `ISesionPartidaRepository` (interface in `Domain/Abstractions/Persistence/`), implementation in `Infrastructure/Persistence/`. Methods: `GetByPartidaIdAsync`, `ExistsForPartidaAsync`, `Add`, `ParticipanteTieneParticipacionActivaAsync(participanteId, exceptPartidaId)` (queries Operaciones' own DB across sessions in `Lobby`/`Iniciada` — same service, allowed), `ContarInscritosActivosAsync(partidaId)`. `IUnitOfWork` for `SaveChanges`.
- **Tables:** `SesionesPartida` (+ owned `ConfiguracionSnapshot`, with `JuegoResumen` rows e.g. `SesionJuegos`), `InscripcionesPartida`. VOs mapped as owned/column conversions. `EstadoSesion`/`EstadoInscripcion`/`Modalidad` persisted as strings (mirror SP-2 `JsonStringEnumConverter`/EF enum-to-string).
- **`OperacionesSesionDbContext`** (scaffolded in the shell) gains the entity configurations. **Initial EF migration** created.
- **InMemory fallback** for tests, same registration switch as the SP-0 shell / SP-2, so application/contract tests run without Postgres.

## 7. Api layer (graded)

- `Api/Controllers/SesionesController` (native `ControllerBase`, MediatR dispatch, **no** business logic). Reads `ParticipanteId` from the JWT `sub` claim and the bearer token from the request for the publish forward; everything else is `_mediator.Send`.
- Endpoints (gateway `/operaciones-sesion/*`):

  | Capability | Verb | Route | Actor | Success |
  |---|---|---|---|---|
  | Publish → Lobby | POST | `/partidas/{id}/publicacion` | Operador | 201 + `LobbyDto` |
  | Inscribe (Individual) | POST | `/partidas/{id}/inscripciones` | Participante | 201 + `InscripcionResponse` |
  | Cancel own inscription | DELETE | `/partidas/{id}/inscripciones/mia` | Participante | 204 |
  | Lobby state | GET | `/partidas/{id}/lobby` | Operador/Participante | 200 / 404 |

- `Program.cs` registers controllers via `MapControllers` only (no minimal-API), registers the typed `HttpClient`, the InMemory/Npgsql switch, MediatR + `ValidationBehavior`, and binds `ISesionEventsPublisher → NoOpSesionEventsPublisher`.
- Centralized `ExceptionHandlingMiddleware` maps exceptions → status + `{message}` (§8).
- **Every controller has unit tests** (graded, required).

## 8. Error handling

Central `ExceptionHandlingMiddleware` (ported from Partidas). No try/catch in controllers or handlers.

| Exception | Status |
|---|---|
| `ValidationException` (FluentValidation via `ValidationBehavior`) | 400 |
| `PartidaConfigNoEncontrada` | 404 |
| `PartidasConfigInaccesible` | 502 |
| `SesionYaPublicada` | 409 |
| `PartidaNoPublicable` | 409 |
| `SesionNoEnLobby` | 409 |
| `ParticipanteYaInscrito` / `InscripcionNoEncontrada` | 409 / 404 |
| `ParticipacionActivaExistente` | 409 |
| `CupoLleno` | 409 |
| `ModalidadNoSoportada` | 409 |

## 9. Contracts

- `contracts/http/operaciones-sesion-api.md` — register the 4 endpoints (today index-only): verbs/routes/`:guid`, request/response shapes, status codes. Authored as source-of-truth, asserted by contract tests.
- `contracts/events/operaciones-sesion-events.md` — register the **`PartidaPublicadaEnLobby`** payload (today "payload not registered"): `PartidaId`, `SesionPartidaId`, `Modalidad`, `MinimosParticipacion`, `MaximosParticipacion`, occurred-at. Exchange/queue/routing-key/idempotency remain "defined by the backbone slice" — 3a registers the **payload shape only**, emitted through the No-Op port.

## 10. Testing

- **Unit (Domain):** `SesionPartida.Publicar` (≥1 contiguous game → Lobby; empty/non-contiguous → `PartidaNoPublicable`). `Inscribir` invariants — not-in-Lobby, duplicate, capacity, `Equipo` → respective throws; happy path adds `Activa`. `CancelarInscripcion` frees capacity + slot. `InscripcionPartida` transitions. VO validation.
- **Application (handlers, with fakes):** `PublicarPartidaCommandHandler` — fake client snapshot → saved in Lobby + publisher called; client `null` → `PartidaConfigNoEncontrada`; existing session → `SesionYaPublicada`; empty config → `PartidaNoPublicable`. `InscribirParticipanteCommandHandler` — happy + each invariant incl. cross-session active participation (fake repo). `CancelarInscripcionCommandHandler`, `ObtenerLobbyQueryHandler`. Fakes: `FakeSesionPartidaRepository` (store + SaveCount + active-participation/active-count stubs), `FakeConfiguracionPartidaClient`, `FakeSesionEventsPublisher` (records calls).
- **Validators (unit):** each `*CommandValidator`.
- **Controller unit tests (graded, required):** `SesionesController` — each endpoint dispatches the right command/query via mocked `IMediator`, returns the right status (201/204/200), reads `sub`/bearer correctly. Pure dispatch (M-2 lesson — no business logic in the controller).
- **`ValidationBehavior` (unit):** the 4-test pattern (invalid → `ValidationException`, valid → `next`).
- **Middleware (unit):** representative exception → status mapping.
- **Contract (`WebApplicationFactory<Program>`, InMemory + stub `IConfiguracionPartidaClient`):** publish → lobby → inscribe → query end-to-end; duplicate inscribe → 409; capacity → 409; publish unknown partida (stub `null`) → 404; inscribe not-in-Lobby → 409; `Equipo` snapshot inscribe → 409. The stub config client avoids standing up Partidas.
- Whole Operaciones suite green on InMemory (no Postgres needed), following SP-0/SP-2.

## 11. R1 structural gate

Before close: `Api/Controllers/` present, no minimal-API routes (only `MapControllers`); exact graded `Application/` folder set; `Infrastructure/` has `Persistence/` + `Services/`; repository interface in `Domain/`; exception middleware registered; controller unit tests present; full suite green. Mirrors the SP-0/SP-1R/SP-2 gate.

## 12. Micro-decisions (recorded, low-risk)

- **No inscription event in 3a.** The event registry lists only `PartidaPublicadaEnLobby` (and downstream runtime events). Inscriptions emit **no** event in 3a; revisited when the backbone lands (and Puntuaciones needs them). Alternative considered: register `InscripcionRegistrada` now — deferred to avoid speculative contract surface.
- **JWT forwarding on the internal call.** Operaciones forwards the caller's bearer token to Partidas' `GET /partidas/{id}` rather than using a service principal — keeps Partidas' own authz authoritative and avoids introducing a service-credential before SP-5.
- **Authentication wired in SP-3a; functional-permission enforcement deferred to SP-5.** JWT bearer validation (Keycloak authority, audiences/issuers via env vars) is now wired in `Program.cs`, mirroring the identity-service pattern; `UseAuthentication()` + `UseAuthorization()` run in the pipeline so `User`/`sub` are populated from a valid token in production. Only fine-grained functional-permission authorization (`GestionarPartidas` for publish, `ParticiparEnPartidas` for inscribe) and gateway coarse-role policy remain deferred to SP-5 (no `[Authorize]` attributes are added in SP-3a).

## 13. Mini-ADR (R1) — to author in `docs/05-decisions/`

A short ADR fixing: **runtime `EstadoPartida` is materialized as `SesionPartida.EstadoSesion` in Operaciones de Sesión; Partidas' `EstadoPartida` remains `null` (config-only, never advanced).** Context = the SP-2 nullable SEAM + the hard cross-service write boundary; decision = R1; consequences = estado is single-sourced in Operaciones, Partidas exposes no publish/runtime mutation, the eventual backbone does not duplicate estado. Numbered after the latest accepted ADR.

## 14. Risks / open points

- **Owned-VO + owned-collection EF mapping** (`ConfiguracionSnapshot` with `JuegoResumen` rows as an owned collection) — confirm mapping strategy during the persistence task; lean app-assigned `Guid` VO factories for testability (SP-2 precedent).
- **"One active participation" query correctness** — the cross-session check spans `Lobby` + `Iniciada`; in 3a only `Lobby` is reachable, but model the query for both so SP-3b needs no change. Cover with a fake-repo handler test.
- **Internal HTTP coupling** — Option A makes publish depend on Partidas being up. The `PartidasConfigInaccesible` → 502 split makes the failure honest; the backbone slice later offers an async alternative if desired (not required for 3a).
- **`EstadoPartida`-stays-null** may surprise a reader expecting SP-2's "SP-3 sets Lobby." The mini-ADR (§13) is the durable record; cross-reference it from the SP-2 SEAM note if confusion arises.
