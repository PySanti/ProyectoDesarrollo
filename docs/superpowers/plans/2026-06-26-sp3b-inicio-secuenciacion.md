# SP-3b — Partida start + sequential game lifecycle Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bring the published `SesionPartida` to life — manual + automatic (time-gated, idempotent) start with minimums-enforcement / auto-cancellation, sequential game activation, and advance through `Terminada` — all as transient session state in Operaciones de Sesión, emitting domain events through the existing No-Op port.

**Architecture:** Extend the SP-3a `SesionPartida` aggregate with `Iniciar` / `IntentarInicioAutomatico` / `FinalizarJuegoActual` (returning small `Resultado*` records the handlers use to publish the right events), add `EstadoJuego` to `JuegoResumen`, expose 3 commands + 1 query through `SesionesController`, and persist the new fields via an EF migration. No gameplay (3c/3d), no scheduler (3f), no real broker (backbone slice).

**Tech Stack:** .NET 8, Clean Architecture + CQRS via MediatR 12.2.0, FluentValidation 11.x, EF Core 8 (Npgsql + InMemory test fallback), xUnit, `WebApplicationFactory<Program>` contract tests.

## Global Constraints

- **Service:** `services/operaciones-sesion`, namespaces `Umbral.OperacionesSesion.*`, DB `umbral_operaciones_sesion`. Solution: `services/operaciones-sesion/Umbral.OperacionesSesion.sln`.
- **Graded `Application/` folders (exact set, no per-feature slices):** `Commands/`, `Queries/`, `Interfaces/`, `Validators/`, `DTOs/`, `Handlers/`, `Handlers/Commands/`, `Handlers/Queries/`, `Exceptions/` + root `DependencyInjection.cs` / `ValidationBehavior.cs`. Do not add new top-level folders.
- **Controllers:** native `ControllerBase`, MediatR dispatch only (`_mediator.Send(...)`), zero business logic. Every controller has unit tests.
- **Repository interfaces** live in `Domain/Abstractions/Persistence/`; **implementations** in `Infrastructure/Persistence/`. `Infrastructure/` has only `Persistence/` + `Services/`. Domain never depends on infrastructure.
- **R1 / ADR-0010:** runtime estado lives in `SesionPartida.EstadoSesion` inside Operaciones. **Never read/write Partidas' DB.** Partidas' `EstadoPartida` stays `null` forever.
- **Events:** through `ISesionEventsPublisher` → `NoOpSesionEventsPublisher` (No-Op until the RabbitMQ backbone slice). Publish **after** `SaveChanges`.
- **Clock:** inject `TimeProvider` (already registered as `TimeProvider.System`); never `DateTime.UtcNow` inline. Domain methods receive `now` as a parameter (stay pure).
- **Auth:** JWT identity validation is already wired (SP-3a). No `[Authorize]` attributes — functional-permission authz + gateway coarse-role deferred to SP-5.
- **HTTP:** start/advance return **200** (state transition), not 201. Enums serialized as strings (`JsonStringEnumConverter`, already configured); persisted as `int`.
- **Centralized** `ExceptionHandlingMiddleware`; no try/catch in controllers or handlers.
- **Tests run on InMemory** (no Postgres needed). Full suite green before close (R1 gate). Baseline at plan start: **74/74** (59 unit + 3 integration + 12 contract).
- Leave `GUIA-USO-AGENTE.md` (pre-existing unstaged modification) untouched.

---

### Task 1: `EstadoJuego` enum + `JuegoResumen` per-game state

**Files:**
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Enums/EstadoJuego.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/JuegoResumen.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/SesionPartidaTests.cs`

**Interfaces:**
- Consumes: existing `JuegoResumen(Guid, int, TipoJuego)` ctor; `SesionPartida.Publicar`.
- Produces: `EstadoJuego{Pendiente, Activo, Finalizado}`; `JuegoResumen.Estado` (public get); `internal void Activar()` / `internal void Finalizar()` (callable only from the Domain assembly, i.e. the `SesionPartida` aggregate). New `JuegoResumen` instances default to `Pendiente`.

- [ ] **Step 1: Write the failing test** — append to `SesionPartidaTests.cs` (inside the class):

```csharp
    [Fact]
    public void Publicar_creates_all_games_in_pendiente()
    {
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), Snapshot(juegos: 3));
        Assert.All(sesion.Juegos, j => Assert.Equal(EstadoJuego.Pendiente, j.Estado));
    }
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj --filter "FullyQualifiedName~Publicar_creates_all_games_in_pendiente"`
Expected: FAIL — compile error, `EstadoJuego` / `JuegoResumen.Estado` do not exist.

- [ ] **Step 3: Create the enum** — `EstadoJuego.cs`:

```csharp
namespace Umbral.OperacionesSesion.Domain.Enums;

public enum EstadoJuego { Pendiente, Activo, Finalizado }
```

- [ ] **Step 4: Extend `JuegoResumen`** — replace the file body with:

```csharp
using Umbral.OperacionesSesion.Domain.Enums;

namespace Umbral.OperacionesSesion.Domain.Entities;

public sealed class JuegoResumen
{
    public Guid JuegoId { get; private set; }
    public int Orden { get; private set; }
    public TipoJuego TipoJuego { get; private set; }
    public EstadoJuego Estado { get; private set; } = EstadoJuego.Pendiente;

    private JuegoResumen() { } // EF

    public JuegoResumen(Guid juegoId, int orden, TipoJuego tipoJuego)
    {
        JuegoId = juegoId;
        Orden = orden;
        TipoJuego = tipoJuego;
    }

    internal void Activar()
    {
        if (Estado != EstadoJuego.Pendiente)
            throw new InvalidOperationException($"El juego {JuegoId} no está pendiente.");
        Estado = EstadoJuego.Activo;
    }

    internal void Finalizar()
    {
        if (Estado != EstadoJuego.Activo)
            throw new InvalidOperationException($"El juego {JuegoId} no está activo.");
        Estado = EstadoJuego.Finalizado;
    }
}
```

(The `Activar`/`Finalizar` guards are exercised transitively by Tasks 2 and 4 through the aggregate.)

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj --filter "FullyQualifiedName~Publicar_creates_all_games_in_pendiente"`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Enums/EstadoJuego.cs services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/JuegoResumen.cs services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/SesionPartidaTests.cs
git commit -m "SP-3b: EstadoJuego enum + estado por-juego en JuegoResumen"
```

---

### Task 2: `SesionPartida.Iniciar` (manual) + `ResultadoInicio` + lifecycle fields + `ModoInicioNoCompatibleException`

**Files:**
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Results/ResultadoInicio.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Exceptions/ModoInicioNoCompatibleException.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/SesionPartida.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/SesionPartidaTests.cs`

**Interfaces:**
- Consumes: `SesionPartida` (fields `Estado`, `ModoInicioPartida`, `MinimosParticipacion`, `_inscripciones`, `_juegos`); `JuegoResumen.Activar()`; `EstadoSesion`; `SesionNoEnLobbyException`.
- Produces:
  - `enum TipoResultadoInicio { Iniciada, Cancelada, NoCorresponde }`
  - `ResultadoInicio` (record) with `Tipo` + `JuegoActivado` (`JuegoResumen?`); factories `ResultadoInicio.Iniciada(JuegoResumen)`, `ResultadoInicio.Cancelada`, `ResultadoInicio.NoCorresponde`.
  - `SesionPartida.Iniciar(DateTime now) → ResultadoInicio`; new fields `DateTime? FechaInicio`, `DateTime? FechaFin`; private `AplicarInicio(DateTime now)`.
  - `ModoInicioNoCompatibleException(Guid partidaId)`.

- [ ] **Step 1: Write the failing tests** — append to `SesionPartidaTests.cs`. (Helper note: the existing `Snapshot(...)` uses `ModoInicioPartida.Manual`; add a local helper for other modes.)

```csharp
    private static ConfiguracionSnapshot SnapshotModo(
        ModoInicioPartida modo, int min = 1, int max = 5, int juegos = 2, DateTime? tiempoInicio = null)
    {
        var lista = Enumerable.Range(1, juegos)
            .Select(o => new JuegoResumen(Guid.NewGuid(), o, TipoJuego.Trivia))
            .ToList();
        return new ConfiguracionSnapshot("Copa", Modalidad.Individual, modo, tiempoInicio, min, max, lista);
    }

    [Fact]
    public void Iniciar_with_minimums_met_starts_and_activates_first_game()
    {
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), Snapshot(min: 1, max: 5, juegos: 2));
        sesion.Inscribir(Guid.NewGuid(), false, 0, T0);

        var resultado = sesion.Iniciar(T0);

        Assert.Equal(TipoResultadoInicio.Iniciada, resultado.Tipo);
        Assert.Equal(EstadoSesion.Iniciada, sesion.Estado);
        Assert.Equal(T0, sesion.FechaInicio);
        Assert.Null(sesion.FechaFin);
        var ordenados = sesion.Juegos.OrderBy(j => j.Orden).ToList();
        Assert.Equal(EstadoJuego.Activo, ordenados[0].Estado);
        Assert.Equal(EstadoJuego.Pendiente, ordenados[1].Estado);
        Assert.Equal(1, resultado.JuegoActivado!.Orden);
    }

    [Fact]
    public void Iniciar_with_minimums_not_met_auto_cancels()
    {
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), Snapshot(min: 2, max: 5, juegos: 1));
        sesion.Inscribir(Guid.NewGuid(), false, 0, T0); // only 1 < 2

        var resultado = sesion.Iniciar(T0);

        Assert.Equal(TipoResultadoInicio.Cancelada, resultado.Tipo);
        Assert.Equal(EstadoSesion.Cancelada, sesion.Estado);
        Assert.Equal(T0, sesion.FechaFin);
        Assert.Null(resultado.JuegoActivado);
        Assert.All(sesion.Juegos, j => Assert.Equal(EstadoJuego.Pendiente, j.Estado));
    }

    [Fact]
    public void Iniciar_when_not_in_lobby_throws()
    {
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), Snapshot());
        sesion.Inscribir(Guid.NewGuid(), false, 0, T0);
        sesion.Iniciar(T0); // now Iniciada

        Assert.Throws<SesionNoEnLobbyException>(() => sesion.Iniciar(T0));
    }

    [Fact]
    public void Iniciar_when_mode_is_automatic_only_throws()
    {
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), SnapshotModo(ModoInicioPartida.Automatico));
        sesion.Inscribir(Guid.NewGuid(), false, 0, T0);

        Assert.Throws<ModoInicioNoCompatibleException>(() => sesion.Iniciar(T0));
    }
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj --filter "FullyQualifiedName~Iniciar_"`
Expected: FAIL — `Iniciar`, `ResultadoInicio`, `TipoResultadoInicio`, `FechaInicio`/`FechaFin`, `ModoInicioNoCompatibleException` do not exist.

- [ ] **Step 3: Create `ResultadoInicio.cs`**

```csharp
using Umbral.OperacionesSesion.Domain.Entities;

namespace Umbral.OperacionesSesion.Domain.Results;

public enum TipoResultadoInicio { Iniciada, Cancelada, NoCorresponde }

public sealed record ResultadoInicio(TipoResultadoInicio Tipo, JuegoResumen? JuegoActivado)
{
    public static ResultadoInicio Iniciada(JuegoResumen juegoActivado) => new(TipoResultadoInicio.Iniciada, juegoActivado);
    public static ResultadoInicio Cancelada { get; } = new(TipoResultadoInicio.Cancelada, null);
    public static ResultadoInicio NoCorresponde { get; } = new(TipoResultadoInicio.NoCorresponde, null);
}
```

