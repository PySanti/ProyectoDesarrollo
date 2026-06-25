# SP-2 — `Partida`/`Juego` model + Partidas (config) — Design

- **Date:** 2026-06-24
- **Branch:** `feature/code-migration-SP-2`
- **Slice:** SP-2 of the four-service code-structure migration (`docs/superpowers/specs/2026-06-22-code-structure-doctrine-migration-design.md`)
- **Depends on:** SP-0 (foundation: YARP gateway + `partidas`/`operaciones-sesion`/`puntuaciones` graded shells + DBs + `/health`), merged. SP-1R (Identity graded conformance), merged.
- **Service:** Partidas (`services/partidas`, namespace `Umbral.Partidas.*`, DB `umbral_partidas`).
- **Canonical structure reference:** `services/identity` (graded-compliant after SP-1R).

## 1. Goal

Introduce the doctrinal `Partida` → `Juego` domain into the **Partidas** service and re-home **Trivia question configuration** and **BDT stage configuration** (with per-stage `PuntajeAsignado`) there. Deliver the **configuration** capability only: create a `Partida` header, then incrementally add fully-formed games with their content; review what was configured via queries. No live session, no scoring.

This is a **FRESH-BUILD** from the project doctrine (`docs/01-project-source/diagrama-de-clases.md`, `modelo-de-dominio.md`), **not** a port of the old `trivia-game-service` / `bdt-game-service` aggregates. The old services keep working (runtime/scoring) until SP-3/SP-4 and are retired in SP-5.

## 2. Locked decisions (from brainstorming — not re-litigated here)

1. **FRESH-BUILD** from doctrine. The old `PartidaTrivia` (trivia-game) and `PartidaBdt` (bdt-game) aggregates — where one "game" *was* the whole partida — are **not** reused. The doctrine splits `Partida` (root) from `Juego` (specialized child). Old layout is dropped at SP-5 close.
2. **API INCREMENTAL** — `CrearPartida` creates the header; `AgregarJuego` adds **one atomic game with all its content** per call (a Trivia game arrives with its full question set; a BDT game with its full stage set).
3. **Old services ADDITIVE + deprecation** — `trivia-game-service` / `bdt-game-service` are untouched functionally; mark superseded config surfaces `[Obsolete]` for SP-5 retirement. Do not break them (runtime used by SP-3, scoring by SP-4).
4. **MULTI-AGGREGATE per the class diagram** — three aggregate roots: `Partida` (root, holds ordered **references** to its games), `JuegoTrivia` (root, owns its `Pregunta`s), `JuegoBDT` (root, owns its `EtapaBDT`s). **Three repositories.**

## 3. Scope

**IN:**
- `Partida` → `Juego` domain model + Trivia/BDT configuration, three aggregates.
- Incremental create/config commands (`CrearPartida`, `AgregarJuegoTrivia`, `AgregarJuegoBDT`).
- Review queries (`GetPartidaById`, `ListPartidas`).
- Persistence in `umbral_partidas` (EF Core + Npgsql), tables per aggregate, initial migration, InMemory fallback for tests (SP-0 pattern).
- HTTP contract `contracts/http/partidas-config.md`; internal domain events only.
- Tests: domain-invariant unit, application (handlers/validators on InMemory), contract, **controller unit tests** (graded).
- R1 structural gate + green suite.

**OUT (later slices):**
- Publish → `Lobby`, inscriptions, convocatorias, lobby, start, runtime sync (**SP-3**, Operaciones de Sesión).
- Scoring, native rankings, consolidated ranking, audit/history projection (**SP-4**, Puntuaciones).
- Client (web/mobile) repoint and old-service retirement (**SP-5**).

## 4. Domain model (authoritative names from `diagrama-de-clases.md`)

> Grounding correction vs the brainstorm sketch (faithful refinements, not scope changes):
> - Trivia question text field is **`Texto`** (not "enunciado").
> - BDT stage fields are **`CodigoQREsperado`** + **`PuntajeAsignado`** (VO), not "textoQREsperado"/"Puntaje".
> - **`AreaBusqueda`** belongs to **`JuegoBDT`** (descriptive text), **not** to `EtapaBDT`.
> - `PuntajeAsignado` is one shared Value Object (`Valor`, `EsValido()`) reused by `Pregunta` and `EtapaBDT`.

### 4.1 Shared Partidas-context primitives (defined here, referenced by all aggregates)

- **Value Objects:** `PartidaId{Valor}`, `JuegoId{Valor}`, `NombrePartida{Valor}` (non-empty, length-bounded), `PuntajeAsignado{Valor}` (`EsValido()` → positive).
- **Enums:** `Modalidad{Individual, Equipo}`, `ModoInicioPartida{Manual, Automatico, ManualYAutomatico}`, `TipoJuego{Trivia, BusquedaDelTesoro}`, `EstadoJuego{Pendiente, Activo, Finalizado}`, `EstadoPartida{Lobby, Iniciada, Cancelada, Terminada}`.

### 4.2 Aggregate `Partida` (root)

Fields (SP-2 subset of the diagram): `PartidaId`, `NombrePartida`, `EstadoPartida?`, `Modalidad`, `ModoInicioPartida`, `TiempoInicio?`, `MinimosParticipacion`, `MaximosParticipacion`, ordered **game references** (`JuegoId` + `Orden` + `TipoJuego`).