- [ ] **Step 4: Create `ModoInicioNoCompatibleException.cs`**

```csharp
namespace Umbral.OperacionesSesion.Domain.Exceptions;

public sealed class ModoInicioNoCompatibleException : Exception
{
    public ModoInicioNoCompatibleException(Guid partidaId)
        : base($"El modo de inicio de la partida {partidaId} no permite esta acción.") { }
}
```

- [ ] **Step 5: Extend `SesionPartida`** — add the `using`, the two fields, and the methods.

Add to the `using` block at the top:
```csharp
using Umbral.OperacionesSesion.Domain.Results;
```

Add the lifecycle fields next to the existing scalar properties (after `MaximosParticipacion`):
```csharp
    public DateTime? FechaInicio { get; private set; }
    public DateTime? FechaFin { get; private set; }
```

Add these methods to the class (e.g. after `CancelarInscripcion`):
```csharp
    public ResultadoInicio Iniciar(DateTime now)
    {
        if (Estado != EstadoSesion.Lobby)
            throw new SesionNoEnLobbyException(PartidaId);
        if (ModoInicioPartida is not (ModoInicioPartida.Manual or ModoInicioPartida.ManualYAutomatico))
            throw new ModoInicioNoCompatibleException(PartidaId);
        return AplicarInicio(now);
    }

    private ResultadoInicio AplicarInicio(DateTime now)
    {
        var inscritosActivos = _inscripciones.Count(i => i.EsActiva);
        if (inscritosActivos < MinimosParticipacion)
        {
            Estado = EstadoSesion.Cancelada;
            FechaFin = now;
            return ResultadoInicio.Cancelada;
        }

        Estado = EstadoSesion.Iniciada;
        FechaInicio = now;
        var primero = _juegos.OrderBy(j => j.Orden).First();
        primero.Activar();
        return ResultadoInicio.Iniciada(primero);
    }
```

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj --filter "FullyQualifiedName~Iniciar_"`
Expected: PASS (4 tests).

- [ ] **Step 7: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Results/ResultadoInicio.cs services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Exceptions/ModoInicioNoCompatibleException.cs services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/SesionPartida.cs services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/SesionPartidaTests.cs
git commit -m "SP-3b: SesionPartida.Iniciar (manual) + auto-cancelación por mínimos + FechaInicio/FechaFin"
```

---

### Task 3: `SesionPartida.IntentarInicioAutomatico` (idempotent, time-gated)

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/SesionPartida.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/SesionPartidaTests.cs`

**Interfaces:**
- Consumes: `AplicarInicio`, `ModoInicioPartida`, `Estado`, `TiempoInicio` (existing nullable field), `ResultadoInicio`.
- Produces: `SesionPartida.IntentarInicioAutomatico(DateTime now) → ResultadoInicio`.

- [ ] **Step 1: Write the failing tests** — append to `SesionPartidaTests.cs`:

```csharp
    private static readonly DateTime TDue = new(2026, 6, 26, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime TBefore = new(2026, 6, 26, 11, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void IntentarInicioAutomatico_when_due_and_minimums_met_starts()
    {
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), SnapshotModo(ModoInicioPartida.Automatico, min: 1, tiempoInicio: TDue));
        sesion.Inscribir(Guid.NewGuid(), false, 0, TBefore);

        var resultado = sesion.IntentarInicioAutomatico(TDue);

        Assert.Equal(TipoResultadoInicio.Iniciada, resultado.Tipo);
        Assert.Equal(EstadoSesion.Iniciada, sesion.Estado);
    }

    [Fact]
    public void IntentarInicioAutomatico_when_due_and_minimums_not_met_auto_cancels()
    {
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), SnapshotModo(ModoInicioPartida.ManualYAutomatico, min: 2, tiempoInicio: TDue));
        sesion.Inscribir(Guid.NewGuid(), false, 0, TBefore); // 1 < 2

        var resultado = sesion.IntentarInicioAutomatico(TDue);

        Assert.Equal(TipoResultadoInicio.Cancelada, resultado.Tipo);
        Assert.Equal(EstadoSesion.Cancelada, sesion.Estado);
    }

    [Fact]
    public void IntentarInicioAutomatico_before_tiempo_inicio_is_noop()
    {
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), SnapshotModo(ModoInicioPartida.Automatico, min: 1, tiempoInicio: TDue));
        sesion.Inscribir(Guid.NewGuid(), false, 0, TBefore);

        var resultado = sesion.IntentarInicioAutomatico(TBefore); // before due

        Assert.Equal(TipoResultadoInicio.NoCorresponde, resultado.Tipo);
        Assert.Equal(EstadoSesion.Lobby, sesion.Estado);
    }

    [Fact]
    public void IntentarInicioAutomatico_when_not_in_lobby_is_idempotent_noop()
    {
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), SnapshotModo(ModoInicioPartida.ManualYAutomatico, min: 1, tiempoInicio: TDue));
        sesion.Inscribir(Guid.NewGuid(), false, 0, TBefore);
        sesion.Iniciar(TDue); // now Iniciada (manual path allowed by ManualYAutomatico)

        var resultado = sesion.IntentarInicioAutomatico(TDue);

        Assert.Equal(TipoResultadoInicio.NoCorresponde, resultado.Tipo);
        Assert.Equal(EstadoSesion.Iniciada, sesion.Estado); // unchanged
    }

    [Fact]
    public void IntentarInicioAutomatico_when_mode_is_manual_only_throws()
    {
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), SnapshotModo(ModoInicioPartida.Manual, min: 1, tiempoInicio: TDue));
        sesion.Inscribir(Guid.NewGuid(), false, 0, TBefore);

        Assert.Throws<ModoInicioNoCompatibleException>(() => sesion.IntentarInicioAutomatico(TDue));
    }
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj --filter "FullyQualifiedName~IntentarInicioAutomatico_"`
Expected: FAIL — `IntentarInicioAutomatico` does not exist.

- [ ] **Step 3: Add the method to `SesionPartida`** (after `Iniciar`):

```csharp
    public ResultadoInicio IntentarInicioAutomatico(DateTime now)
    {
        if (ModoInicioPartida is not (ModoInicioPartida.Automatico or ModoInicioPartida.ManualYAutomatico))
            throw new ModoInicioNoCompatibleException(PartidaId);
        if (Estado != EstadoSesion.Lobby)
            return ResultadoInicio.NoCorresponde;
        if (TiempoInicio is null || now < TiempoInicio.Value)
            return ResultadoInicio.NoCorresponde;
        return AplicarInicio(now);
    }
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj --filter "FullyQualifiedName~IntentarInicioAutomatico_"`
Expected: PASS (5 tests).

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/SesionPartida.cs services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/SesionPartidaTests.cs
git commit -m "SP-3b: SesionPartida.IntentarInicioAutomatico (idempotente, gated por TiempoInicio)"
```

---

### Task 4: `SesionPartida.FinalizarJuegoActual` + `ResultadoAvance` + `SesionNoIniciadaException`

**Files:**
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Results/ResultadoAvance.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Exceptions/SesionNoIniciadaException.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/SesionPartida.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/SesionPartidaTests.cs`

**Interfaces:**
- Consumes: `_juegos`, `JuegoResumen.Activar()/Finalizar()`, `EstadoJuego`, `EstadoSesion`.
- Produces:
  - `enum TipoResultadoAvance { Avanzado, Terminada }`
  - `ResultadoAvance` (record) with `Tipo`, `JuegoFinalizado` (`JuegoResumen`), `JuegoActivado` (`JuegoResumen?`); factories `ResultadoAvance.Avanzado(finalizado, activado)`, `ResultadoAvance.Terminada(finalizado)`.
  - `SesionPartida.FinalizarJuegoActual(DateTime now) → ResultadoAvance`.
  - `SesionNoIniciadaException(Guid partidaId)`.

- [ ] **Step 1: Write the failing tests** — append to `SesionPartidaTests.cs`:

```csharp
    private static SesionPartida Iniciada(int juegos)
    {
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), Snapshot(min: 1, max: 5, juegos: juegos));
        sesion.Inscribir(Guid.NewGuid(), false, 0, T0);
        sesion.Iniciar(T0);
        return sesion;
    }

    [Fact]
    public void FinalizarJuegoActual_advances_to_next_game()
    {
        var sesion = Iniciada(juegos: 3);

        var resultado = sesion.FinalizarJuegoActual(T0);

        Assert.Equal(TipoResultadoAvance.Avanzado, resultado.Tipo);
        Assert.False(resultado.Terminada());
        var ordenados = sesion.Juegos.OrderBy(j => j.Orden).ToList();
        Assert.Equal(EstadoJuego.Finalizado, ordenados[0].Estado);
        Assert.Equal(EstadoJuego.Activo, ordenados[1].Estado);
        Assert.Equal(EstadoJuego.Pendiente, ordenados[2].Estado);
        Assert.Equal(1, resultado.JuegoFinalizado.Orden);
        Assert.Equal(2, resultado.JuegoActivado!.Orden);
        Assert.Equal(EstadoSesion.Iniciada, sesion.Estado);
    }

    [Fact]
    public void FinalizarJuegoActual_on_last_game_terminates_partida()
    {
        var sesion = Iniciada(juegos: 1);

        var resultado = sesion.FinalizarJuegoActual(T0);

        Assert.Equal(TipoResultadoAvance.Terminada, resultado.Tipo);
        Assert.True(resultado.Terminada());
        Assert.Equal(EstadoSesion.Terminada, sesion.Estado);
        Assert.Equal(T0, sesion.FechaFin);
        Assert.Null(resultado.JuegoActivado);
        Assert.Equal(EstadoJuego.Finalizado, sesion.Juegos.Single().Estado);
    }

    [Fact]
    public void FinalizarJuegoActual_runs_full_sequence_to_terminada()
    {
        var sesion = Iniciada(juegos: 2);

        Assert.Equal(TipoResultadoAvance.Avanzado, sesion.FinalizarJuegoActual(T0).Tipo);
        Assert.Equal(TipoResultadoAvance.Terminada, sesion.FinalizarJuegoActual(T0).Tipo);
        Assert.Equal(EstadoSesion.Terminada, sesion.Estado);
        Assert.All(sesion.Juegos, j => Assert.Equal(EstadoJuego.Finalizado, j.Estado));
    }

    [Fact]
    public void FinalizarJuegoActual_when_not_iniciada_throws()
    {
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), Snapshot()); // Lobby
        Assert.Throws<SesionNoIniciadaException>(() => sesion.FinalizarJuegoActual(T0));
    }
```

Note: `resultado.Terminada()` is a convenience the test uses; expose it on `ResultadoAvance` as a method (Step 3).

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj --filter "FullyQualifiedName~FinalizarJuegoActual_"`
Expected: FAIL — `FinalizarJuegoActual`, `ResultadoAvance`, `SesionNoIniciadaException` do not exist.

- [ ] **Step 3: Create `ResultadoAvance.cs`**