- **`MinimosParticipacion`/`MaximosParticipacion`** are partida-level (CLAUDE.md domain essentials; back the diagram's `ValidarMinimosParticipacion()`). They are *configured* here; *enforced* at start in SP-3.
- **SP-2 methods only:** `Crear(...)`, `AgregarJuego(juegoId, orden, tipoJuego)` (registers the ordered reference; rejects duplicate `Orden` and duplicate `JuegoId`), `ValidarListaParaPublicar()` (invariant: at least one game; `Orden` is a contiguous sequence with no gaps/dups). Diagram runtime methods (`PublicarPartida`, `IniciarPartida`, `ActivarSiguienteJuego`, `CalcularRankingConsolidado`, …) are **NOT** implemented in SP-2.
- `RankingConsolidado` (diagram VO) is **out** — computed by Puntuaciones (SP-4).

### 4.3 SEAM — `EstadoPartida` before publish (confirmed)

The doctrine `EstadoPartida` enum has no pre-`Lobby` value, but **publish (→`Lobby`) is SP-3's responsibility**. A partida that is created/configured but not yet published is therefore not in any enum state. **Decision: model `EstadoPartida` as nullable; `null` = "configured / not yet published".** SP-3's `PublicarPartida()` transitions `null → Lobby`. This keeps the enum doctrinal (no synthetic `Borrador` value) while letting SP-2 persist unpublished partidas. Persistence: `EstadoPartida` column nullable.

### 4.4 Aggregate `JuegoTrivia` (root, specialization of `Juego`)

Fields (SP-2 subset): `JuegoId`, `PartidaId`, `Orden`, `EstadoJuego` (defaults `Pendiente`), `Preguntas` (1..\*). Runtime fields (`Participantes`, `Respuestas`, `PreguntaActualId`) are **out** (SP-3).

- **SP-2 methods:** `Crear(juegoId, partidaId, orden, preguntas)`, `AgregarPregunta(...)`. Invariant: at least one `Pregunta`.
- **`Pregunta`** (child): `PreguntaId`, `Texto`, `Opciones` (≥2, exactly one marked correct), `PuntajeAsignado`, `TiempoLimite`. Methods used at config time: `AgregarOpcion`, `DefinirRespuestaCorrecta`, `EsValida`. Invariants: non-empty `Texto`; ≥2 options; exactly one correct; valid `PuntajeAsignado`; positive `TiempoLimite`.
- **`Opcion`** (child of `Pregunta`): `OpcionId`, `Texto`, `EsCorrecta`.
- **No question bank / no reuse** — questions are created with the game (the diagram explicitly eliminates the `FormularioTrivia` aggregate; its content moves into `JuegoTrivia`).

### 4.5 Aggregate `JuegoBDT` (root, specialization of `Juego`)

Fields (SP-2 subset): `JuegoId`, `PartidaId`, `Orden`, `EstadoJuego` (defaults `Pendiente`), **`AreaBusqueda`** (descriptive text), `Etapas` (1..\*). Runtime fields (`Participantes`, `IndiceEtapaActual`, tesoros, pistas) are **out** (SP-3).

- **SP-2 methods:** `Crear(juegoId, partidaId, orden, areaBusqueda, etapas)`, `AgregarEtapa(...)`. Invariant: at least one `EtapaBDT`.
- **`EtapaBDT`** (child): `EtapaId`, `Orden`, `CodigoQREsperado` (expected QR **text**), `PuntajeAsignado`, `TiempoLimite`. Runtime fields (`EstadoEtapa`, `GanadorId`, `TiempoResolucion`) are **out** (SP-3). Invariants: non-empty `CodigoQREsperado`; valid `PuntajeAsignado`; positive `TiempoLimite`; contiguous `Orden`.

### 4.6 Cross-aggregate consistency on `AgregarJuego`

`AgregarJuegoTrivia` / `AgregarJuegoBDT` mutate **two** aggregates: they create the new `JuegoTrivia`/`JuegoBDT` root **and** register its ordered reference on `Partida`. Both live in the **same service and DB** (`umbral_partidas`), so the handler commits both repos inside **one `SaveChanges`/transaction** on the shared `PartidasDbContext`. This is a deliberate intra-service transaction across aggregate roots, acceptable because there is no cross-service boundary here. The `Orden` uniqueness/contiguity invariant is owned by `Partida` (single source of truth for ordering).

## 5. Application layer (CQRS / MediatR, graded folders)

Graded `Application/` folders (per CLAUDE.md, identical to identity-service): `Commands/`, `Queries/`, `Interfaces/`, `Validators/`, `DTOs/`, `Handlers/`, `Handlers/Commands/`, `Handlers/Queries/`, `Exceptions/`.

- **Commands:** `CrearPartidaCommand`, `AgregarJuegoTriviaCommand`, `AgregarJuegoBDTCommand`.
- **Handlers/Commands:** `CrearPartidaCommandHandler`, `AgregarJuegoTriviaCommandHandler`, `AgregarJuegoBDTCommandHandler`.
- **Queries:** `GetPartidaByIdQuery`, `ListPartidasQuery`. **Handlers/Queries:** matching `*QueryHandler`.
- **Validators (FluentValidation):** one per command. `CrearPartida` — non-empty name, valid enums, `Maximos ≥ Minimos ≥ 1`, `TiempoInicio` required iff `ModoInicioPartida ∈ {Automatico, ManualYAutomatico}`. `AgregarJuegoTrivia` — ≥1 question, each with ≥2 options/exactly one correct/positive puntaje+tiempo. `AgregarJuegoBDT` — non-empty `AreaBusqueda`, ≥1 stage, each with non-empty `CodigoQREsperado`/positive puntaje+tiempo.
- **DTOs:** request/response records (`CrearPartidaRequest/Response`, `AgregarJuego*Request/Response`, `PartidaDetailDto`, `PartidaSummaryDto`).
- **Interfaces:** application-layer ports if any (e.g. clock for `TiempoInicio` validation). Repository interfaces live in **Domain/** (graded rule).
- **Exceptions:** application-layer exceptions mapped centrally by the Api middleware.

Commands mutate; queries never mutate. Controllers dispatch via `_mediator.Send(...)` only.

## 6. Persistence (`umbral_partidas`, EF Core + Npgsql)

- **Three repositories**, interfaces in `Domain/` (e.g. `IPartidaRepository`, `IJuegoTriviaRepository`, `IJuegoBDTRepository`), implementations in `Infrastructure/Persistence/`.
- **Tables per aggregate:** `Partidas` (+ ordered game-reference rows, e.g. `PartidaJuegos`); `JuegosTrivia` + `Preguntas` + `Opciones`; `JuegosBDT` + `EtapasBDT`. VOs mapped as owned/column conversions (`PartidaId`, `JuegoId`, `NombrePartida`, `PuntajeAsignado`). `EstadoPartida` nullable column.
- **`PartidasDbContext`** (already scaffolded in the shell) gains the entity configurations. **Initial EF migration** created.
- **InMemory fallback** for tests, mirroring the SP-0 pattern (same registration switch used by the shell), so application/contract tests run without Postgres.

## 7. Api layer (graded)

- Controllers under `Api/Controllers/`, inheriting native `ControllerBase`, dispatching via MediatR; **no** business logic. Likely `PartidasController` (create partida, add trivia game, add bdt game, get-by-id, list) — split if it grows.
- `Program.cs` registers controllers via `MapControllers` only (no inline endpoints), consistent with SP-0/SP-1R.
- Centralized `ExceptionHandlingMiddleware` (already in the shell) maps application exceptions to status codes + `{message}` body.
- **Every controller has unit tests** (graded, required).

## 8. Contracts

- `contracts/http/partidas-config.md` — the config endpoints (create partida → add trivia/bdt game → get/list), request/response shapes, status codes. Authored as source-of-truth and asserted by the contract tests.
- **No integration (RabbitMQ) events** in SP-2 — publishing, inscriptions, scoring all live in SP-3/SP-4. Internal domain events only (raised, not yet bridged to a broker).

## 9. Testing

- **Unit (Domain):** aggregate invariants — `Partida` order contiguity/no-dup, `ValidarListaParaPublicar`, `JuegoTrivia` ≥1 question + exactly-one-correct-option, `JuegoBDT` ≥1 stage, VO validation (`NombrePartida`, `PuntajeAsignado`).
- **Application:** command handlers + validators against InMemory repos (incl. the two-aggregate `AgregarJuego` transaction path and `TiempoInicio`-vs-`ModoInicio` rule).
- **Contract:** assert `partidas-config.md` shapes/status codes.
- **Controller unit tests:** dispatch + status mapping (graded, required).
- Whole Partidas suite green on InMemory (no Postgres needed for CI), following SP-0.

## 10. R1 structural gate (per the migration design's appended checklist)

Before close: `Api/Controllers/` present, no minimal-API routes (only `MapControllers`); exact graded `Application/` folder set; `Infrastructure/` has `Persistence/` + `Services/`; repository interfaces in `Domain/`; exception middleware registered; controller unit tests present; full suite green. Mirrors the SP-0/SP-1R gate.

## 11. Inherited SP-0 minors — disposition for this slice

- **A. `ExceptionHandlingMiddleware` serializes `ex.Message`** across all four services. **Defer.** Whole-fleet hardening is its own slice and carries no correctness risk in a config-only entity slice; SP-2 keeps parity with the canonical identity-service middleware.
- **B. Gateway `/partidas` = `RequireRole("Operador")` 403s an Administrador token.** **Note-only in SP-2.** SP-2 introduces real partida-config routes, but no client traffic reaches them until SP-5 (client repoint). Gateway coarse authz is config-first (trivially changeable in `appsettings`). Record the open question — "should Administrador read-only partida views (web) reach config GETs through the gateway?" — for the SP-5 client-routing decision; do not change gateway policy in SP-2.

## 12. Risks / open points

- **Owned-VO mapping in EF** (`PartidaId`/`JuegoId` as keys) — confirm key generation strategy (app-assigned `Guid` VOs vs DB-generated) during design of the persistence task; lean app-assigned `Guid` inside the VO factory for testability.
- **Three-aggregate boundary vs the diagram's containment** — the diagram shows `Partida.Juegos` as containment; the multi-aggregate decision realizes this as Partida-held **references** + separate game roots. The `AgregarJuego` handler is the seam (section 4.6); keep ordering invariants on `Partida`.
- **`AreaBusqueda` placement** corrected to `JuegoBDT` — verify no validator/DTO still attaches it to the stage.