```csharp
using Umbral.OperacionesSesion.Domain.Entities;

namespace Umbral.OperacionesSesion.Domain.Results;

public enum TipoResultadoAvance { Avanzado, Terminada }

public sealed record ResultadoAvance(TipoResultadoAvance Tipo, JuegoResumen JuegoFinalizado, JuegoResumen? JuegoActivado)
{
    public static ResultadoAvance Avanzado(JuegoResumen finalizado, JuegoResumen activado) =>
        new(TipoResultadoAvance.Avanzado, finalizado, activado);

    public static ResultadoAvance Terminada(JuegoResumen finalizado) =>
        new(TipoResultadoAvance.Terminada, finalizado, null);

    public bool Terminada() => Tipo == TipoResultadoAvance.Terminada;
}
```

- [ ] **Step 4: Create `SesionNoIniciadaException.cs`**

```csharp
namespace Umbral.OperacionesSesion.Domain.Exceptions;

public sealed class SesionNoIniciadaException : Exception
{
    public SesionNoIniciadaException(Guid partidaId)
        : base($"La sesión de la partida {partidaId} no está iniciada.") { }
}
```

- [ ] **Step 5: Add the method to `SesionPartida`** (after `IntentarInicioAutomatico`):

```csharp
    public ResultadoAvance FinalizarJuegoActual(DateTime now)
    {
        if (Estado != EstadoSesion.Iniciada)
            throw new SesionNoIniciadaException(PartidaId);

        var actual = _juegos.Single(j => j.Estado == EstadoJuego.Activo);
        actual.Finalizar();

        var siguiente = _juegos
            .Where(j => j.Estado == EstadoJuego.Pendiente)
            .OrderBy(j => j.Orden)
            .FirstOrDefault();

        if (siguiente is not null)
        {
            siguiente.Activar();
            return ResultadoAvance.Avanzado(actual, siguiente);
        }

        Estado = EstadoSesion.Terminada;
        FechaFin = now;
        return ResultadoAvance.Terminada(actual);
    }
```

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj --filter "FullyQualifiedName~FinalizarJuegoActual_"`
Expected: PASS (4 tests).

- [ ] **Step 7: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Results/ResultadoAvance.cs services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Exceptions/SesionNoIniciadaException.cs services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/SesionPartida.cs services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/SesionPartidaTests.cs
git commit -m "SP-3b: SesionPartida.FinalizarJuegoActual (avance secuencial → Terminada)"
```

---

### Task 5: Start events seam + `IniciarPartidaCommand` + handler

**Files:**
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Interfaces/SesionLifecycleEvents.cs` (the 3 start/cancel event records)
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Interfaces/ISesionEventsPublisher.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Services/NoOpSesionEventsPublisher.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Commands/IniciarPartidaCommand.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/DTOs/InicioPartidaResponse.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Validators/IniciarPartidaCommandValidator.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Commands/IniciarPartidaCommandHandler.cs`
- Modify (test fake): `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/Fakes/FakeSesionEventsPublisher.cs`
- Create (test fake): `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/Fakes/FakeTimeProvider.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/IniciarPartidaCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `ISesionPartidaRepository.GetByPartidaIdAsync`, `IOperacionesSesionUnitOfWork`, `TimeProvider`, `SesionPartida.Iniciar`, `ResultadoInicio`/`TipoResultadoInicio`, `SesionNoEncontradaException`.
- Produces:
  - Records `PartidaIniciadaEvent`, `JuegoActivadoEvent`, `PartidaCanceladaEvent`.
  - `ISesionEventsPublisher` gains `PublicarPartidaIniciadaAsync`, `PublicarJuegoActivadoAsync`, `PublicarPartidaCanceladaAsync`.
  - `IniciarPartidaCommand(Guid PartidaId) : IRequest<InicioPartidaResponse>`.
  - `InicioPartidaResponse(Guid PartidaId, string Estado, Guid? JuegoActivadoId, int? JuegoActivadoOrden)`.
  - `IniciarPartidaCommandHandler` with `internal static Task PublicarEventosInicioAsync(ISesionEventsPublisher, SesionPartida, ResultadoInicio, DateTime now, CancellationToken)` and `internal static InicioPartidaResponse MapearInicio(SesionPartida, ResultadoInicio)` (reused by Task 6).

- [ ] **Step 1: Write the failing test** — create `IniciarPartidaCommandHandlerTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Handlers.Commands;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class IniciarPartidaCommandHandlerTests
{
    private static readonly DateTime T0 = new(2026, 6, 26, 12, 0, 0, DateTimeKind.Utc);

    private static SesionPartida Sesion(Guid partidaId, int min, int max, int juegos, int inscritos)
    {
        var lista = Enumerable.Range(1, juegos).Select(o => new JuegoResumen(Guid.NewGuid(), o, TipoJuego.Trivia)).ToList();
        var snapshot = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, min, max, lista);
        var sesion = SesionPartida.Publicar(partidaId, snapshot);
        for (var i = 0; i < inscritos; i++) sesion.Inscribir(Guid.NewGuid(), false, i, T0);
        return sesion;
    }

    private static (IniciarPartidaCommandHandler handler, FakeSesionPartidaRepository repo, FakeOperacionesSesionUnitOfWork uow, FakeSesionEventsPublisher events) Build()
    {
        var repo = new FakeSesionPartidaRepository();
        var uow = new FakeOperacionesSesionUnitOfWork();
        var events = new FakeSesionEventsPublisher();
        var time = new FakeTimeProvider(T0);
        return (new IniciarPartidaCommandHandler(repo, uow, events, time), repo, uow, events);
    }

    [Fact]
    public async Task Iniciar_minimums_met_starts_saves_and_publishes_iniciada_and_juego_activado()
    {
        var partidaId = Guid.NewGuid();
        var (handler, repo, uow, events) = Build();
        repo.Add(Sesion(partidaId, min: 1, max: 5, juegos: 2, inscritos: 1));

        var response = await handler.Handle(new IniciarPartidaCommand(partidaId), CancellationToken.None);

        Assert.Equal("Iniciada", response.Estado);
        Assert.Equal(1, response.JuegoActivadoOrden);
        Assert.Equal(1, uow.SaveCount);
        Assert.Single(events.PartidasIniciadas);
        Assert.Single(events.JuegosActivados);
        Assert.Empty(events.PartidasCanceladas);
        Assert.Equal(1, events.JuegosActivados[0].Orden);
    }

    [Fact]
    public async Task Iniciar_minimums_not_met_cancels_saves_and_publishes_cancelada()
    {
        var partidaId = Guid.NewGuid();
        var (handler, repo, uow, events) = Build();
        repo.Add(Sesion(partidaId, min: 2, max: 5, juegos: 1, inscritos: 1));

        var response = await handler.Handle(new IniciarPartidaCommand(partidaId), CancellationToken.None);

        Assert.Equal("Cancelada", response.Estado);
        Assert.Null(response.JuegoActivadoOrden);
        Assert.Equal(1, uow.SaveCount);
        Assert.Single(events.PartidasCanceladas);
        Assert.Equal("MinimosNoAlcanzados", events.PartidasCanceladas[0].Motivo);
        Assert.Empty(events.PartidasIniciadas);
    }

    [Fact]
    public async Task Iniciar_unknown_partida_throws()
    {
        var (handler, _, _, _) = Build();
        await Assert.ThrowsAsync<SesionNoEncontradaException>(
            () => handler.Handle(new IniciarPartidaCommand(Guid.NewGuid()), CancellationToken.None));
    }
}
```

`FakeTimeProvider` is a hand-rolled test fake (created next), not the NuGet `Microsoft.Extensions.Time.Testing` type — no new package is added.

- [ ] **Step 1b: Create the deterministic clock fake** — `Fakes/FakeTimeProvider.cs`:

```csharp
using System;

namespace Umbral.OperacionesSesion.UnitTests.Application.Fakes;

public sealed class FakeTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _now;

    public FakeTimeProvider(DateTime utcNow)
        => _now = new DateTimeOffset(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc));

    public override DateTimeOffset GetUtcNow() => _now;
}
```

(`handler.GetUtcNow().UtcDateTime` then returns exactly the provided `DateTime`, so `FechaInicio == T0` and due/not-due gating are deterministic.)

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj --filter "FullyQualifiedName~IniciarPartidaCommandHandlerTests"`
Expected: FAIL — the SP-3b production types (`IniciarPartidaCommand`, handler, `InicioPartidaResponse`, event records) do not exist. (`FakeTimeProvider` now exists from Step 1b.)

- [ ] **Step 3: Create the event records** — `Interfaces/SesionLifecycleEvents.cs`:

```csharp
namespace Umbral.OperacionesSesion.Application.Interfaces;

public sealed record PartidaIniciadaEvent(
    Guid PartidaId, Guid SesionPartidaId, DateTime FechaInicio, Guid PrimerJuegoId, int PrimerJuegoOrden);

public sealed record JuegoActivadoEvent(
    Guid PartidaId, Guid SesionPartidaId, Guid JuegoId, int Orden, string TipoJuego);

public sealed record PartidaCanceladaEvent(
    Guid PartidaId, Guid SesionPartidaId, string Motivo, DateTime FechaCancelacion);
```

- [ ] **Step 4: Extend `ISesionEventsPublisher`** — add the three methods:

```csharp
namespace Umbral.OperacionesSesion.Application.Interfaces;

public interface ISesionEventsPublisher
{
    Task PublicarPartidaPublicadaEnLobbyAsync(PartidaPublicadaEnLobbyEvent evento, CancellationToken cancellationToken);
    Task PublicarPartidaIniciadaAsync(PartidaIniciadaEvent evento, CancellationToken cancellationToken);
    Task PublicarJuegoActivadoAsync(JuegoActivadoEvent evento, CancellationToken cancellationToken);
    Task PublicarPartidaCanceladaAsync(PartidaCanceladaEvent evento, CancellationToken cancellationToken);
}
```

- [ ] **Step 5: Extend `NoOpSesionEventsPublisher`** — implement the new methods:

```csharp
using Umbral.OperacionesSesion.Application.Interfaces;

namespace Umbral.OperacionesSesion.Infrastructure.Services;

// No-Op until the dedicated RabbitMQ backbone slice (mirrors Identity's NoOpEquipoEventsPublisher).
// The publish seam is exercised end-to-end; nothing is delivered yet.
public sealed class NoOpSesionEventsPublisher : ISesionEventsPublisher
{
    public Task PublicarPartidaPublicadaEnLobbyAsync(PartidaPublicadaEnLobbyEvent evento, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task PublicarPartidaIniciadaAsync(PartidaIniciadaEvent evento, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task PublicarJuegoActivadoAsync(JuegoActivadoEvent evento, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task PublicarPartidaCanceladaAsync(PartidaCanceladaEvent evento, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
```

- [ ] **Step 6: Extend `FakeSesionEventsPublisher`** (test fake) — replace the file:

```csharp
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.Interfaces;

namespace Umbral.OperacionesSesion.UnitTests.Application.Fakes;

public sealed class FakeSesionEventsPublisher : ISesionEventsPublisher
{
    public PartidaPublicadaEnLobbyEvent? LastEvent { get; private set; }
    public int PublishCount { get; private set; }
    public List<PartidaIniciadaEvent> PartidasIniciadas { get; } = new();
    public List<JuegoActivadoEvent> JuegosActivados { get; } = new();
    public List<PartidaFinalizadaEvent> PartidasFinalizadas { get; } = new();
    public List<PartidaCanceladaEvent> PartidasCanceladas { get; } = new();

    public Task PublicarPartidaPublicadaEnLobbyAsync(PartidaPublicadaEnLobbyEvent evento, CancellationToken cancellationToken)
    {
        LastEvent = evento;
        PublishCount++;
        return Task.CompletedTask;
    }

    public Task PublicarPartidaIniciadaAsync(PartidaIniciadaEvent evento, CancellationToken cancellationToken)
    {
        PartidasIniciadas.Add(evento);
        return Task.CompletedTask;
    }

    public Task PublicarJuegoActivadoAsync(JuegoActivadoEvent evento, CancellationToken cancellationToken)
    {
        JuegosActivados.Add(evento);
        return Task.CompletedTask;
    }

    public Task PublicarPartidaFinalizadaAsync(PartidaFinalizadaEvent evento, CancellationToken cancellationToken)
    {
        PartidasFinalizadas.Add(evento);
        return Task.CompletedTask;
    }

    public Task PublicarPartidaCanceladaAsync(PartidaCanceladaEvent evento, CancellationToken cancellationToken)
    {
        PartidasCanceladas.Add(evento);
        return Task.CompletedTask;
    }
}
```

(`PartidaFinalizadaEvent` + `PublicarPartidaFinalizadaAsync` are added to the interface in Task 7. This fake already implements them so the fake compiles after Task 7; before Task 7 the interface lacks that member, so **temporarily** the fake's `PublicarPartidaFinalizadaAsync` is just an extra method that does not implement any interface member — it compiles fine. The `PartidaFinalizadaEvent` type, however, does not exist until Task 7. **To keep this task compiling, omit the `PartidasFinalizadas` list and `PublicarPartidaFinalizadaAsync` method now and add them in Task 7.** Use the reduced fake below for Task 5.)

Reduced fake for Task 5 (use this; Task 7 re-adds the finalizada members):

```csharp
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.Interfaces;

namespace Umbral.OperacionesSesion.UnitTests.Application.Fakes;

public sealed class FakeSesionEventsPublisher : ISesionEventsPublisher
{
    public PartidaPublicadaEnLobbyEvent? LastEvent { get; private set; }
    public int PublishCount { get; private set; }
    public List<PartidaIniciadaEvent> PartidasIniciadas { get; } = new();
    public List<JuegoActivadoEvent> JuegosActivados { get; } = new();
    public List<PartidaCanceladaEvent> PartidasCanceladas { get; } = new();

    public Task PublicarPartidaPublicadaEnLobbyAsync(PartidaPublicadaEnLobbyEvent evento, CancellationToken cancellationToken)
    {
        LastEvent = evento;
        PublishCount++;
        return Task.CompletedTask;
    }

    public Task PublicarPartidaIniciadaAsync(PartidaIniciadaEvent evento, CancellationToken cancellationToken)
    {
        PartidasIniciadas.Add(evento);
        return Task.CompletedTask;
    }

    public Task PublicarJuegoActivadoAsync(JuegoActivadoEvent evento, CancellationToken cancellationToken)
    {
        JuegosActivados.Add(evento);
        return Task.CompletedTask;
    }

    public Task PublicarPartidaCanceladaAsync(PartidaCanceladaEvent evento, CancellationToken cancellationToken)
    {
        PartidasCanceladas.Add(evento);
        return Task.CompletedTask;
    }
}
```

- [ ] **Step 7: Create the command + DTO + validator**

`Commands/IniciarPartidaCommand.cs`:
```csharp
using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;

namespace Umbral.OperacionesSesion.Application.Commands;

public sealed record IniciarPartidaCommand(Guid PartidaId) : IRequest<InicioPartidaResponse>;
```

`DTOs/InicioPartidaResponse.cs`:
```csharp
namespace Umbral.OperacionesSesion.Application.DTOs;

public sealed record InicioPartidaResponse(
    Guid PartidaId,
    string Estado,
    Guid? JuegoActivadoId,
    int? JuegoActivadoOrden);
```

`Validators/IniciarPartidaCommandValidator.cs`:
```csharp
using FluentValidation;
using Umbral.OperacionesSesion.Application.Commands;

namespace Umbral.OperacionesSesion.Application.Validators;

public sealed class IniciarPartidaCommandValidator : AbstractValidator<IniciarPartidaCommand>
{
    public IniciarPartidaCommandValidator()
    {
        RuleFor(x => x.PartidaId).NotEmpty();
    }
}
```

- [ ] **Step 8: Create the handler** — `Handlers/Commands/IniciarPartidaCommandHandler.cs`:

```csharp
using MediatR;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Interfaces;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Results;

namespace Umbral.OperacionesSesion.Application.Handlers.Commands;

public sealed class IniciarPartidaCommandHandler : IRequestHandler<IniciarPartidaCommand, InicioPartidaResponse>
{
    private readonly ISesionPartidaRepository _sesiones;
    private readonly IOperacionesSesionUnitOfWork _unitOfWork;
    private readonly ISesionEventsPublisher _events;
    private readonly TimeProvider _timeProvider;

    public IniciarPartidaCommandHandler(
        ISesionPartidaRepository sesiones,
        IOperacionesSesionUnitOfWork unitOfWork,
        ISesionEventsPublisher events,
        TimeProvider timeProvider)
    {
        _sesiones = sesiones;
        _unitOfWork = unitOfWork;
        _events = events;
        _timeProvider = timeProvider;
    }

    public async Task<InicioPartidaResponse> Handle(IniciarPartidaCommand request, CancellationToken cancellationToken)
    {
        var sesion = await _sesiones.GetByPartidaIdAsync(request.PartidaId, cancellationToken)
            ?? throw new SesionNoEncontradaException(request.PartidaId);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var resultado = sesion.Iniciar(now);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await PublicarEventosInicioAsync(_events, sesion, resultado, now, cancellationToken);
        return MapearInicio(sesion, resultado);
    }

    internal static async Task PublicarEventosInicioAsync(
        ISesionEventsPublisher events, SesionPartida sesion, ResultadoInicio resultado, DateTime now, CancellationToken cancellationToken)
    {
        switch (resultado.Tipo)
        {
            case TipoResultadoInicio.Iniciada:
                var juego = resultado.JuegoActivado!;
                await events.PublicarPartidaIniciadaAsync(
                    new PartidaIniciadaEvent(sesion.PartidaId, sesion.Id.Valor, now, juego.JuegoId, juego.Orden),
                    cancellationToken);
                await events.PublicarJuegoActivadoAsync(
                    new JuegoActivadoEvent(sesion.PartidaId, sesion.Id.Valor, juego.JuegoId, juego.Orden, juego.TipoJuego.ToString()),
                    cancellationToken);
                break;
            case TipoResultadoInicio.Cancelada:
                await events.PublicarPartidaCanceladaAsync(
                    new PartidaCanceladaEvent(sesion.PartidaId, sesion.Id.Valor, "MinimosNoAlcanzados", now),
                    cancellationToken);
                break;
            // NoCorresponde → no event
        }
    }

    internal static InicioPartidaResponse MapearInicio(SesionPartida sesion, ResultadoInicio resultado) =>
        new(sesion.PartidaId, sesion.Estado.ToString(), resultado.JuegoActivado?.JuegoId, resultado.JuegoActivado?.Orden);
}
```

- [ ] **Step 9: Run test to verify it passes**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj --filter "FullyQualifiedName~IniciarPartidaCommandHandlerTests"`
Expected: PASS (3 tests).

- [ ] **Step 10: Commit**

```bash
git add services/operaciones-sesion/src services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests
git commit -m "SP-3b: seam de eventos de inicio + IniciarPartidaCommand + handler"
```

---

### Task 6: `IntentarInicioAutomaticoCommand` + handler

**Files:**
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Commands/IntentarInicioAutomaticoCommand.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Validators/IntentarInicioAutomaticoCommandValidator.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Commands/IntentarInicioAutomaticoCommandHandler.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/IntentarInicioAutomaticoCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `SesionPartida.IntentarInicioAutomatico`, `IniciarPartidaCommandHandler.PublicarEventosInicioAsync` + `MapearInicio` (Task 5), `InicioPartidaResponse`, `TimeProvider`, `SesionNoEncontradaException`.
- Produces: `IntentarInicioAutomaticoCommand(Guid PartidaId) : IRequest<InicioPartidaResponse>`; its handler.

- [ ] **Step 1: Write the failing test** — create `IntentarInicioAutomaticoCommandHandlerTests.cs`:

```csharp
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.Handlers.Commands;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class IntentarInicioAutomaticoCommandHandlerTests
{
    private static readonly DateTime TDue = new(2026, 6, 26, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime TBefore = new(2026, 6, 26, 11, 0, 0, DateTimeKind.Utc);

    private static SesionPartida Sesion(Guid partidaId, ModoInicioPartida modo, DateTime? tiempoInicio, int inscritos)
    {
        var lista = Enumerable.Range(1, 2).Select(o => new JuegoResumen(Guid.NewGuid(), o, TipoJuego.Trivia)).ToList();
        var snapshot = new ConfiguracionSnapshot("Copa", Modalidad.Individual, modo, tiempoInicio, 1, 5, lista);
        var sesion = SesionPartida.Publicar(partidaId, snapshot);
        for (var i = 0; i < inscritos; i++) sesion.Inscribir(Guid.NewGuid(), false, i, TBefore);
        return sesion;
    }

    private static (IntentarInicioAutomaticoCommandHandler handler, FakeOperacionesSesionUnitOfWork uow, FakeSesionEventsPublisher events) Build(
        FakeSesionPartidaRepository repo, DateTime now)
    {
        var uow = new FakeOperacionesSesionUnitOfWork();
        var events = new FakeSesionEventsPublisher();
        var time = new FakeTimeProvider(now);
        return (new IntentarInicioAutomaticoCommandHandler(repo, uow, events, time), uow, events);
    }

    [Fact]
    public async Task When_due_and_minimums_met_starts_and_publishes()
    {
        var partidaId = Guid.NewGuid();
        var repo = new FakeSesionPartidaRepository();
        repo.Add(Sesion(partidaId, ModoInicioPartida.Automatico, TDue, inscritos: 1));
        var (handler, uow, events) = Build(repo, TDue);

        var response = await handler.Handle(new IntentarInicioAutomaticoCommand(partidaId), CancellationToken.None);

        Assert.Equal("Iniciada", response.Estado);
        Assert.Equal(1, uow.SaveCount);
        Assert.Single(events.PartidasIniciadas);
        Assert.Single(events.JuegosActivados);
    }

    [Fact]
    public async Task When_not_due_is_noop_no_save_no_event()
    {
        var partidaId = Guid.NewGuid();
        var repo = new FakeSesionPartidaRepository();
        repo.Add(Sesion(partidaId, ModoInicioPartida.Automatico, TDue, inscritos: 1));
        var (handler, uow, events) = Build(repo, TBefore); // before due

        var response = await handler.Handle(new IntentarInicioAutomaticoCommand(partidaId), CancellationToken.None);

        Assert.Equal("Lobby", response.Estado);
        Assert.Equal(0, uow.SaveCount);
        Assert.Empty(events.PartidasIniciadas);
        Assert.Empty(events.JuegosActivados);
        Assert.Empty(events.PartidasCanceladas);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj --filter "FullyQualifiedName~IntentarInicioAutomaticoCommandHandlerTests"`
Expected: FAIL — command + handler do not exist.

- [ ] **Step 3: Create the command + validator**

`Commands/IntentarInicioAutomaticoCommand.cs`:
```csharp
using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;

namespace Umbral.OperacionesSesion.Application.Commands;

public sealed record IntentarInicioAutomaticoCommand(Guid PartidaId) : IRequest<InicioPartidaResponse>;
```

`Validators/IntentarInicioAutomaticoCommandValidator.cs`:
```csharp
using FluentValidation;
using Umbral.OperacionesSesion.Application.Commands;

namespace Umbral.OperacionesSesion.Application.Validators;

public sealed class IntentarInicioAutomaticoCommandValidator : AbstractValidator<IntentarInicioAutomaticoCommand>
{
    public IntentarInicioAutomaticoCommandValidator()
    {
        RuleFor(x => x.PartidaId).NotEmpty();
    }
}
```

- [ ] **Step 4: Create the handler** — `Handlers/Commands/IntentarInicioAutomaticoCommandHandler.cs`:

```csharp
using MediatR;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Interfaces;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;
using Umbral.OperacionesSesion.Domain.Results;

namespace Umbral.OperacionesSesion.Application.Handlers.Commands;

public sealed class IntentarInicioAutomaticoCommandHandler : IRequestHandler<IntentarInicioAutomaticoCommand, InicioPartidaResponse>
{
    private readonly ISesionPartidaRepository _sesiones;
    private readonly IOperacionesSesionUnitOfWork _unitOfWork;
    private readonly ISesionEventsPublisher _events;
    private readonly TimeProvider _timeProvider;

    public IntentarInicioAutomaticoCommandHandler(
        ISesionPartidaRepository sesiones,
        IOperacionesSesionUnitOfWork unitOfWork,
        ISesionEventsPublisher events,
        TimeProvider timeProvider)
    {
        _sesiones = sesiones;
        _unitOfWork = unitOfWork;
        _events = events;
        _timeProvider = timeProvider;
    }

    public async Task<InicioPartidaResponse> Handle(IntentarInicioAutomaticoCommand request, CancellationToken cancellationToken)
    {
        var sesion = await _sesiones.GetByPartidaIdAsync(request.PartidaId, cancellationToken)
            ?? throw new SesionNoEncontradaException(request.PartidaId);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var resultado = sesion.IntentarInicioAutomatico(now);

        if (resultado.Tipo != TipoResultadoInicio.NoCorresponde)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await IniciarPartidaCommandHandler.PublicarEventosInicioAsync(_events, sesion, resultado, now, cancellationToken);
        }

        return IniciarPartidaCommandHandler.MapearInicio(sesion, resultado);
    }
}
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj --filter "FullyQualifiedName~IntentarInicioAutomaticoCommandHandlerTests"`
Expected: PASS (2 tests).

- [ ] **Step 6: Commit**

```bash
git add services/operaciones-sesion/src services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests
git commit -m "SP-3b: IntentarInicioAutomaticoCommand + handler (idempotente)"
```

---

### Task 7: `FinalizarJuegoActualCommand` + handler + `PartidaFinalizada` event

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Interfaces/SesionLifecycleEvents.cs` (add `PartidaFinalizadaEvent`)
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Interfaces/ISesionEventsPublisher.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Services/NoOpSesionEventsPublisher.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Commands/FinalizarJuegoActualCommand.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/DTOs/AvanceJuegoResponse.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Validators/FinalizarJuegoActualCommandValidator.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Commands/FinalizarJuegoActualCommandHandler.cs`
- Modify (test fake): `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/Fakes/FakeSesionEventsPublisher.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/FinalizarJuegoActualCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `SesionPartida.FinalizarJuegoActual`, `ResultadoAvance`/`TipoResultadoAvance`, `JuegoActivadoEvent` (Task 5), `TimeProvider`, `SesionNoEncontradaException`.
- Produces: `PartidaFinalizadaEvent`; `ISesionEventsPublisher.PublicarPartidaFinalizadaAsync`; `FinalizarJuegoActualCommand(Guid PartidaId) : IRequest<AvanceJuegoResponse>`; `AvanceJuegoResponse(Guid PartidaId, string Estado, int? JuegoFinalizadoOrden, int? JuegoActivadoOrden, bool Terminada)`; its handler.

- [ ] **Step 1: Write the failing test** — create `FinalizarJuegoActualCommandHandlerTests.cs`:

```csharp
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Handlers.Commands;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class FinalizarJuegoActualCommandHandlerTests
{
    private static readonly DateTime T0 = new(2026, 6, 26, 12, 0, 0, DateTimeKind.Utc);

    private static SesionPartida Iniciada(Guid partidaId, int juegos)
    {
        var lista = Enumerable.Range(1, juegos).Select(o => new JuegoResumen(Guid.NewGuid(), o, TipoJuego.Trivia)).ToList();
        var snapshot = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 5, lista);
        var sesion = SesionPartida.Publicar(partidaId, snapshot);
        sesion.Inscribir(Guid.NewGuid(), false, 0, T0);
        sesion.Iniciar(T0);
        return sesion;
    }

    private static (FinalizarJuegoActualCommandHandler handler, FakeOperacionesSesionUnitOfWork uow, FakeSesionEventsPublisher events) Build(FakeSesionPartidaRepository repo)
    {
        var uow = new FakeOperacionesSesionUnitOfWork();
        var events = new FakeSesionEventsPublisher();
        var time = new FakeTimeProvider(T0);
        return (new FinalizarJuegoActualCommandHandler(repo, uow, events, time), uow, events);
    }

    [Fact]
    public async Task Advance_publishes_juego_activado()
    {
        var partidaId = Guid.NewGuid();
        var repo = new FakeSesionPartidaRepository();
        repo.Add(Iniciada(partidaId, juegos: 2));
        var (handler, uow, events) = Build(repo);

        var response = await handler.Handle(new FinalizarJuegoActualCommand(partidaId), CancellationToken.None);

        Assert.Equal("Iniciada", response.Estado);
        Assert.False(response.Terminada);
        Assert.Equal(1, response.JuegoFinalizadoOrden);
        Assert.Equal(2, response.JuegoActivadoOrden);
        Assert.Equal(1, uow.SaveCount);
        Assert.Single(events.JuegosActivados);
        Assert.Empty(events.PartidasFinalizadas);
    }

    [Fact]
    public async Task Finishing_last_game_publishes_partida_finalizada()
    {
        var partidaId = Guid.NewGuid();
        var repo = new FakeSesionPartidaRepository();
        repo.Add(Iniciada(partidaId, juegos: 1));
        var (handler, uow, events) = Build(repo);

        var response = await handler.Handle(new FinalizarJuegoActualCommand(partidaId), CancellationToken.None);

        Assert.Equal("Terminada", response.Estado);
        Assert.True(response.Terminada);
        Assert.Null(response.JuegoActivadoOrden);
        Assert.Single(events.PartidasFinalizadas);
        Assert.Empty(events.JuegosActivados);
    }

    [Fact]
    public async Task Unknown_partida_throws()
    {
        var (handler, _, _) = Build(new FakeSesionPartidaRepository());
        await Assert.ThrowsAsync<SesionNoEncontradaException>(
            () => handler.Handle(new FinalizarJuegoActualCommand(Guid.NewGuid()), CancellationToken.None));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj --filter "FullyQualifiedName~FinalizarJuegoActualCommandHandlerTests"`
Expected: FAIL — types do not exist.

- [ ] **Step 3: Add `PartidaFinalizadaEvent`** to `Interfaces/SesionLifecycleEvents.cs` (append):

```csharp
public sealed record PartidaFinalizadaEvent(
    Guid PartidaId, Guid SesionPartidaId, DateTime FechaFin);
```

- [ ] **Step 4: Add the publisher method** to `ISesionEventsPublisher` (insert before the closing brace):

```csharp
    Task PublicarPartidaFinalizadaAsync(PartidaFinalizadaEvent evento, CancellationToken cancellationToken);
```

- [ ] **Step 5: Implement it in `NoOpSesionEventsPublisher`** (add method):

```csharp
    public Task PublicarPartidaFinalizadaAsync(PartidaFinalizadaEvent evento, CancellationToken cancellationToken)
        => Task.CompletedTask;
```

- [ ] **Step 6: Add the finalizada members to `FakeSesionEventsPublisher`** — add the list property:

```csharp
    public List<PartidaFinalizadaEvent> PartidasFinalizadas { get; } = new();
```

and the method:

```csharp
    public Task PublicarPartidaFinalizadaAsync(PartidaFinalizadaEvent evento, CancellationToken cancellationToken)
    {
        PartidasFinalizadas.Add(evento);
        return Task.CompletedTask;
    }
```

- [ ] **Step 7: Create the command + DTO + validator**

`Commands/FinalizarJuegoActualCommand.cs`:
```csharp
using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;

namespace Umbral.OperacionesSesion.Application.Commands;

public sealed record FinalizarJuegoActualCommand(Guid PartidaId) : IRequest<AvanceJuegoResponse>;
```

`DTOs/AvanceJuegoResponse.cs`:
```csharp
namespace Umbral.OperacionesSesion.Application.DTOs;

public sealed record AvanceJuegoResponse(
    Guid PartidaId,
    string Estado,
    int? JuegoFinalizadoOrden,
    int? JuegoActivadoOrden,
    bool Terminada);
```

`Validators/FinalizarJuegoActualCommandValidator.cs`:
```csharp
using FluentValidation;
using Umbral.OperacionesSesion.Application.Commands;

namespace Umbral.OperacionesSesion.Application.Validators;

public sealed class FinalizarJuegoActualCommandValidator : AbstractValidator<FinalizarJuegoActualCommand>
{
    public FinalizarJuegoActualCommandValidator()
    {
        RuleFor(x => x.PartidaId).NotEmpty();
    }
}
```

- [ ] **Step 8: Create the handler** — `Handlers/Commands/FinalizarJuegoActualCommandHandler.cs`:

```csharp
using MediatR;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Interfaces;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;
using Umbral.OperacionesSesion.Domain.Results;

namespace Umbral.OperacionesSesion.Application.Handlers.Commands;

public sealed class FinalizarJuegoActualCommandHandler : IRequestHandler<FinalizarJuegoActualCommand, AvanceJuegoResponse>
{
    private readonly ISesionPartidaRepository _sesiones;
    private readonly IOperacionesSesionUnitOfWork _unitOfWork;
    private readonly ISesionEventsPublisher _events;
    private readonly TimeProvider _timeProvider;

    public FinalizarJuegoActualCommandHandler(
        ISesionPartidaRepository sesiones,
        IOperacionesSesionUnitOfWork unitOfWork,
        ISesionEventsPublisher events,
        TimeProvider timeProvider)
    {
        _sesiones = sesiones;
        _unitOfWork = unitOfWork;
        _events = events;
        _timeProvider = timeProvider;
    }

    public async Task<AvanceJuegoResponse> Handle(FinalizarJuegoActualCommand request, CancellationToken cancellationToken)
    {
        var sesion = await _sesiones.GetByPartidaIdAsync(request.PartidaId, cancellationToken)
            ?? throw new SesionNoEncontradaException(request.PartidaId);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var resultado = sesion.FinalizarJuegoActual(now);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (resultado.Tipo == TipoResultadoAvance.Avanzado)
        {
            var juego = resultado.JuegoActivado!;
            await _events.PublicarJuegoActivadoAsync(
                new JuegoActivadoEvent(sesion.PartidaId, sesion.Id.Valor, juego.JuegoId, juego.Orden, juego.TipoJuego.ToString()),
                cancellationToken);
        }
        else
        {
            await _events.PublicarPartidaFinalizadaAsync(
                new PartidaFinalizadaEvent(sesion.PartidaId, sesion.Id.Valor, now),
                cancellationToken);
        }

        return new AvanceJuegoResponse(
            sesion.PartidaId,
            sesion.Estado.ToString(),
            resultado.JuegoFinalizado.Orden,
            resultado.JuegoActivado?.Orden,
            resultado.Terminada());
    }
}
```

- [ ] **Step 9: Run test to verify it passes**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj --filter "FullyQualifiedName~FinalizarJuegoActualCommandHandlerTests"`
Expected: PASS (3 tests).

- [ ] **Step 10: Commit**

```bash
git add services/operaciones-sesion/src services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests
git commit -m "SP-3b: FinalizarJuegoActualCommand + handler + evento PartidaFinalizada"
```

---

### Task 8: `ObtenerEstadoSesionQuery` + handler + DTOs

**Files:**
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Queries/ObtenerEstadoSesionQuery.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/DTOs/EstadoSesionDto.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Queries/ObtenerEstadoSesionQueryHandler.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/ObtenerEstadoSesionQueryHandlerTests.cs`

**Interfaces:**
- Consumes: `ISesionPartidaRepository.GetByPartidaIdAsync`, `SesionPartida` (Juegos with `EstadoJuego`), `SesionNoEncontradaException`.
- Produces:
  - `ObtenerEstadoSesionQuery(Guid PartidaId) : IRequest<EstadoSesionDto>`.
  - `EstadoSesionDto(Guid PartidaId, Guid SesionPartidaId, string Estado, string Modalidad, IReadOnlyList<JuegoEstadoDto> Juegos, int? JuegoActualOrden)`.
  - `JuegoEstadoDto(Guid JuegoId, int Orden, string TipoJuego, string Estado)`.

- [ ] **Step 1: Write the failing test** — create `ObtenerEstadoSesionQueryHandlerTests.cs`:

```csharp
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Handlers.Queries;
using Umbral.OperacionesSesion.Application.Queries;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class ObtenerEstadoSesionQueryHandlerTests
{
    private static readonly DateTime T0 = new(2026, 6, 26, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Returns_estado_games_and_active_game_orden()
    {
        var partidaId = Guid.NewGuid();
        var lista = Enumerable.Range(1, 2).Select(o => new JuegoResumen(Guid.NewGuid(), o, TipoJuego.Trivia)).ToList();
        var snapshot = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 5, lista);
        var sesion = SesionPartida.Publicar(partidaId, snapshot);
        sesion.Inscribir(Guid.NewGuid(), false, 0, T0);
        sesion.Iniciar(T0); // game 1 → Activo
        var repo = new FakeSesionPartidaRepository();
        repo.Add(sesion);
        var handler = new ObtenerEstadoSesionQueryHandler(repo);

        var dto = await handler.Handle(new ObtenerEstadoSesionQuery(partidaId), CancellationToken.None);

        Assert.Equal("Iniciada", dto.Estado);
        Assert.Equal("Individual", dto.Modalidad);
        Assert.Equal(2, dto.Juegos.Count);
        Assert.Equal(1, dto.JuegoActualOrden);
        Assert.Equal("Activo", dto.Juegos.Single(j => j.Orden == 1).Estado);
        Assert.Equal("Pendiente", dto.Juegos.Single(j => j.Orden == 2).Estado);
    }

    [Fact]
    public async Task Unknown_partida_throws()
    {
        var handler = new ObtenerEstadoSesionQueryHandler(new FakeSesionPartidaRepository());
        await Assert.ThrowsAsync<SesionNoEncontradaException>(
            () => handler.Handle(new ObtenerEstadoSesionQuery(Guid.NewGuid()), CancellationToken.None));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj --filter "FullyQualifiedName~ObtenerEstadoSesionQueryHandlerTests"`
Expected: FAIL — types do not exist.

- [ ] **Step 3: Create the query**

`Queries/ObtenerEstadoSesionQuery.cs`:
```csharp
using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;

namespace Umbral.OperacionesSesion.Application.Queries;

public sealed record ObtenerEstadoSesionQuery(Guid PartidaId) : IRequest<EstadoSesionDto>;
```

- [ ] **Step 4: Create the DTOs** — `DTOs/EstadoSesionDto.cs`:

```csharp
namespace Umbral.OperacionesSesion.Application.DTOs;

public sealed record EstadoSesionDto(
    Guid PartidaId,
    Guid SesionPartidaId,
    string Estado,
    string Modalidad,
    IReadOnlyList<JuegoEstadoDto> Juegos,
    int? JuegoActualOrden);

public sealed record JuegoEstadoDto(
    Guid JuegoId,
    int Orden,
    string TipoJuego,
    string Estado);
```

- [ ] **Step 5: Create the handler** — `Handlers/Queries/ObtenerEstadoSesionQueryHandler.cs`:

```csharp
using System.Linq;
using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Queries;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;
using Umbral.OperacionesSesion.Domain.Enums;

namespace Umbral.OperacionesSesion.Application.Handlers.Queries;

public sealed class ObtenerEstadoSesionQueryHandler : IRequestHandler<ObtenerEstadoSesionQuery, EstadoSesionDto>
{
    private readonly ISesionPartidaRepository _sesiones;

    public ObtenerEstadoSesionQueryHandler(ISesionPartidaRepository sesiones) => _sesiones = sesiones;

    public async Task<EstadoSesionDto> Handle(ObtenerEstadoSesionQuery request, CancellationToken cancellationToken)
    {
        var sesion = await _sesiones.GetByPartidaIdAsync(request.PartidaId, cancellationToken)
            ?? throw new SesionNoEncontradaException(request.PartidaId);

        var juegos = sesion.Juegos
            .OrderBy(j => j.Orden)
            .Select(j => new JuegoEstadoDto(j.JuegoId, j.Orden, j.TipoJuego.ToString(), j.Estado.ToString()))
            .ToList();

        var actual = sesion.Juegos.FirstOrDefault(j => j.Estado == EstadoJuego.Activo);

        return new EstadoSesionDto(
            sesion.PartidaId,
            sesion.Id.Valor,
            sesion.Estado.ToString(),
            sesion.Modalidad.ToString(),
            juegos,
            actual?.Orden);
    }
}
```

- [ ] **Step 6: Run test to verify it passes**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj --filter "FullyQualifiedName~ObtenerEstadoSesionQueryHandlerTests"`
Expected: PASS (2 tests).

- [ ] **Step 7: Commit**

```bash
git add services/operaciones-sesion/src services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests
git commit -m "SP-3b: ObtenerEstadoSesionQuery + handler + EstadoSesionDto"
```

---

### Task 9: Persistence — column mappings + EF migration

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Persistence/OperacionesSesionDbContext.cs`
- Create (generated): `services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Persistence/Migrations/<timestamp>_SP3bInicioSecuenciacion.cs` (+ `.Designer.cs`) and updated `OperacionesSesionDbContextModelSnapshot.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests/SesionPersistenceTests.cs`

**Interfaces:**
- Consumes: existing `OperacionesSesionDbContext` mappings; `SesionPartida.Iniciar`/`FinalizarJuegoActual`; `JuegoResumen.Estado`.
- Produces: `sesiones_partida.fechainicio`, `sesiones_partida.fechafin`, `sesion_juegos.estadojuego` columns; the migration.

- [ ] **Step 1: Write the failing test** — append to `SesionPersistenceTests.cs` (inside the class):

```csharp
    [Fact]
    public async Task Persists_lifecycle_state_after_start_and_advance()
    {
        var partidaId = Guid.NewGuid();
        var snapshot = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10,
            new[] { new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia), new JuegoResumen(Guid.NewGuid(), 2, TipoJuego.Trivia) });
        var sesion = SesionPartida.Publicar(partidaId, snapshot);
        sesion.Inscribir(Guid.NewGuid(), false, 0, DateTime.UtcNow);
        var now = new DateTime(2026, 6, 26, 12, 0, 0, DateTimeKind.Utc);
        sesion.Iniciar(now);
        sesion.FinalizarJuegoActual(now); // game1 Finalizado, game2 Activo, still Iniciada

        var options = new DbContextOptionsBuilder<OperacionesSesionDbContext>()
            .UseInMemoryDatabase("lifecycle-" + Guid.NewGuid()).Options;

        await using (var write = new OperacionesSesionDbContext(options))
        {
            new SesionPartidaRepository(write).Add(sesion);
            await new OperacionesSesionUnitOfWork(write).SaveChangesAsync(CancellationToken.None);
        }

        await using (var read = new OperacionesSesionDbContext(options))
        {
            var loaded = await new SesionPartidaRepository(read).GetByPartidaIdAsync(partidaId, CancellationToken.None);
            Assert.NotNull(loaded);
            Assert.Equal(EstadoSesion.Iniciada, loaded!.Estado);
            Assert.Equal(now, loaded.FechaInicio);
            Assert.Null(loaded.FechaFin);
            var ordenados = loaded.Juegos.OrderBy(j => j.Orden).ToList();
            Assert.Equal(EstadoJuego.Finalizado, ordenados[0].Estado);
            Assert.Equal(EstadoJuego.Activo, ordenados[1].Estado);
        }
    }
```

- [ ] **Step 2: Run test to verify it fails (or passes by EF convention)**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests/Umbral.OperacionesSesion.IntegrationTests.csproj --filter "FullyQualifiedName~Persists_lifecycle_state_after_start_and_advance"`
Expected: PASS — EF Core maps `FechaInicio`/`FechaFin`/`Estado` by convention on InMemory even without explicit column names. (The explicit mappings + migration in Steps 3–4 are required for **Postgres** column names, not for this InMemory assertion. This test is the regression guard that the lifecycle survives a round-trip.)

- [ ] **Step 3: Add explicit column mappings** to `OperacionesSesionDbContext.cs`.

In the `SesionPartida` entity block, after the `MaximosParticipacion` property line, add:
```csharp
            entity.Property(x => x.FechaInicio).HasColumnName("fechainicio");
            entity.Property(x => x.FechaFin).HasColumnName("fechafin");
```

In the `JuegoResumen` entity block, after the `TipoJuego` property line, add:
```csharp
            entity.Property(x => x.Estado).HasColumnName("estadojuego").IsRequired();
```

- [ ] **Step 4: Generate the migration**

Run (from repo root):
```bash
dotnet tool restore --tool-manifest services/operaciones-sesion/.config/dotnet-tools.json
dotnet ef migrations add SP3bInicioSecuenciacion \
  --project services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure \
  --startup-project services/operaciones-sesion/src/Umbral.OperacionesSesion.Api \
  --output-dir Persistence/Migrations
```
Expected: a new `<timestamp>_SP3bInicioSecuenciacion.cs` under `Persistence/Migrations`, the updated `ModelSnapshot`, and a successful build. The `Up()` should `AddColumn` `fechainicio`, `fechafin` (nullable timestamps) and `estadojuego` (int, not null, default 0).

- [ ] **Step 5: Verify the migration content + re-run the test**

Run:
```bash
grep -E "fechainicio|fechafin|estadojuego" services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Persistence/Migrations/*_SP3bInicioSecuenciacion.cs
dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests/Umbral.OperacionesSesion.IntegrationTests.csproj --filter "FullyQualifiedName~SesionPersistenceTests"
```
Expected: the three column names appear in the migration; integration tests PASS.

- [ ] **Step 6: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests
git commit -m "SP-3b: persistencia (estadojuego + fechainicio/fechafin) + migración EF"
```

---

### Task 10: Api — controller endpoints + middleware arms + unit tests

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Controllers/SesionesController.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Middleware/ExceptionHandlingMiddleware.cs`
- Modify (test): `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/SesionesControllerTests.cs`
- Modify (test): `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/ExceptionHandlingMiddlewareTests.cs`

**Interfaces:**
- Consumes: `IniciarPartidaCommand`, `IntentarInicioAutomaticoCommand`, `FinalizarJuegoActualCommand`, `ObtenerEstadoSesionQuery`; `InicioPartidaResponse`, `AvanceJuegoResponse`, `EstadoSesionDto`; `ModoInicioNoCompatibleException`, `SesionNoIniciadaException`; `FakeSender`.
- Produces: 4 endpoints on `SesionesController`; two new 409 arms in the middleware.

- [ ] **Step 1: Write the failing controller tests** — append to `SesionesControllerTests.cs` (inside the class):

```csharp
    [Fact]
    public async Task Iniciar_returns_200_and_dispatches_command()
    {
        var partidaId = Guid.NewGuid();
        var sender = new FakeSender(new InicioPartidaResponse(partidaId, "Iniciada", Guid.NewGuid(), 1));
        var controller = ControllerWith(sender);

        var result = await controller.Iniciar(partidaId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        var command = Assert.IsType<IniciarPartidaCommand>(sender.LastRequest);
        Assert.Equal(partidaId, command.PartidaId);
    }

    [Fact]
    public async Task IniciarAutomatico_returns_200_and_dispatches_command()
    {
        var partidaId = Guid.NewGuid();
        var sender = new FakeSender(new InicioPartidaResponse(partidaId, "Lobby", null, null));
        var controller = ControllerWith(sender);

        var result = await controller.IniciarAutomatico(partidaId, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.IsType<IntentarInicioAutomaticoCommand>(sender.LastRequest);
    }

    [Fact]
    public async Task FinalizarJuegoActual_returns_200_and_dispatches_command()
    {
        var partidaId = Guid.NewGuid();
        var sender = new FakeSender(new AvanceJuegoResponse(partidaId, "Iniciada", 1, 2, false));
        var controller = ControllerWith(sender);

        var result = await controller.FinalizarJuegoActual(partidaId, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.IsType<FinalizarJuegoActualCommand>(sender.LastRequest);
    }

    [Fact]
    public async Task ObtenerEstado_returns_200_and_dispatches_query()
    {
        var partidaId = Guid.NewGuid();
        var sender = new FakeSender(new EstadoSesionDto(partidaId, Guid.NewGuid(), "Lobby", "Individual", Array.Empty<JuegoEstadoDto>(), null));
        var controller = ControllerWith(sender);

        var result = await controller.ObtenerEstado(partidaId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<EstadoSesionDto>(ok.Value);
        Assert.IsType<ObtenerEstadoSesionQuery>(sender.LastRequest);
    }
```

(Add `using Umbral.OperacionesSesion.Application.Queries;` to the test file's usings if not present.)

- [ ] **Step 2: Write the failing middleware tests** — append to `ExceptionHandlingMiddlewareTests.cs`:

```csharp
    [Fact]
    public async Task Maps_modo_inicio_no_compatible_to_409()
        => Assert.Equal((int)HttpStatusCode.Conflict, await StatusFor(new ModoInicioNoCompatibleException(Guid.NewGuid())));

    [Fact]
    public async Task Maps_sesion_no_iniciada_to_409()
        => Assert.Equal((int)HttpStatusCode.Conflict, await StatusFor(new SesionNoIniciadaException(Guid.NewGuid())));
```

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj --filter "FullyQualifiedName~SesionesControllerTests|FullyQualifiedName~ExceptionHandlingMiddlewareTests"`
Expected: FAIL — controller actions + exception arms do not exist.

- [ ] **Step 4: Add the controller endpoints** to `SesionesController.cs`.

Add `using Umbral.OperacionesSesion.Application.Queries;` is already present. Add these actions before the `private Guid ObtenerParticipanteId()` method:

```csharp
    [HttpPost("partidas/{partidaId:guid}/inicio")]
    public async Task<IActionResult> Iniciar(Guid partidaId, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new IniciarPartidaCommand(partidaId), cancellationToken));

    [HttpPost("partidas/{partidaId:guid}/inicio-automatico")]
    public async Task<IActionResult> IniciarAutomatico(Guid partidaId, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new IntentarInicioAutomaticoCommand(partidaId), cancellationToken));

    [HttpPost("partidas/{partidaId:guid}/juego-actual/finalizacion")]
    public async Task<IActionResult> FinalizarJuegoActual(Guid partidaId, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new FinalizarJuegoActualCommand(partidaId), cancellationToken));

    [HttpGet("partidas/{partidaId:guid}/estado")]
    public async Task<IActionResult> ObtenerEstado(Guid partidaId, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new ObtenerEstadoSesionQuery(partidaId), cancellationToken));
```

- [ ] **Step 5: Add the middleware arms** to `ExceptionHandlingMiddleware.cs`.

Add `using` (already imports `Umbral.OperacionesSesion.Domain.Exceptions`). Extend the 409 group in `MapStatus` so it reads:

```csharp
        SesionYaPublicadaException
            or PartidaNoPublicableException
            or SesionNoEnLobbyException
            or ModalidadNoSoportadaException
            or ParticipanteYaInscritoException
            or ParticipacionActivaExistenteException
            or CupoLlenoException
            or ModoInicioNoCompatibleException
            or SesionNoIniciadaException => HttpStatusCode.Conflict,
```

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj --filter "FullyQualifiedName~SesionesControllerTests|FullyQualifiedName~ExceptionHandlingMiddlewareTests"`
Expected: PASS (controller: 11 total; middleware: 11 total).

- [ ] **Step 7: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Api services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api
git commit -m "SP-3b: endpoints de inicio/avance/estado + arms de excepción 409 + tests de controller/middleware"
```

---

### Task 11: Contract tests — full lifecycle end-to-end

**Files:**
- Modify: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/SesionEndpointsTests.cs`

**Interfaces:**
- Consumes: `OperacionesSesionWebFactory` (+ `Stub`, `CreateClientAs`), the real DI graph on InMemory, all SP-3b endpoints, `InicioPartidaResponse`, `AvanceJuegoResponse`, `EstadoSesionDto`.
- Produces: end-to-end contract coverage of the start/sequencing lifecycle.

- [ ] **Step 1: Write the failing tests** — append to `SesionEndpointsTests.cs` (inside the class). The existing `Config(...)` helper defaults to `ModoInicioPartida = "Manual"`, `min = 1`, so a single inscription meets minimums.

```csharp
    private async Task PublishAndInscribe(Guid partidaId, Guid participanteId, int juegos)
    {
        _factory.Stub.Respuestas[partidaId] = Config(modalidad: "Individual", max: 10, juegos: juegos);
        Assert.Equal(HttpStatusCode.Created, (await _client.PostAsync($"/partidas/{partidaId}/publicacion", null)).StatusCode);
        var authClient = _factory.CreateClientAs(participanteId);
        Assert.Equal(HttpStatusCode.Created, (await authClient.PostAsync($"/partidas/{partidaId}/inscripciones", null)).StatusCode);
    }

    [Fact]
    public async Task Start_then_advance_runs_full_lifecycle_to_terminada()
    {
        var partidaId = Guid.NewGuid();
        await PublishAndInscribe(partidaId, Guid.NewGuid(), juegos: 2);

        var start = await _client.PostAsync($"/partidas/{partidaId}/inicio", null);
        Assert.Equal(HttpStatusCode.OK, start.StatusCode);
        var inicio = await start.Content.ReadFromJsonAsync<InicioPartidaResponse>();
        Assert.Equal("Iniciada", inicio!.Estado);
        Assert.Equal(1, inicio.JuegoActivadoOrden);

        var estado1 = await _client.GetFromJsonAsync<EstadoSesionDto>($"/partidas/{partidaId}/estado");
        Assert.Equal("Iniciada", estado1!.Estado);
        Assert.Equal(1, estado1.JuegoActualOrden);
        Assert.Equal("Activo", estado1.Juegos.Single(j => j.Orden == 1).Estado);

        var avance1 = await (await _client.PostAsync($"/partidas/{partidaId}/juego-actual/finalizacion", null)).Content.ReadFromJsonAsync<AvanceJuegoResponse>();
        Assert.False(avance1!.Terminada);
        Assert.Equal(2, avance1.JuegoActivadoOrden);

        var avance2 = await (await _client.PostAsync($"/partidas/{partidaId}/juego-actual/finalizacion", null)).Content.ReadFromJsonAsync<AvanceJuegoResponse>();
        Assert.True(avance2!.Terminada);
        Assert.Equal("Terminada", avance2.Estado);

        var estadoFinal = await _client.GetFromJsonAsync<EstadoSesionDto>($"/partidas/{partidaId}/estado");
        Assert.Equal("Terminada", estadoFinal!.Estado);
        Assert.Null(estadoFinal.JuegoActualOrden);
    }

    [Fact]
    public async Task Start_with_minimums_not_met_auto_cancels()
    {
        var partidaId = Guid.NewGuid();
        // min defaults to 1 in Config; publish a partida with min=2 by overriding the stub config.
        _factory.Stub.Respuestas[partidaId] = new ConfiguracionPartidaDto("Copa", "Individual", "Manual", null, 2, 10,
            new List<JuegoResumenDto> { new(Guid.NewGuid(), 1, "Trivia") });
        Assert.Equal(HttpStatusCode.Created, (await _client.PostAsync($"/partidas/{partidaId}/publicacion", null)).StatusCode);
        // no inscriptions → 0 < 2

        var start = await _client.PostAsync($"/partidas/{partidaId}/inicio", null);
        Assert.Equal(HttpStatusCode.OK, start.StatusCode);
        var inicio = await start.Content.ReadFromJsonAsync<InicioPartidaResponse>();
        Assert.Equal("Cancelada", inicio!.Estado);
    }

    [Fact]
    public async Task Start_when_not_in_lobby_returns_409()
    {
        var partidaId = Guid.NewGuid();
        await PublishAndInscribe(partidaId, Guid.NewGuid(), juegos: 1);
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsync($"/partidas/{partidaId}/inicio", null)).StatusCode);

        var second = await _client.PostAsync($"/partidas/{partidaId}/inicio", null);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode); // already Iniciada → SesionNoEnLobby
    }

    [Fact]
    public async Task Automatic_start_not_due_is_noop_lobby()
    {
        var partidaId = Guid.NewGuid();
        // Automatic mode, TiempoInicio in the far future → not due.
        _factory.Stub.Respuestas[partidaId] = new ConfiguracionPartidaDto("Copa", "Individual", "Automatico",
            new DateTime(2099, 1, 1, 0, 0, 0, DateTimeKind.Utc), 1, 10,
            new List<JuegoResumenDto> { new(Guid.NewGuid(), 1, "Trivia") });
        Assert.Equal(HttpStatusCode.Created, (await _client.PostAsync($"/partidas/{partidaId}/publicacion", null)).StatusCode);

        var auto = await _client.PostAsync($"/partidas/{partidaId}/inicio-automatico", null);
        Assert.Equal(HttpStatusCode.OK, auto.StatusCode);
        var inicio = await auto.Content.ReadFromJsonAsync<InicioPartidaResponse>();
        Assert.Equal("Lobby", inicio!.Estado); // not due → no-op
    }

    [Fact]
    public async Task Finalizar_when_not_iniciada_returns_409()
    {
        var partidaId = Guid.NewGuid();
        _factory.Stub.Respuestas[partidaId] = Config(juegos: 1);
        Assert.Equal(HttpStatusCode.Created, (await _client.PostAsync($"/partidas/{partidaId}/publicacion", null)).StatusCode);
        // still in Lobby (never started)

        var finalizar = await _client.PostAsync($"/partidas/{partidaId}/juego-actual/finalizacion", null);
        Assert.Equal(HttpStatusCode.Conflict, finalizar.StatusCode);
    }

    [Fact]
    public async Task Estado_for_unknown_partida_returns_404()
    {
        var response = await _client.GetAsync($"/partidas/{Guid.NewGuid()}/estado");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/Umbral.OperacionesSesion.ContractTests.csproj --filter "FullyQualifiedName~SesionEndpointsTests"`
Expected: FAIL — new endpoints not reachable until built (compile errors on new response types / methods). If they compile but fail, the assertions drive the fixes; all production code already exists by Task 10, so the expected end state is PASS once the test file compiles. Run and confirm: if the run shows FAIL only because of a pre-build of the SUT, ensure Tasks 1–10 are committed, then re-run.

- [ ] **Step 3: No production changes needed**

All endpoints + handlers + persistence exist from Tasks 1–10. This task only adds coverage. If a test reveals a genuine defect, fix it in the owning file and note it in the commit.

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/Umbral.OperacionesSesion.ContractTests.csproj --filter "FullyQualifiedName~SesionEndpointsTests"`
Expected: PASS (12 existing SP-3a + 7 new = 19 contract tests).

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests
git commit -m "SP-3b: contract tests del ciclo de vida completo (inicio→avance→Terminada, auto no-op, 409s)"
```

---

### Task 12: Contracts docs + traceability + R1 structural gate

**Files:**
- Modify: `contracts/http/operaciones-sesion-api.md`
- Modify: `contracts/events/operaciones-sesion-events.md`
- Modify: `docs/04-sdd/traceability-matrix.md`

**Interfaces:**
- Consumes: the implemented endpoints + event payloads (source of truth must match code).
- Produces: updated contracts + traceability; verified R1 gate.

- [ ] **Step 1: Register the 4 endpoints** in `contracts/http/operaciones-sesion-api.md` — add to the Endpoint Registry table:

```markdown
| Start a partida (manual) | POST | `/operaciones-sesion/partidas/{partidaId}/inicio` | Operador | 200 + InicioPartidaResponse | 404 sesión no existe · 409 no en Lobby / modo incompatible |
| Start a partida (automatic, idempotent) | POST | `/operaciones-sesion/partidas/{partidaId}/inicio-automatico` | Operador/Sistema | 200 + InicioPartidaResponse | 404 sesión no existe · 409 modo incompatible |
| Finalize current game (advance) | POST | `/operaciones-sesion/partidas/{partidaId}/juego-actual/finalizacion` | Operador | 200 + AvanceJuegoResponse | 404 sesión no existe · 409 no iniciada |
| Session state | GET | `/operaciones-sesion/partidas/{partidaId}/estado` | Operador/Participante | 200 + EstadoSesionDto | 404 sesión no existe |
```

And add to the DTOs section:
```markdown
- `InicioPartidaResponse { partidaId, estado, juegoActivadoId?, juegoActivadoOrden? }` (estado ∈ {Iniciada, Cancelada, Lobby}; Lobby = automatic no-op)
- `AvanceJuegoResponse { partidaId, estado, juegoFinalizadoOrden?, juegoActivadoOrden?, terminada }`
- `EstadoSesionDto { partidaId, sesionPartidaId, estado, modalidad, juegos[]{ juegoId, orden, tipoJuego, estado }, juegoActualOrden? }`

Notes: start/advance return 200 (state transition, not resource creation). Minimums not met on start is a valid `200 + estado=Cancelada` outcome (not a 4xx). `/inicio-automatico` is idempotent: not in Lobby or before `TiempoInicio` → no-op `200` with the current estado.
```

- [ ] **Step 2: Register the event payloads** in `contracts/events/operaciones-sesion-events.md`.

Update the Event Registry rows for `PartidaIniciada`, `JuegoActivado`, `PartidaFinalizada` to `Payload registered (SP-3b)`, and add a new row:
```markdown
| `PartidaCancelada` | A partida is auto-cancelled when minimums are not met at start. | Defined by SDD | Payload registered (SP-3b) |
```

Add to the "Payloads (registered)" section:
```markdown
### `PartidaIniciada` (SP-3b)

Emitted after a partida starts (manual or automatic). No-Op port in SP-3b.

```json
{ "partidaId": "guid", "sesionPartidaId": "guid", "fechaInicio": "datetime", "primerJuegoId": "guid", "primerJuegoOrden": 1 }
```

### `JuegoActivado` (SP-3b)

Emitted when a game becomes active — at start (first game) and on each sequential advance.

```json
{ "partidaId": "guid", "sesionPartidaId": "guid", "juegoId": "guid", "orden": 1, "tipoJuego": "Trivia | BusquedaDelTesoro" }
```

### `PartidaFinalizada` (SP-3b)

Emitted when the last game finishes and the partida reaches Terminada. The consolidated ranking is computed by Puntuaciones in SP-4; this payload only signals finish.

```json
{ "partidaId": "guid", "sesionPartidaId": "guid", "fechaFin": "datetime" }
```

### `PartidaCancelada` (SP-3b)

Emitted when a partida is auto-cancelled at start because participation minimums were not met.

```json
{ "partidaId": "guid", "sesionPartidaId": "guid", "motivo": "MinimosNoAlcanzados", "fechaCancelacion": "datetime" }
```
```

- [ ] **Step 3: Update the traceability matrix** — append an SP-3b row to `docs/04-sdd/traceability-matrix.md`:

```markdown
| Inicio (manual/automático) + secuenciación de juegos (SP-3b) | Iniciar una `SesionPartida` publicada (manual + automático idempotente time-gated), chequeo de mínimos → auto-cancelación, activación secuencial de juegos (`EstadoJuego` Pendiente→Activo→Finalizado) hasta `Terminada`; eventos por puerto No-Op | Operaciones de Sesión | — (Puntuaciones consume eventos en SP-4) | docs/superpowers/specs/2026-06-26-sp3b-inicio-secuenciacion-design.md · docs/superpowers/plans/2026-06-26-sp3b-inicio-secuenciacion.md | contracts/http/operaciones-sesion-api.md · contracts/events/operaciones-sesion-events.md | Implemented — full suite verde; R1 gate passed; eventos PartidaIniciada/JuegoActivado/PartidaFinalizada/PartidaCancelada (No-Op). **Diferido:** gameplay Trivia/BDT→3c/3d (disparo real de FinalizarJuegoActual), scheduler del automático→3f, reconexión→3e, SignalR→3f, Equipo→3a-E. |
```

- [ ] **Step 4: Verify the R1 structural gate**

Run:
```bash
ls services/operaciones-sesion/src/Umbral.OperacionesSesion.Application
# Expect exactly: Commands Queries Interfaces Validators DTOs Handlers Exceptions DependencyInjection.cs ValidationBehavior.cs (+ obj)
ls services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers
# Expect: Commands Queries
ls services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure
# Expect: Persistence Services DependencyInjection.cs (+ obj)
grep -n "MapControllers\|MapGet\|MapPost\|MapPut\|MapDelete" services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Program.cs
# Expect: only MapControllers (zero minimal-API route registrations)
git -C . status --short
# Expect: only the intended SP-3b files (+ pre-existing GUIA-USO-AGENTE.md), nothing under other services
```
Expected: graded folder set intact; repo interfaces still in `Domain/Abstractions/Persistence/`; `Program.cs` has only `MapControllers`; no other service touched.

- [ ] **Step 5: Run the full suite (green gate)**

Run: `dotnet test services/operaciones-sesion/Umbral.OperacionesSesion.sln`
Expected: PASS — all projects green. Target after SP-3b: unit (~59 + ~31 new), integration (~4), contract (~19). Record the exact totals in the commit message.

- [ ] **Step 6: Commit**

```bash
git add contracts/http/operaciones-sesion-api.md contracts/events/operaciones-sesion-events.md docs/04-sdd/traceability-matrix.md
git commit -m "SP-3b: contratos HTTP/eventos registrados + traceability + R1 gate verificado"
```

---

## Self-Review

**1. Spec coverage** (each spec section → task):
- §3 `EstadoJuego` + `JuegoResumen.Estado` → Task 1. ✓
- §4.3 `Iniciar` + `FechaInicio/FechaFin` → Task 2; `IntentarInicioAutomatico` → Task 3; `FinalizarJuegoActual` → Task 4. ✓
- §4.4 `ModoInicioNoCompatible` → Task 2; `SesionNoIniciada` → Task 4. ✓
- §5 commands/queries/handlers/DTOs/validators/events → Tasks 5–8. ✓
- §6 persistence + migration → Task 9. ✓
- §7 endpoints → Task 10. ✓
- §8 middleware arms → Task 10. ✓
- §9 contracts → Task 12. ✓
- §10 micro-decisions (explicit handler publish, own-state minimums, auto-cancel=200, no manual cancel, 200-not-201, PartidaCancelada new, auto idempotency) → enforced across Tasks 5–10. ✓
- §11 R1 gate → Task 12. ✓
- §12 testing (domain/handlers/validators/controller/middleware/contract) → Tasks 1–11. ✓

**2. Placeholder scan:** No TBD/TODO/"add error handling"/"similar to". Every code step has complete code. ✓

**3. Type consistency:** `ResultadoInicio.{Iniciada,Cancelada,NoCorresponde}` + `TipoResultadoInicio`; `ResultadoAvance.{Avanzado,Terminada}` + `Terminada()` method; `InicioPartidaResponse(PartidaId, Estado, JuegoActivadoId?, JuegoActivadoOrden?)`; `AvanceJuegoResponse(PartidaId, Estado, JuegoFinalizadoOrden?, JuegoActivadoOrden?, Terminada)`; `EstadoSesionDto(... Juegos, JuegoActualOrden?)` + `JuegoEstadoDto(JuegoId, Orden, TipoJuego, Estado)`; event records `PartidaIniciadaEvent`/`JuegoActivadoEvent`/`PartidaFinalizadaEvent`/`PartidaCanceladaEvent`; publisher methods `PublicarPartidaIniciadaAsync`/`PublicarJuegoActivadoAsync`/`PublicarPartidaFinalizadaAsync`/`PublicarPartidaCanceladaAsync`. Handler ctor arg order `(repo, uow, events, timeProvider)` consistent across Tasks 5/6/7. `IniciarPartidaCommandHandler.PublicarEventosInicioAsync`/`MapearInicio` reused in Task 6. ✓

**Notes for the executor:**
- MediatR auto-discovers handlers/validators by assembly scan (`AddOperacionesSesionApplication`) — no manual registration needed for new commands/queries.
- `FakeSesionEventsPublisher` is edited twice (Task 5 reduced set, Task 7 adds finalizada members). Use the **reduced** version in Task 5.
- Time is faked with a hand-rolled `Fakes/FakeTimeProvider.cs` (Task 5 Step 1b) — a `TimeProvider` subclass returning a fixed `GetUtcNow()`. **No NuGet package is added** (SP-3a used `TimeProvider.System`; the testing package is not referenced). Tasks 6 and 7 reuse this fake.
