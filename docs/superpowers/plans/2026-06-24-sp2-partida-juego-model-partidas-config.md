# SP-2 — Partida/Juego Model + Partidas (config) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the doctrinal `Partida` → `Juego` domain plus Trivia/BDT configuration in the Partidas microservice (DB `umbral_partidas`), with an incremental create/config API, review queries, contracts and tests.

**Architecture:** Clean Architecture + CQRS/MediatR in `services/partidas` (namespaces `Umbral.Partidas.*`). Three aggregate roots — `Partida` (holds ordered game references), `JuegoTrivia` (owns `Pregunta`/`Opcion`), `JuegoBDT` (owns `EtapaBDT`) — each with its own repository, all persisted in the shared `PartidasDbContext`. A unit-of-work commits the two aggregates that `AgregarJuego` mutates in one transaction. EF Core + Npgsql for production, EF InMemory provider for tests (SP-0 fallback pattern). Canonical structural reference: `services/identity-service` (graded-compliant after SP-1R).

**Tech Stack:** .NET 8, MediatR 12.2.0, FluentValidation 11.11.0, EF Core 8.0.7 (+ InMemory + Npgsql), xUnit 2.5.3, Microsoft.AspNetCore.Mvc.Testing 8.0.7.

## Global Constraints

- **Namespaces:** every file under `Umbral.Partidas.{Domain,Application,Infrastructure,Api}` (note: the slug is `partidas`, the namespace root is `Umbral.Partidas`).
- **Graded structure (non-negotiable, per CLAUDE.md):** `Application/` contains exactly `Commands/`, `Queries/`, `Interfaces/`, `Validators/`, `DTOs/`, `Handlers/`, `Handlers/Commands/`, `Handlers/Queries/`, `Exceptions/`. Repository interfaces live in `Domain/Abstractions/Persistence/` (mirrors identity-service). `Infrastructure/` contains `Persistence/` and `Services/`. Controllers live in `Api/Controllers/`, inherit native `ControllerBase`, dispatch only via `_mediator.Send(...)`, contain no business logic. Every controller has unit tests.
- **Value Objects (full, per approved spec + user decision):** `PartidaId`, `JuegoId`, `NombrePartida`, `PuntajeAsignado` are value objects each exposing `EsValido()`. EF maps them with `HasConversion` (VO ⇄ column). Child identifiers (`PreguntaId`, `OpcionId`, `EtapaBDTId`) stay raw `Guid` (the class diagram does not model them as VOs).
- **SEAM:** `Partida.Estado` is `EstadoPartida?` (nullable). `null` = configured but not yet published. SP-3 sets `Lobby` on publish. Do NOT add a synthetic `Borrador` enum value (it does not exist in the doctrine).
- **DB:** `umbral_partidas`. Connection-string name `PartidasDatabase`. When that connection string is empty, the DbContext falls back to EF InMemory (shell pattern) — this is what every test relies on; do not remove it.
- **No edits to old services:** do NOT touch `services/trivia-game-service` or `services/bdt-game-service` in SP-2. They keep running for SP-3/SP-4. Their `[Obsolete]` tagging/retirement is SP-5.
- **No integration events:** internal domain behaviour only. No RabbitMQ publishing in SP-2.
- **Commits:** small and frequent (one per task minimum). Run `dotnet test "services/partidas/Umbral.Partidas.sln"` green before each commit that closes a task.

## File Structure

**Domain** (`src/Umbral.Partidas.Domain/`):
- `ValueObjects/{PartidaId,JuegoId,NombrePartida,PuntajeAsignado}.cs`
- `Enums/{Modalidad,ModoInicioPartida,TipoJuego,EstadoJuego,EstadoPartida}.cs`
- `Entities/{Partida,JuegoReferencia,JuegoTrivia,Pregunta,Opcion,JuegoBDT,EtapaBDT}.cs`
- `Exceptions/*.cs` (domain invariant violations)
- `Abstractions/Persistence/{IPartidaRepository,IJuegoTriviaRepository,IJuegoBDTRepository,IPartidasUnitOfWork}.cs`

**Application** (`src/Umbral.Partidas.Application/`):
- `Commands/{CrearPartidaCommand,AgregarJuegoTriviaCommand,AgregarJuegoBDTCommand}.cs`
- `Queries/{GetPartidaByIdQuery,ListPartidasQuery}.cs`
- `Handlers/Commands/*Handler.cs`, `Handlers/Queries/*Handler.cs`
- `Validators/*Validator.cs`
- `DTOs/*.cs` (requests, responses, detail/summary DTOs)
- `Exceptions/{PartidaNoEncontradaException}.cs`
- `Interfaces/` (empty for SP-2 unless a clock port is needed; created only if used)

**Infrastructure** (`src/Umbral.Partidas.Infrastructure/`):
- `Persistence/PartidasDbContext.cs` (modify), `Persistence/{PartidaRepository,JuegoTriviaRepository,JuegoBDTRepository,PartidasUnitOfWork}.cs`
- `Persistence/Migrations/*` (generated)
- `DependencyInjection.cs` (modify)

**Api** (`src/Umbral.Partidas.Api/`):
- `Controllers/PartidasController.cs`, `Api/Contracts/*.cs` (request records if extracted)

**Tests:**
- `tests/Umbral.Partidas.UnitTests/` — domain invariants, application handlers/validators (hand-rolled fakes), controller unit tests
- `tests/Umbral.Partidas.IntegrationTests/` — EF InMemory repository round-trips, unit-of-work atomicity
- `tests/Umbral.Partidas.ContractTests/` — `WebApplicationFactory<Program>` endpoint/contract shape tests

**Contracts:** `contracts/http/partidas-config.md`

---

### Task 1: Domain value objects + enums

**Files:**
- Create: `services/partidas/src/Umbral.Partidas.Domain/ValueObjects/PartidaId.cs`
- Create: `services/partidas/src/Umbral.Partidas.Domain/ValueObjects/JuegoId.cs`
- Create: `services/partidas/src/Umbral.Partidas.Domain/ValueObjects/NombrePartida.cs`
- Create: `services/partidas/src/Umbral.Partidas.Domain/ValueObjects/PuntajeAsignado.cs`
- Create: `services/partidas/src/Umbral.Partidas.Domain/Enums/Modalidad.cs`
- Create: `services/partidas/src/Umbral.Partidas.Domain/Enums/ModoInicioPartida.cs`
- Create: `services/partidas/src/Umbral.Partidas.Domain/Enums/TipoJuego.cs`
- Create: `services/partidas/src/Umbral.Partidas.Domain/Enums/EstadoJuego.cs`
- Create: `services/partidas/src/Umbral.Partidas.Domain/Enums/EstadoPartida.cs`
- Test: `services/partidas/tests/Umbral.Partidas.UnitTests/Domain/ValueObjectTests.cs`

**Interfaces:**
- Produces: `PartidaId` (`readonly record struct`, `Guid Valor`, `New()`, `From(Guid)`, `EsValido()`); `JuegoId` (same shape); `NombrePartida` (`sealed record`, `string Valor`, `const int LongitudMaxima = 120`, `Crear(string)`, `EsValido()`); `PuntajeAsignado` (`readonly record struct`, `int Valor`, `Crear(int)`, `EsValido()`); enums `Modalidad{Individual,Equipo}`, `ModoInicioPartida{Manual,Automatico,ManualYAutomatico}`, `TipoJuego{Trivia,BusquedaDelTesoro}`, `EstadoJuego{Pendiente,Activo,Finalizado}`, `EstadoPartida{Lobby,Iniciada,Cancelada,Terminada}`.

- [ ] **Step 1: Write the failing tests**

```csharp
// services/partidas/tests/Umbral.Partidas.UnitTests/Domain/ValueObjectTests.cs
using System;
using Umbral.Partidas.Domain.ValueObjects;

namespace Umbral.Partidas.UnitTests.Domain;

public class ValueObjectTests
{
    [Fact]
    public void PartidaId_New_is_valid_and_nonempty()
    {
        var id = PartidaId.New();
        Assert.True(id.EsValido());
        Assert.NotEqual(Guid.Empty, id.Valor);
    }

    [Fact]
    public void PartidaId_From_empty_guid_is_invalid()
    {
        Assert.False(PartidaId.From(Guid.Empty).EsValido());
    }

    [Fact]
    public void JuegoId_New_is_valid()
    {
        Assert.True(JuegoId.New().EsValido());
    }

    [Fact]
    public void NombrePartida_trims_and_accepts_valid_value()
    {
        var nombre = NombrePartida.Crear("  Copa UMBRAL  ");
        Assert.Equal("Copa UMBRAL", nombre.Valor);
        Assert.True(nombre.EsValido());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void NombrePartida_rejects_blank(string value)
    {
        Assert.Throws<ArgumentException>(() => NombrePartida.Crear(value));
    }

    [Fact]
    public void NombrePartida_rejects_over_max_length()
    {
        var tooLong = new string('x', NombrePartida.LongitudMaxima + 1);
        Assert.Throws<ArgumentException>(() => NombrePartida.Crear(tooLong));
    }

    [Fact]
    public void PuntajeAsignado_accepts_positive()
    {
        var p = PuntajeAsignado.Crear(10);
        Assert.Equal(10, p.Valor);
        Assert.True(p.EsValido());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void PuntajeAsignado_rejects_non_positive(int value)
    {
        Assert.Throws<ArgumentException>(() => PuntajeAsignado.Crear(value));
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test "services/partidas/tests/Umbral.Partidas.UnitTests/Umbral.Partidas.UnitTests.csproj" --filter "FullyQualifiedName~ValueObjectTests"`
Expected: FAIL — build error, types `PartidaId`/`JuegoId`/`NombrePartida`/`PuntajeAsignado` do not exist.

- [ ] **Step 3: Implement the value objects**

```csharp
// ValueObjects/PartidaId.cs
namespace Umbral.Partidas.Domain.ValueObjects;

public readonly record struct PartidaId(Guid Valor)
{
    public static PartidaId New() => new(Guid.NewGuid());
    public static PartidaId From(Guid valor) => new(valor);
    public bool EsValido() => Valor != Guid.Empty;
}
```

```csharp
// ValueObjects/JuegoId.cs
namespace Umbral.Partidas.Domain.ValueObjects;

public readonly record struct JuegoId(Guid Valor)
{
    public static JuegoId New() => new(Guid.NewGuid());
    public static JuegoId From(Guid valor) => new(valor);
    public bool EsValido() => Valor != Guid.Empty;
}
```

```csharp
// ValueObjects/NombrePartida.cs
namespace Umbral.Partidas.Domain.ValueObjects;

public sealed record NombrePartida
{
    public const int LongitudMaxima = 120;

    public string Valor { get; }

    private NombrePartida(string valor) => Valor = valor;

    public static NombrePartida Crear(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new ArgumentException("NombrePartida es requerido.", nameof(valor));

        var trimmed = valor.Trim();
        if (trimmed.Length > LongitudMaxima)
            throw new ArgumentException($"NombrePartida no puede exceder {LongitudMaxima} caracteres.", nameof(valor));

        return new NombrePartida(trimmed);
    }

    public bool EsValido() => !string.IsNullOrWhiteSpace(Valor) && Valor.Length <= LongitudMaxima;
}
```

```csharp
// ValueObjects/PuntajeAsignado.cs
namespace Umbral.Partidas.Domain.ValueObjects;

public readonly record struct PuntajeAsignado
{
    public int Valor { get; }

    private PuntajeAsignado(int valor) => Valor = valor;

    public static PuntajeAsignado Crear(int valor)
    {
        if (valor <= 0)
            throw new ArgumentException("PuntajeAsignado debe ser positivo.", nameof(valor));

        return new PuntajeAsignado(valor);
    }

    public bool EsValido() => Valor > 0;
}
```

- [ ] **Step 4: Implement the enums**

```csharp
// Enums/Modalidad.cs
namespace Umbral.Partidas.Domain.Enums;
public enum Modalidad { Individual, Equipo }
```

```csharp
// Enums/ModoInicioPartida.cs
namespace Umbral.Partidas.Domain.Enums;
public enum ModoInicioPartida { Manual, Automatico, ManualYAutomatico }
```

```csharp
// Enums/TipoJuego.cs
namespace Umbral.Partidas.Domain.Enums;
public enum TipoJuego { Trivia, BusquedaDelTesoro }
```

```csharp
// Enums/EstadoJuego.cs
namespace Umbral.Partidas.Domain.Enums;
public enum EstadoJuego { Pendiente, Activo, Finalizado }
```

```csharp
// Enums/EstadoPartida.cs
namespace Umbral.Partidas.Domain.Enums;
public enum EstadoPartida { Lobby, Iniciada, Cancelada, Terminada }
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test "services/partidas/tests/Umbral.Partidas.UnitTests/Umbral.Partidas.UnitTests.csproj" --filter "FullyQualifiedName~ValueObjectTests"`
Expected: PASS (11 tests).

- [ ] **Step 6: Commit**

```bash
git add services/partidas/src/Umbral.Partidas.Domain services/partidas/tests/Umbral.Partidas.UnitTests/Domain/ValueObjectTests.cs
git commit -m "feat(partidas): add Partida domain value objects and enums"
```

---

### Task 2: `Partida` aggregate root

**Files:**
- Create: `services/partidas/src/Umbral.Partidas.Domain/Entities/JuegoReferencia.cs`
- Create: `services/partidas/src/Umbral.Partidas.Domain/Entities/Partida.cs`
- Create: `services/partidas/src/Umbral.Partidas.Domain/Exceptions/JuegoDuplicadoException.cs`
- Create: `services/partidas/src/Umbral.Partidas.Domain/Exceptions/OrdenJuegoDuplicadoException.cs`
- Create: `services/partidas/src/Umbral.Partidas.Domain/Exceptions/PartidaSinJuegosException.cs`
- Create: `services/partidas/src/Umbral.Partidas.Domain/Exceptions/OrdenJuegosNoContiguoException.cs`
- Test: `services/partidas/tests/Umbral.Partidas.UnitTests/Domain/PartidaTests.cs`

**Interfaces:**
- Consumes: `PartidaId`, `JuegoId`, `NombrePartida`, `Modalidad`, `ModoInicioPartida`, `TipoJuego`, `EstadoPartida` (Task 1).
- Produces:
  - `JuegoReferencia` (`JuegoId JuegoId`, `int Orden`, `TipoJuego TipoJuego`; `internal` ctor).
  - `Partida` with: `PartidaId PartidaId`, `NombrePartida NombrePartida`, `EstadoPartida? Estado`, `Modalidad Modalidad`, `ModoInicioPartida ModoInicioPartida`, `DateTime? TiempoInicio`, `int MinimosParticipacion`, `int MaximosParticipacion`, `IReadOnlyList<JuegoReferencia> Juegos`.
  - `static Partida Crear(NombrePartida nombre, Modalidad modalidad, ModoInicioPartida modo, DateTime? tiempoInicio, int minimos, int maximos)`.
  - `void AgregarJuego(JuegoId juegoId, int orden, TipoJuego tipoJuego)`.
  - `void ValidarListaParaPublicar()`.

- [ ] **Step 1: Write the failing tests**

```csharp
// services/partidas/tests/Umbral.Partidas.UnitTests/Domain/PartidaTests.cs
using System;
using Umbral.Partidas.Domain.Entities;
using Umbral.Partidas.Domain.Enums;
using Umbral.Partidas.Domain.Exceptions;
using Umbral.Partidas.Domain.ValueObjects;

namespace Umbral.Partidas.UnitTests.Domain;

public class PartidaTests
{
    private static Partida CrearManual() =>
        Partida.Crear(NombrePartida.Crear("Copa"), Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10);

    [Fact]
    public void Crear_manual_sets_null_estado_and_no_games()
    {
        var partida = CrearManual();
        Assert.Null(partida.Estado);          // SEAM: not published yet
        Assert.True(partida.PartidaId.EsValido());
        Assert.Empty(partida.Juegos);
    }

    [Fact]
    public void Crear_automatico_requires_tiempo_inicio()
    {
        Assert.Throws<ArgumentException>(() =>
            Partida.Crear(NombrePartida.Crear("Copa"), Modalidad.Individual, ModoInicioPartida.Automatico, null, 1, 10));
    }

    [Fact]
    public void Crear_manual_rejects_tiempo_inicio()
    {
        Assert.Throws<ArgumentException>(() =>
            Partida.Crear(NombrePartida.Crear("Copa"), Modalidad.Individual, ModoInicioPartida.Manual, DateTime.UtcNow, 1, 10));
    }

    [Fact]
    public void Crear_rejects_maximos_below_minimos()
    {
        Assert.Throws<ArgumentException>(() =>
            Partida.Crear(NombrePartida.Crear("Copa"), Modalidad.Individual, ModoInicioPartida.Manual, null, 5, 2));
    }

    [Fact]
    public void AgregarJuego_appends_reference()
    {
        var partida = CrearManual();
        var juegoId = JuegoId.New();
        partida.AgregarJuego(juegoId, 1, TipoJuego.Trivia);
        Assert.Single(partida.Juegos);
        Assert.Equal(juegoId, partida.Juegos[0].JuegoId);
        Assert.Equal(TipoJuego.Trivia, partida.Juegos[0].TipoJuego);
    }

    [Fact]
    public void AgregarJuego_rejects_duplicate_orden()
    {
        var partida = CrearManual();
        partida.AgregarJuego(JuegoId.New(), 1, TipoJuego.Trivia);
        Assert.Throws<OrdenJuegoDuplicadoException>(() => partida.AgregarJuego(JuegoId.New(), 1, TipoJuego.BusquedaDelTesoro));
    }

    [Fact]
    public void AgregarJuego_rejects_duplicate_juego_id()
    {
        var partida = CrearManual();
        var juegoId = JuegoId.New();
        partida.AgregarJuego(juegoId, 1, TipoJuego.Trivia);
        Assert.Throws<JuegoDuplicadoException>(() => partida.AgregarJuego(juegoId, 2, TipoJuego.Trivia));
    }

    [Fact]
    public void ValidarListaParaPublicar_throws_when_no_games()
    {
        Assert.Throws<PartidaSinJuegosException>(() => CrearManual().ValidarListaParaPublicar());
    }

    [Fact]
    public void ValidarListaParaPublicar_throws_when_orden_not_contiguous()
    {
        var partida = CrearManual();
        partida.AgregarJuego(JuegoId.New(), 1, TipoJuego.Trivia);
        partida.AgregarJuego(JuegoId.New(), 3, TipoJuego.Trivia); // gap
        Assert.Throws<OrdenJuegosNoContiguoException>(() => partida.ValidarListaParaPublicar());
    }

    [Fact]
    public void ValidarListaParaPublicar_passes_for_contiguous_orden()
    {
        var partida = CrearManual();
        partida.AgregarJuego(JuegoId.New(), 1, TipoJuego.Trivia);
        partida.AgregarJuego(JuegoId.New(), 2, TipoJuego.BusquedaDelTesoro);
        partida.ValidarListaParaPublicar(); // no throw
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test "services/partidas/tests/Umbral.Partidas.UnitTests/Umbral.Partidas.UnitTests.csproj" --filter "FullyQualifiedName~PartidaTests"`
Expected: FAIL — `Partida`, `JuegoReferencia`, exception types do not exist.

- [ ] **Step 3: Implement the domain exceptions**

```csharp
// Exceptions/JuegoDuplicadoException.cs
namespace Umbral.Partidas.Domain.Exceptions;
public sealed class JuegoDuplicadoException : Exception
{
    public JuegoDuplicadoException(Guid juegoId)
        : base($"El juego {juegoId} ya pertenece a la partida.") { }
}
```

```csharp
// Exceptions/OrdenJuegoDuplicadoException.cs
namespace Umbral.Partidas.Domain.Exceptions;
public sealed class OrdenJuegoDuplicadoException : Exception
{
    public OrdenJuegoDuplicadoException(int orden)
        : base($"Ya existe un juego con el orden {orden} en la partida.") { }
}
```

```csharp
// Exceptions/PartidaSinJuegosException.cs
namespace Umbral.Partidas.Domain.Exceptions;
public sealed class PartidaSinJuegosException : Exception
{
    public PartidaSinJuegosException(Guid partidaId)
        : base($"La partida {partidaId} no tiene juegos; no puede publicarse.") { }
}
```

```csharp
// Exceptions/OrdenJuegosNoContiguoException.cs
namespace Umbral.Partidas.Domain.Exceptions;
public sealed class OrdenJuegosNoContiguoException : Exception
{
    public OrdenJuegosNoContiguoException(Guid partidaId)
        : base($"El orden de los juegos de la partida {partidaId} debe ser una secuencia contigua desde 1.") { }
}
```

- [ ] **Step 4: Implement `JuegoReferencia`**

```csharp
// Entities/JuegoReferencia.cs
using Umbral.Partidas.Domain.Enums;
using Umbral.Partidas.Domain.ValueObjects;

namespace Umbral.Partidas.Domain.Entities;

public sealed class JuegoReferencia
{
    public JuegoId JuegoId { get; private set; }
    public int Orden { get; private set; }
    public TipoJuego TipoJuego { get; private set; }

    private JuegoReferencia() { } // EF

    internal JuegoReferencia(JuegoId juegoId, int orden, TipoJuego tipoJuego)
    {
        JuegoId = juegoId;
        Orden = orden;
        TipoJuego = tipoJuego;
    }
}
```

- [ ] **Step 5: Implement `Partida`**

```csharp
// Entities/Partida.cs
using Umbral.Partidas.Domain.Enums;
using Umbral.Partidas.Domain.Exceptions;
using Umbral.Partidas.Domain.ValueObjects;

namespace Umbral.Partidas.Domain.Entities;

public sealed class Partida
{
    private readonly List<JuegoReferencia> _juegos = new();

    public PartidaId PartidaId { get; private set; }
    public NombrePartida NombrePartida { get; private set; } = null!;
    public EstadoPartida? Estado { get; private set; }      // null = configured, not yet published (SP-3 sets Lobby)
    public Modalidad Modalidad { get; private set; }
    public ModoInicioPartida ModoInicioPartida { get; private set; }
    public DateTime? TiempoInicio { get; private set; }
    public int MinimosParticipacion { get; private set; }
    public int MaximosParticipacion { get; private set; }

    public IReadOnlyList<JuegoReferencia> Juegos => _juegos;

    private Partida() { } // EF

    private Partida(
        NombrePartida nombre,
        Modalidad modalidad,
        ModoInicioPartida modo,
        DateTime? tiempoInicio,
        int minimos,
        int maximos)
    {
        PartidaId = PartidaId.New();
        NombrePartida = nombre;
        Modalidad = modalidad;
        ModoInicioPartida = modo;
        TiempoInicio = tiempoInicio;
        MinimosParticipacion = minimos;
        MaximosParticipacion = maximos;
        Estado = null;

        ValidarParametrosParticipacion();
        ValidarParametrosInicio();
    }

    public static Partida Crear(
        NombrePartida nombre,
        Modalidad modalidad,
        ModoInicioPartida modo,
        DateTime? tiempoInicio,
        int minimos,
        int maximos)
        => new(nombre, modalidad, modo, tiempoInicio, minimos, maximos);

    public void AgregarJuego(JuegoId juegoId, int orden, TipoJuego tipoJuego)
    {
        if (!juegoId.EsValido())
            throw new ArgumentException("JuegoId invalido.", nameof(juegoId));
        if (orden < 1)
            throw new ArgumentException("El orden debe ser mayor o igual a 1.", nameof(orden));
        if (_juegos.Any(j => j.JuegoId == juegoId))
            throw new JuegoDuplicadoException(juegoId.Valor);
        if (_juegos.Any(j => j.Orden == orden))
            throw new OrdenJuegoDuplicadoException(orden);

        _juegos.Add(new JuegoReferencia(juegoId, orden, tipoJuego));
    }

    public void ValidarListaParaPublicar()
    {
        if (_juegos.Count == 0)
            throw new PartidaSinJuegosException(PartidaId.Valor);

        var ordenes = _juegos.Select(j => j.Orden).OrderBy(o => o).ToList();
        for (var i = 0; i < ordenes.Count; i++)
        {
            if (ordenes[i] != i + 1)
                throw new OrdenJuegosNoContiguoException(PartidaId.Valor);
        }
    }

    private void ValidarParametrosParticipacion()
    {
        if (MinimosParticipacion < 1)
            throw new ArgumentException("MinimosParticipacion debe ser mayor o igual a 1.");
        if (MaximosParticipacion < MinimosParticipacion)
            throw new ArgumentException("MaximosParticipacion debe ser mayor o igual a MinimosParticipacion.");
    }

    private void ValidarParametrosInicio()
    {
        var requiereTiempo = ModoInicioPartida is ModoInicioPartida.Automatico or ModoInicioPartida.ManualYAutomatico;
        if (requiereTiempo && TiempoInicio is null)
            throw new ArgumentException("TiempoInicio es requerido para inicio Automatico o ManualYAutomatico.");
        if (!requiereTiempo && TiempoInicio is not null)
            throw new ArgumentException("TiempoInicio no aplica para inicio Manual.");
    }
}
```

- [ ] **Step 6: Run the tests to verify they pass**

Run: `dotnet test "services/partidas/tests/Umbral.Partidas.UnitTests/Umbral.Partidas.UnitTests.csproj" --filter "FullyQualifiedName~PartidaTests"`
Expected: PASS (11 tests).

- [ ] **Step 7: Commit**

```bash
git add services/partidas/src/Umbral.Partidas.Domain services/partidas/tests/Umbral.Partidas.UnitTests/Domain/PartidaTests.cs
git commit -m "feat(partidas): add Partida aggregate root with ordered game references"
```

---

### Task 3: `JuegoTrivia` aggregate (JuegoTrivia + Pregunta + Opcion)

**Files:**
- Create: `services/partidas/src/Umbral.Partidas.Domain/Entities/Opcion.cs`
- Create: `services/partidas/src/Umbral.Partidas.Domain/Entities/Pregunta.cs`
- Create: `services/partidas/src/Umbral.Partidas.Domain/Entities/JuegoTrivia.cs`
- Create: `services/partidas/src/Umbral.Partidas.Domain/Exceptions/JuegoTriviaSinPreguntasException.cs`
- Create: `services/partidas/src/Umbral.Partidas.Domain/Exceptions/PreguntaInvalidaException.cs`
- Test: `services/partidas/tests/Umbral.Partidas.UnitTests/Domain/JuegoTriviaTests.cs`

**Interfaces:**
- Consumes: `JuegoId`, `PartidaId`, `PuntajeAsignado`, `EstadoJuego` (Task 1).
- Produces:
  - `Opcion` (`Guid OpcionId`, `string Texto`, `bool EsCorrecta`; `internal static Opcion Crear(string texto, bool esCorrecta)`).
  - `Pregunta` (`Guid PreguntaId`, `string Texto`, `IReadOnlyList<Opcion> Opciones`, `PuntajeAsignado PuntajeAsignado`, `int TiempoLimiteSegundos`; `internal static Pregunta Crear(string texto, IEnumerable<(string Texto, bool EsCorrecta)> opciones, int puntaje, int tiempoLimiteSegundos)`).
  - `JuegoTrivia` (`JuegoId JuegoId`, `PartidaId PartidaId`, `int Orden`, `EstadoJuego Estado`, `IReadOnlyList<Pregunta> Preguntas`; `static JuegoTrivia Crear(PartidaId partidaId, int orden, IEnumerable<PreguntaSpec> preguntas)`; `void AgregarPregunta(string texto, IEnumerable<(string Texto, bool EsCorrecta)> opciones, int puntaje, int tiempoLimiteSegundos)`).
  - `PreguntaSpec` record: `record PreguntaSpec(string Texto, IReadOnlyList<OpcionSpec> Opciones, int Puntaje, int TiempoLimiteSegundos)`, `record OpcionSpec(string Texto, bool EsCorrecta)` — the application layer (Task 6) constructs these from its command.

- [ ] **Step 1: Write the failing tests**

```csharp
// services/partidas/tests/Umbral.Partidas.UnitTests/Domain/JuegoTriviaTests.cs
using System;
using System.Collections.Generic;
using System.Linq;
using Umbral.Partidas.Domain.Entities;
using Umbral.Partidas.Domain.Enums;
using Umbral.Partidas.Domain.Exceptions;
using Umbral.Partidas.Domain.ValueObjects;

namespace Umbral.Partidas.UnitTests.Domain;

public class JuegoTriviaTests
{
    private static PreguntaSpec ValidPregunta(string texto = "Capital de Francia?") =>
        new(texto,
            new List<OpcionSpec> { new("Paris", true), new("Londres", false) },
            10, 30);

    [Fact]
    public void Crear_builds_game_with_questions_and_pendiente_state()
    {
        var juego = JuegoTrivia.Crear(PartidaId.New(), 1, new[] { ValidPregunta() });
        Assert.True(juego.JuegoId.EsValido());
        Assert.Equal(EstadoJuego.Pendiente, juego.Estado);
        Assert.Single(juego.Preguntas);
        Assert.Equal(2, juego.Preguntas[0].Opciones.Count);
        Assert.Equal(10, juego.Preguntas[0].PuntajeAsignado.Valor);
    }

    [Fact]
    public void Crear_rejects_empty_question_list()
    {
        Assert.Throws<JuegoTriviaSinPreguntasException>(() =>
            JuegoTrivia.Crear(PartidaId.New(), 1, Enumerable.Empty<PreguntaSpec>()));
    }

    [Fact]
    public void AgregarPregunta_rejects_blank_text()
    {
        var juego = JuegoTrivia.Crear(PartidaId.New(), 1, new[] { ValidPregunta() });
        Assert.Throws<PreguntaInvalidaException>(() =>
            juego.AgregarPregunta("  ", new[] { ("A", true), ("B", false) }, 10, 30));
    }

    [Fact]
    public void AgregarPregunta_rejects_less_than_two_options()
    {
        var juego = JuegoTrivia.Crear(PartidaId.New(), 1, new[] { ValidPregunta() });
        Assert.Throws<PreguntaInvalidaException>(() =>
            juego.AgregarPregunta("Q", new[] { ("only", true) }, 10, 30));
    }

    [Fact]
    public void AgregarPregunta_rejects_not_exactly_one_correct()
    {
        var juego = JuegoTrivia.Crear(PartidaId.New(), 1, new[] { ValidPregunta() });
        Assert.Throws<PreguntaInvalidaException>(() =>
            juego.AgregarPregunta("Q", new[] { ("A", true), ("B", true) }, 10, 30));
        Assert.Throws<PreguntaInvalidaException>(() =>
            juego.AgregarPregunta("Q", new[] { ("A", false), ("B", false) }, 10, 30));
    }

    [Fact]
    public void AgregarPregunta_rejects_non_positive_time_limit()
    {
        var juego = JuegoTrivia.Crear(PartidaId.New(), 1, new[] { ValidPregunta() });
        Assert.Throws<PreguntaInvalidaException>(() =>
            juego.AgregarPregunta("Q", new[] { ("A", true), ("B", false) }, 10, 0));
    }

    [Fact]
    public void AgregarPregunta_rejects_non_positive_puntaje()
    {
        var juego = JuegoTrivia.Crear(PartidaId.New(), 1, new[] { ValidPregunta() });
        Assert.Throws<PreguntaInvalidaException>(() =>
            juego.AgregarPregunta("Q", new[] { ("A", true), ("B", false) }, 0, 30));
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test "services/partidas/tests/Umbral.Partidas.UnitTests/Umbral.Partidas.UnitTests.csproj" --filter "FullyQualifiedName~JuegoTriviaTests"`
Expected: FAIL — `JuegoTrivia`, `Pregunta`, `Opcion`, `PreguntaSpec`, exceptions do not exist.

- [ ] **Step 3: Implement the domain exceptions**

```csharp
// Exceptions/JuegoTriviaSinPreguntasException.cs
namespace Umbral.Partidas.Domain.Exceptions;
public sealed class JuegoTriviaSinPreguntasException : Exception
{
    public JuegoTriviaSinPreguntasException()
        : base("Un JuegoTrivia debe tener al menos una pregunta.") { }
}
```

```csharp
// Exceptions/PreguntaInvalidaException.cs
namespace Umbral.Partidas.Domain.Exceptions;
public sealed class PreguntaInvalidaException : Exception
{
    public PreguntaInvalidaException(string motivo)
        : base($"Pregunta invalida: {motivo}") { }
}
```

- [ ] **Step 4: Implement `Opcion`**

```csharp
// Entities/Opcion.cs
namespace Umbral.Partidas.Domain.Entities;

public sealed class Opcion
{
    public Guid OpcionId { get; private set; }
    public string Texto { get; private set; } = string.Empty;
    public bool EsCorrecta { get; private set; }

    private Opcion() { } // EF

    internal static Opcion Crear(string texto, bool esCorrecta)
    {
        return new Opcion
        {
            OpcionId = Guid.NewGuid(),
            Texto = texto.Trim(),
            EsCorrecta = esCorrecta
        };
    }
}
```

- [ ] **Step 5: Implement `Pregunta`**

```csharp
// Entities/Pregunta.cs
using Umbral.Partidas.Domain.Exceptions;
using Umbral.Partidas.Domain.ValueObjects;

namespace Umbral.Partidas.Domain.Entities;

public sealed class Pregunta
{
    private readonly List<Opcion> _opciones = new();

    public Guid PreguntaId { get; private set; }
    public string Texto { get; private set; } = string.Empty;
    public PuntajeAsignado PuntajeAsignado { get; private set; }
    public int TiempoLimiteSegundos { get; private set; }

    public IReadOnlyList<Opcion> Opciones => _opciones;

    private Pregunta() { } // EF

    internal static Pregunta Crear(
        string texto,
        IEnumerable<(string Texto, bool EsCorrecta)> opciones,
        int puntaje,
        int tiempoLimiteSegundos)
    {
        if (string.IsNullOrWhiteSpace(texto))
            throw new PreguntaInvalidaException("el texto es requerido.");

        var opcionesList = opciones?.ToList() ?? new List<(string, bool)>();
        if (opcionesList.Count < 2)
            throw new PreguntaInvalidaException("se requieren al menos 2 opciones.");
        if (opcionesList.Any(o => string.IsNullOrWhiteSpace(o.Item1)))
            throw new PreguntaInvalidaException("el texto de cada opcion es requerido.");
        if (opcionesList.Count(o => o.Item2) != 1)
            throw new PreguntaInvalidaException("debe haber exactamente una opcion correcta.");
        if (tiempoLimiteSegundos <= 0)
            throw new PreguntaInvalidaException("el tiempo limite debe ser positivo.");

        PuntajeAsignado puntajeVo;
        try
        {
            puntajeVo = PuntajeAsignado.Crear(puntaje);
        }
        catch (ArgumentException ex)
        {
            throw new PreguntaInvalidaException(ex.Message);
        }

        var pregunta = new Pregunta
        {
            PreguntaId = Guid.NewGuid(),
            Texto = texto.Trim(),
            PuntajeAsignado = puntajeVo,
            TiempoLimiteSegundos = tiempoLimiteSegundos
        };
        foreach (var (opcionTexto, esCorrecta) in opcionesList)
            pregunta._opciones.Add(Opcion.Crear(opcionTexto, esCorrecta));

        return pregunta;
    }
}
```

- [ ] **Step 6: Implement `JuegoTrivia` and `PreguntaSpec`**

```csharp
// Entities/JuegoTrivia.cs
using Umbral.Partidas.Domain.Enums;
using Umbral.Partidas.Domain.Exceptions;
using Umbral.Partidas.Domain.ValueObjects;

namespace Umbral.Partidas.Domain.Entities;

public sealed record OpcionSpec(string Texto, bool EsCorrecta);

public sealed record PreguntaSpec(
    string Texto,
    IReadOnlyList<OpcionSpec> Opciones,
    int Puntaje,
    int TiempoLimiteSegundos);

public sealed class JuegoTrivia
{
    private readonly List<Pregunta> _preguntas = new();

    public JuegoId JuegoId { get; private set; }
    public PartidaId PartidaId { get; private set; }
    public int Orden { get; private set; }
    public EstadoJuego Estado { get; private set; }

    public IReadOnlyList<Pregunta> Preguntas => _preguntas;

    private JuegoTrivia() { } // EF

    private JuegoTrivia(PartidaId partidaId, int orden)
    {
        JuegoId = JuegoId.New();
        PartidaId = partidaId;
        Orden = orden;
        Estado = EstadoJuego.Pendiente;
    }

    public static JuegoTrivia Crear(PartidaId partidaId, int orden, IEnumerable<PreguntaSpec> preguntas)
    {
        var juego = new JuegoTrivia(partidaId, orden);
        foreach (var p in preguntas ?? Enumerable.Empty<PreguntaSpec>())
        {
            juego.AgregarPregunta(
                p.Texto,
                p.Opciones.Select(o => (o.Texto, o.EsCorrecta)),
                p.Puntaje,
                p.TiempoLimiteSegundos);
        }

        if (juego._preguntas.Count == 0)
            throw new JuegoTriviaSinPreguntasException();

        return juego;
    }

    public void AgregarPregunta(
        string texto,
        IEnumerable<(string Texto, bool EsCorrecta)> opciones,
        int puntaje,
        int tiempoLimiteSegundos)
    {
        _preguntas.Add(Pregunta.Crear(texto, opciones, puntaje, tiempoLimiteSegundos));
    }
}
```

- [ ] **Step 7: Run the tests to verify they pass**

Run: `dotnet test "services/partidas/tests/Umbral.Partidas.UnitTests/Umbral.Partidas.UnitTests.csproj" --filter "FullyQualifiedName~JuegoTriviaTests"`
Expected: PASS (8 tests).

- [ ] **Step 8: Commit**

```bash
git add services/partidas/src/Umbral.Partidas.Domain services/partidas/tests/Umbral.Partidas.UnitTests/Domain/JuegoTriviaTests.cs
git commit -m "feat(partidas): add JuegoTrivia aggregate with questions and options"
```

---

### Task 4: `JuegoBDT` aggregate (JuegoBDT + EtapaBDT)

**Files:**
- Create: `services/partidas/src/Umbral.Partidas.Domain/Entities/EtapaBDT.cs`
- Create: `services/partidas/src/Umbral.Partidas.Domain/Entities/JuegoBDT.cs`
- Create: `services/partidas/src/Umbral.Partidas.Domain/Exceptions/JuegoBDTSinEtapasException.cs`
- Create: `services/partidas/src/Umbral.Partidas.Domain/Exceptions/EtapaBDTInvalidaException.cs`
- Create: `services/partidas/src/Umbral.Partidas.Domain/Exceptions/AreaBusquedaRequeridaException.cs`
- Test: `services/partidas/tests/Umbral.Partidas.UnitTests/Domain/JuegoBDTTests.cs`

**Interfaces:**
- Consumes: `JuegoId`, `PartidaId`, `PuntajeAsignado`, `EstadoJuego` (Task 1).
- Produces:
  - `EtapaBDT` (`Guid EtapaBDTId`, `int Orden`, `string CodigoQREsperado`, `PuntajeAsignado PuntajeAsignado`, `int TiempoLimiteSegundos`; `internal static EtapaBDT Crear(int orden, string codigoQr, int puntaje, int tiempoLimiteSegundos)`).
  - `JuegoBDT` (`JuegoId JuegoId`, `PartidaId PartidaId`, `int Orden`, `EstadoJuego Estado`, `string AreaBusqueda`, `IReadOnlyList<EtapaBDT> Etapas`; `static JuegoBDT Crear(PartidaId partidaId, int orden, string areaBusqueda, IEnumerable<EtapaSpec> etapas)`; `void AgregarEtapa(int orden, string codigoQr, int puntaje, int tiempoLimiteSegundos)`).
  - `EtapaSpec` record: `record EtapaSpec(int Orden, string CodigoQREsperado, int Puntaje, int TiempoLimiteSegundos)`.

- [ ] **Step 1: Write the failing tests**

```csharp
// services/partidas/tests/Umbral.Partidas.UnitTests/Domain/JuegoBDTTests.cs
using System;
using System.Collections.Generic;
using System.Linq;
using Umbral.Partidas.Domain.Entities;
using Umbral.Partidas.Domain.Enums;
using Umbral.Partidas.Domain.Exceptions;
using Umbral.Partidas.Domain.ValueObjects;

namespace Umbral.Partidas.UnitTests.Domain;

public class JuegoBDTTests
{
    private static EtapaSpec Etapa(int orden, string qr = "QR-TEXT") => new(orden, qr, 50, 120);

    private static JuegoBDT CrearValido() =>
        JuegoBDT.Crear(PartidaId.New(), 1, "Plaza central", new[] { Etapa(1) });

    [Fact]
    public void Crear_builds_game_with_stages_and_pendiente_state()
    {
        var juego = CrearValido();
        Assert.True(juego.JuegoId.EsValido());
        Assert.Equal(EstadoJuego.Pendiente, juego.Estado);
        Assert.Equal("Plaza central", juego.AreaBusqueda);
        Assert.Single(juego.Etapas);
        Assert.Equal(50, juego.Etapas[0].PuntajeAsignado.Valor);
        Assert.Equal("QR-TEXT", juego.Etapas[0].CodigoQREsperado);
    }

    [Fact]
    public void Crear_rejects_blank_area_busqueda()
    {
        Assert.Throws<AreaBusquedaRequeridaException>(() =>
            JuegoBDT.Crear(PartidaId.New(), 1, "  ", new[] { Etapa(1) }));
    }

    [Fact]
    public void Crear_rejects_empty_stage_list()
    {
        Assert.Throws<JuegoBDTSinEtapasException>(() =>
            JuegoBDT.Crear(PartidaId.New(), 1, "Plaza", Enumerable.Empty<EtapaSpec>()));
    }

    [Fact]
    public void AgregarEtapa_rejects_blank_codigo_qr()
    {
        var juego = CrearValido();
        Assert.Throws<EtapaBDTInvalidaException>(() => juego.AgregarEtapa(2, "  ", 50, 120));
    }

    [Fact]
    public void AgregarEtapa_rejects_non_positive_puntaje()
    {
        var juego = CrearValido();
        Assert.Throws<EtapaBDTInvalidaException>(() => juego.AgregarEtapa(2, "QR", 0, 120));
    }

    [Fact]
    public void AgregarEtapa_rejects_non_positive_time_limit()
    {
        var juego = CrearValido();
        Assert.Throws<EtapaBDTInvalidaException>(() => juego.AgregarEtapa(2, "QR", 50, 0));
    }

    [Fact]
    public void Crear_rejects_non_contiguous_stage_orden()
    {
        Assert.Throws<EtapaBDTInvalidaException>(() =>
            JuegoBDT.Crear(PartidaId.New(), 1, "Plaza", new[] { Etapa(1), Etapa(3) }));
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test "services/partidas/tests/Umbral.Partidas.UnitTests/Umbral.Partidas.UnitTests.csproj" --filter "FullyQualifiedName~JuegoBDTTests"`
Expected: FAIL — `JuegoBDT`, `EtapaBDT`, `EtapaSpec`, exceptions do not exist.

- [ ] **Step 3: Implement the domain exceptions**

```csharp
// Exceptions/JuegoBDTSinEtapasException.cs
namespace Umbral.Partidas.Domain.Exceptions;
public sealed class JuegoBDTSinEtapasException : Exception
{
    public JuegoBDTSinEtapasException()
        : base("Un JuegoBDT debe tener al menos una etapa.") { }
}
```

```csharp
// Exceptions/EtapaBDTInvalidaException.cs
namespace Umbral.Partidas.Domain.Exceptions;
public sealed class EtapaBDTInvalidaException : Exception
{
    public EtapaBDTInvalidaException(string motivo)
        : base($"Etapa BDT invalida: {motivo}") { }
}
```

```csharp
// Exceptions/AreaBusquedaRequeridaException.cs
namespace Umbral.Partidas.Domain.Exceptions;
public sealed class AreaBusquedaRequeridaException : Exception
{
    public AreaBusquedaRequeridaException()
        : base("El area de busqueda es requerida para un JuegoBDT.") { }
}
```

- [ ] **Step 4: Implement `EtapaBDT`**

```csharp
// Entities/EtapaBDT.cs
using Umbral.Partidas.Domain.Exceptions;
using Umbral.Partidas.Domain.ValueObjects;

namespace Umbral.Partidas.Domain.Entities;

public sealed class EtapaBDT
{
    public Guid EtapaBDTId { get; private set; }
    public int Orden { get; private set; }
    public string CodigoQREsperado { get; private set; } = string.Empty;
    public PuntajeAsignado PuntajeAsignado { get; private set; }
    public int TiempoLimiteSegundos { get; private set; }

    private EtapaBDT() { } // EF

    internal static EtapaBDT Crear(int orden, string codigoQr, int puntaje, int tiempoLimiteSegundos)
    {
        if (orden < 1)
            throw new EtapaBDTInvalidaException("el orden debe ser mayor o igual a 1.");
        if (string.IsNullOrWhiteSpace(codigoQr))
            throw new EtapaBDTInvalidaException("el codigo QR esperado es requerido.");
        if (tiempoLimiteSegundos <= 0)
            throw new EtapaBDTInvalidaException("el tiempo limite debe ser positivo.");

        PuntajeAsignado puntajeVo;
        try
        {
            puntajeVo = PuntajeAsignado.Crear(puntaje);
        }
        catch (ArgumentException ex)
        {
            throw new EtapaBDTInvalidaException(ex.Message);
        }

        return new EtapaBDT
        {
            EtapaBDTId = Guid.NewGuid(),
            Orden = orden,
            CodigoQREsperado = codigoQr.Trim(),
            PuntajeAsignado = puntajeVo,
            TiempoLimiteSegundos = tiempoLimiteSegundos
        };
    }
}
```

- [ ] **Step 5: Implement `JuegoBDT` and `EtapaSpec`**

```csharp
// Entities/JuegoBDT.cs
using Umbral.Partidas.Domain.Enums;
using Umbral.Partidas.Domain.Exceptions;
using Umbral.Partidas.Domain.ValueObjects;

namespace Umbral.Partidas.Domain.Entities;

public sealed record EtapaSpec(int Orden, string CodigoQREsperado, int Puntaje, int TiempoLimiteSegundos);

public sealed class JuegoBDT
{
    private readonly List<EtapaBDT> _etapas = new();

    public JuegoId JuegoId { get; private set; }
    public PartidaId PartidaId { get; private set; }
    public int Orden { get; private set; }
    public EstadoJuego Estado { get; private set; }
    public string AreaBusqueda { get; private set; } = string.Empty;

    public IReadOnlyList<EtapaBDT> Etapas => _etapas;

    private JuegoBDT() { } // EF

    private JuegoBDT(PartidaId partidaId, int orden, string areaBusqueda)
    {
        JuegoId = JuegoId.New();
        PartidaId = partidaId;
        Orden = orden;
        Estado = EstadoJuego.Pendiente;
        AreaBusqueda = areaBusqueda.Trim();
    }

    public static JuegoBDT Crear(PartidaId partidaId, int orden, string areaBusqueda, IEnumerable<EtapaSpec> etapas)
    {
        if (string.IsNullOrWhiteSpace(areaBusqueda))
            throw new AreaBusquedaRequeridaException();

        var juego = new JuegoBDT(partidaId, orden, areaBusqueda);
        foreach (var e in etapas ?? Enumerable.Empty<EtapaSpec>())
            juego.AgregarEtapa(e.Orden, e.CodigoQREsperado, e.Puntaje, e.TiempoLimiteSegundos);

        if (juego._etapas.Count == 0)
            throw new JuegoBDTSinEtapasException();

        juego.ValidarOrdenContiguo();
        return juego;
    }

    public void AgregarEtapa(int orden, string codigoQr, int puntaje, int tiempoLimiteSegundos)
    {
        if (_etapas.Any(e => e.Orden == orden))
            throw new EtapaBDTInvalidaException($"ya existe una etapa con el orden {orden}.");

        _etapas.Add(EtapaBDT.Crear(orden, codigoQr, puntaje, tiempoLimiteSegundos));
    }

    private void ValidarOrdenContiguo()
    {
        var ordenes = _etapas.Select(e => e.Orden).OrderBy(o => o).ToList();
        for (var i = 0; i < ordenes.Count; i++)
        {
            if (ordenes[i] != i + 1)
                throw new EtapaBDTInvalidaException("el orden de las etapas debe ser una secuencia contigua desde 1.");
        }
    }
}
```

- [ ] **Step 6: Run the tests to verify they pass**

Run: `dotnet test "services/partidas/tests/Umbral.Partidas.UnitTests/Umbral.Partidas.UnitTests.csproj" --filter "FullyQualifiedName~JuegoBDTTests"`
Expected: PASS (7 tests).

- [ ] **Step 7: Commit**

```bash
git add services/partidas/src/Umbral.Partidas.Domain services/partidas/tests/Umbral.Partidas.UnitTests/Domain/JuegoBDTTests.cs
git commit -m "feat(partidas): add JuegoBDT aggregate with stages"
```

---

### Task 5: Application — `CrearPartida` (+ repository/unit-of-work interfaces)

**Files:**
- Create: `services/partidas/src/Umbral.Partidas.Domain/Abstractions/Persistence/IPartidaRepository.cs`
- Create: `services/partidas/src/Umbral.Partidas.Domain/Abstractions/Persistence/IPartidasUnitOfWork.cs`
- Create: `services/partidas/src/Umbral.Partidas.Application/Commands/CrearPartidaCommand.cs`
- Create: `services/partidas/src/Umbral.Partidas.Application/DTOs/CrearPartidaResponse.cs`
- Create: `services/partidas/src/Umbral.Partidas.Application/Handlers/Commands/CrearPartidaCommandHandler.cs`
- Create: `services/partidas/src/Umbral.Partidas.Application/Validators/CrearPartidaCommandValidator.cs`
- Create: `services/partidas/tests/Umbral.Partidas.UnitTests/Application/Fakes/FakePartidaRepository.cs`
- Create: `services/partidas/tests/Umbral.Partidas.UnitTests/Application/Fakes/FakePartidasUnitOfWork.cs`
- Test: `services/partidas/tests/Umbral.Partidas.UnitTests/Application/CrearPartidaCommandHandlerTests.cs`
- Test: `services/partidas/tests/Umbral.Partidas.UnitTests/Application/CrearPartidaCommandValidatorTests.cs`

**Interfaces:**
- Consumes: `Partida`, `PartidaId`, `NombrePartida`, `Modalidad`, `ModoInicioPartida` (Tasks 1-2).
- Produces:
  - `IPartidaRepository` — `void Add(Partida partida)`, `void Update(Partida partida)`, `Task<Partida?> GetByIdAsync(PartidaId id, CancellationToken ct)`, `Task<IReadOnlyList<Partida>> ListAsync(CancellationToken ct)`.
  - `IPartidasUnitOfWork` — `Task SaveChangesAsync(CancellationToken ct)`.
  - `CrearPartidaCommand(string NombrePartida, Modalidad Modalidad, ModoInicioPartida ModoInicioPartida, DateTime? TiempoInicio, int MinimosParticipacion, int MaximosParticipacion) : IRequest<CrearPartidaResponse>`.
  - `CrearPartidaResponse(Guid PartidaId)`.
  - `FakePartidaRepository`, `FakePartidasUnitOfWork` (test doubles reused by Tasks 6-8).

- [ ] **Step 1: Create the repository and unit-of-work interfaces**

```csharp
// Domain/Abstractions/Persistence/IPartidaRepository.cs
using Umbral.Partidas.Domain.Entities;
using Umbral.Partidas.Domain.ValueObjects;

namespace Umbral.Partidas.Domain.Abstractions.Persistence;

public interface IPartidaRepository
{
    void Add(Partida partida);
    void Update(Partida partida);
    Task<Partida?> GetByIdAsync(PartidaId id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Partida>> ListAsync(CancellationToken cancellationToken);
}
```

```csharp
// Domain/Abstractions/Persistence/IPartidasUnitOfWork.cs
namespace Umbral.Partidas.Domain.Abstractions.Persistence;

public interface IPartidasUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
```

- [ ] **Step 2: Write the failing handler + validator tests**

```csharp
// tests/Umbral.Partidas.UnitTests/Application/Fakes/FakePartidaRepository.cs
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbral.Partidas.Domain.Abstractions.Persistence;
using Umbral.Partidas.Domain.Entities;
using Umbral.Partidas.Domain.ValueObjects;

namespace Umbral.Partidas.UnitTests.Application.Fakes;

public sealed class FakePartidaRepository : IPartidaRepository
{
    public readonly Dictionary<Guid, Partida> Store = new();

    public void Add(Partida partida) => Store[partida.PartidaId.Valor] = partida;
    public void Update(Partida partida) => Store[partida.PartidaId.Valor] = partida;

    public Task<Partida?> GetByIdAsync(PartidaId id, CancellationToken cancellationToken)
        => Task.FromResult(Store.TryGetValue(id.Valor, out var p) ? p : null);

    public Task<IReadOnlyList<Partida>> ListAsync(CancellationToken cancellationToken)
        => Task.FromResult((IReadOnlyList<Partida>)Store.Values.ToList());
}
```

```csharp
// tests/Umbral.Partidas.UnitTests/Application/Fakes/FakePartidasUnitOfWork.cs
using System.Threading;
using System.Threading.Tasks;
using Umbral.Partidas.Domain.Abstractions.Persistence;

namespace Umbral.Partidas.UnitTests.Application.Fakes;

public sealed class FakePartidasUnitOfWork : IPartidasUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}
```

```csharp
// tests/Umbral.Partidas.UnitTests/Application/CrearPartidaCommandHandlerTests.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using Umbral.Partidas.Application.Commands;
using Umbral.Partidas.Application.Handlers.Commands;
using Umbral.Partidas.Domain.Enums;
using Umbral.Partidas.UnitTests.Application.Fakes;

namespace Umbral.Partidas.UnitTests.Application;

public class CrearPartidaCommandHandlerTests
{
    [Fact]
    public async Task Handle_persists_partida_and_returns_id()
    {
        var repo = new FakePartidaRepository();
        var uow = new FakePartidasUnitOfWork();
        var handler = new CrearPartidaCommandHandler(repo, uow);
        var command = new CrearPartidaCommand("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10);

        var response = await handler.Handle(command, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, response.PartidaId);
        Assert.True(repo.Store.ContainsKey(response.PartidaId));
        Assert.Equal(1, uow.SaveCount);
        Assert.Null(repo.Store[response.PartidaId].Estado); // not published yet
    }
}
```

```csharp
// tests/Umbral.Partidas.UnitTests/Application/CrearPartidaCommandValidatorTests.cs
using System;
using Umbral.Partidas.Application.Commands;
using Umbral.Partidas.Application.Validators;
using Umbral.Partidas.Domain.Enums;

namespace Umbral.Partidas.UnitTests.Application;

public class CrearPartidaCommandValidatorTests
{
    private readonly CrearPartidaCommandValidator _validator = new();

    [Fact]
    public void Valid_manual_command_passes()
    {
        var cmd = new CrearPartidaCommand("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10);
        Assert.True(_validator.Validate(cmd).IsValid);
    }

    [Fact]
    public void Blank_name_fails()
    {
        var cmd = new CrearPartidaCommand("", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10);
        Assert.False(_validator.Validate(cmd).IsValid);
    }

    [Fact]
    public void Maximos_below_minimos_fails()
    {
        var cmd = new CrearPartidaCommand("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 5, 2);
        Assert.False(_validator.Validate(cmd).IsValid);
    }

    [Fact]
    public void Automatico_without_tiempo_inicio_fails()
    {
        var cmd = new CrearPartidaCommand("Copa", Modalidad.Individual, ModoInicioPartida.Automatico, null, 1, 10);
        Assert.False(_validator.Validate(cmd).IsValid);
    }

    [Fact]
    public void Manual_with_tiempo_inicio_fails()
    {
        var cmd = new CrearPartidaCommand("Copa", Modalidad.Individual, ModoInicioPartida.Manual, DateTime.UtcNow, 1, 10);
        Assert.False(_validator.Validate(cmd).IsValid);
    }
}
```

- [ ] **Step 3: Run the tests to verify they fail**

Run: `dotnet test "services/partidas/tests/Umbral.Partidas.UnitTests/Umbral.Partidas.UnitTests.csproj" --filter "FullyQualifiedName~CrearPartida"`
Expected: FAIL — `CrearPartidaCommand`, handler, validator, response do not exist.

- [ ] **Step 4: Implement the command, response, handler and validator**

```csharp
// Application/Commands/CrearPartidaCommand.cs
using MediatR;
using Umbral.Partidas.Application.DTOs;
using Umbral.Partidas.Domain.Enums;

namespace Umbral.Partidas.Application.Commands;

public sealed record CrearPartidaCommand(
    string NombrePartida,
    Modalidad Modalidad,
    ModoInicioPartida ModoInicioPartida,
    DateTime? TiempoInicio,
    int MinimosParticipacion,
    int MaximosParticipacion) : IRequest<CrearPartidaResponse>;
```

```csharp
// Application/DTOs/CrearPartidaResponse.cs
namespace Umbral.Partidas.Application.DTOs;

public sealed record CrearPartidaResponse(Guid PartidaId);
```

```csharp
// Application/Handlers/Commands/CrearPartidaCommandHandler.cs
using MediatR;
using Umbral.Partidas.Application.Commands;
using Umbral.Partidas.Application.DTOs;
using Umbral.Partidas.Domain.Abstractions.Persistence;
using Umbral.Partidas.Domain.Entities;
using Umbral.Partidas.Domain.ValueObjects;

namespace Umbral.Partidas.Application.Handlers.Commands;

public sealed class CrearPartidaCommandHandler : IRequestHandler<CrearPartidaCommand, CrearPartidaResponse>
{
    private readonly IPartidaRepository _partidas;
    private readonly IPartidasUnitOfWork _unitOfWork;

    public CrearPartidaCommandHandler(IPartidaRepository partidas, IPartidasUnitOfWork unitOfWork)
    {
        _partidas = partidas;
        _unitOfWork = unitOfWork;
    }

    public async Task<CrearPartidaResponse> Handle(CrearPartidaCommand request, CancellationToken cancellationToken)
    {
        var partida = Partida.Crear(
            NombrePartida.Crear(request.NombrePartida),
            request.Modalidad,
            request.ModoInicioPartida,
            request.TiempoInicio,
            request.MinimosParticipacion,
            request.MaximosParticipacion);

        _partidas.Add(partida);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CrearPartidaResponse(partida.PartidaId.Valor);
    }
}
```

```csharp
// Application/Validators/CrearPartidaCommandValidator.cs
using FluentValidation;
using Umbral.Partidas.Application.Commands;
using Umbral.Partidas.Domain.Enums;

namespace Umbral.Partidas.Application.Validators;

public sealed class CrearPartidaCommandValidator : AbstractValidator<CrearPartidaCommand>
{
    public CrearPartidaCommandValidator()
    {
        RuleFor(x => x.NombrePartida).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Modalidad).IsInEnum();
        RuleFor(x => x.ModoInicioPartida).IsInEnum();
        RuleFor(x => x.MinimosParticipacion).GreaterThanOrEqualTo(1);
        RuleFor(x => x.MaximosParticipacion).GreaterThanOrEqualTo(x => x.MinimosParticipacion);

        When(x => x.ModoInicioPartida is ModoInicioPartida.Automatico or ModoInicioPartida.ManualYAutomatico, () =>
        {
            RuleFor(x => x.TiempoInicio).NotNull();
        });

        When(x => x.ModoInicioPartida == ModoInicioPartida.Manual, () =>
        {
            RuleFor(x => x.TiempoInicio).Null();
        });
    }
}
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test "services/partidas/tests/Umbral.Partidas.UnitTests/Umbral.Partidas.UnitTests.csproj" --filter "FullyQualifiedName~CrearPartida"`
Expected: PASS (6 tests).

- [ ] **Step 6: Commit**

```bash
git add services/partidas/src/Umbral.Partidas.Domain/Abstractions services/partidas/src/Umbral.Partidas.Application services/partidas/tests/Umbral.Partidas.UnitTests/Application
git commit -m "feat(partidas): add CrearPartida command, handler, validator and repository ports"
```

---

### Task 6: Exception-mapping middleware + `PartidaNoEncontradaException`

**Files:**
- Create: `services/partidas/src/Umbral.Partidas.Application/Exceptions/PartidaNoEncontradaException.cs`
- Modify: `services/partidas/src/Umbral.Partidas.Api/Middleware/ExceptionHandlingMiddleware.cs`
- Test: `services/partidas/tests/Umbral.Partidas.UnitTests/Api/ExceptionHandlingMiddlewareTests.cs`

**Interfaces:**
- Consumes: domain exceptions from Tasks 2-4.
- Produces: `PartidaNoEncontradaException(Guid partidaId)` (Application; consumed by Tasks 7-9). Middleware maps: `PartidaNoEncontradaException`→404; `JuegoDuplicadoException`/`OrdenJuegoDuplicadoException`→409; domain validation exceptions + `ArgumentException`→400; all else→500.

- [ ] **Step 1: Write the failing middleware tests**

```csharp
// tests/Umbral.Partidas.UnitTests/Api/ExceptionHandlingMiddlewareTests.cs
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Umbral.Partidas.Api.Middleware;
using Umbral.Partidas.Application.Exceptions;
using Umbral.Partidas.Domain.Exceptions;

namespace Umbral.Partidas.UnitTests.Api;

public class ExceptionHandlingMiddlewareTests
{
    private static async Task<(int Status, string Body)> InvokeWith(Exception toThrow)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw toThrow,
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        return (context.Response.StatusCode, body);
    }

    [Fact]
    public async Task Maps_partida_no_encontrada_to_404()
    {
        var (status, body) = await InvokeWith(new PartidaNoEncontradaException(Guid.NewGuid()));
        Assert.Equal(404, status);
        Assert.Contains("message", body);
    }

    [Fact]
    public async Task Maps_orden_duplicado_to_409()
    {
        var (status, _) = await InvokeWith(new OrdenJuegoDuplicadoException(1));
        Assert.Equal(409, status);
    }

    [Fact]
    public async Task Maps_juego_duplicado_to_409()
    {
        var (status, _) = await InvokeWith(new JuegoDuplicadoException(Guid.NewGuid()));
        Assert.Equal(409, status);
    }

    [Fact]
    public async Task Maps_pregunta_invalida_to_400()
    {
        var (status, _) = await InvokeWith(new PreguntaInvalidaException("x"));
        Assert.Equal(400, status);
    }

    [Fact]
    public async Task Maps_etapa_invalida_to_400()
    {
        var (status, _) = await InvokeWith(new EtapaBDTInvalidaException("x"));
        Assert.Equal(400, status);
    }

    [Fact]
    public async Task Maps_unknown_to_500()
    {
        var (status, _) = await InvokeWith(new InvalidOperationException("boom"));
        Assert.Equal(500, status);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test "services/partidas/tests/Umbral.Partidas.UnitTests/Umbral.Partidas.UnitTests.csproj" --filter "FullyQualifiedName~ExceptionHandlingMiddlewareTests"`
Expected: FAIL — `PartidaNoEncontradaException` does not exist; current middleware returns 500 for all.

- [ ] **Step 3: Create `PartidaNoEncontradaException`**

```csharp
// Application/Exceptions/PartidaNoEncontradaException.cs
namespace Umbral.Partidas.Application.Exceptions;

public sealed class PartidaNoEncontradaException : Exception
{
    public PartidaNoEncontradaException(Guid partidaId)
        : base($"No existe la partida {partidaId}.") { }
}
```

- [ ] **Step 4: Extend the middleware with status mapping**

```csharp
// Api/Middleware/ExceptionHandlingMiddleware.cs
using System.Net;
using System.Text.Json;
using Umbral.Partidas.Application.Exceptions;
using Umbral.Partidas.Domain.Exceptions;

namespace Umbral.Partidas.Api.Middleware;

// Centralized exception handling with domain/application exception → status mapping (SP-2).
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var status = MapStatus(ex);
            if (status == HttpStatusCode.InternalServerError)
            {
                _logger.LogError(ex, "Unhandled exception.");
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)status;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { message = ex.Message }));
        }
    }

    private static HttpStatusCode MapStatus(Exception ex) => ex switch
    {
        PartidaNoEncontradaException => HttpStatusCode.NotFound,
        JuegoDuplicadoException or OrdenJuegoDuplicadoException => HttpStatusCode.Conflict,
        PreguntaInvalidaException
            or JuegoTriviaSinPreguntasException
            or EtapaBDTInvalidaException
            or JuegoBDTSinEtapasException
            or AreaBusquedaRequeridaException
            or PartidaSinJuegosException
            or OrdenJuegosNoContiguoException
            or ArgumentException => HttpStatusCode.BadRequest,
        _ => HttpStatusCode.InternalServerError
    };
}
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test "services/partidas/tests/Umbral.Partidas.UnitTests/Umbral.Partidas.UnitTests.csproj" --filter "FullyQualifiedName~ExceptionHandlingMiddlewareTests"`
Expected: PASS (6 tests).

- [ ] **Step 6: Commit**

```bash
git add services/partidas/src/Umbral.Partidas.Application/Exceptions services/partidas/src/Umbral.Partidas.Api/Middleware services/partidas/tests/Umbral.Partidas.UnitTests/Api
git commit -m "feat(partidas): map domain/application exceptions to HTTP status codes"
```

---

### Task 7: Application — `AgregarJuegoTrivia`

**Files:**
- Create: `services/partidas/src/Umbral.Partidas.Domain/Abstractions/Persistence/IJuegoTriviaRepository.cs`
- Create: `services/partidas/src/Umbral.Partidas.Application/DTOs/PreguntaRequest.cs`
- Create: `services/partidas/src/Umbral.Partidas.Application/DTOs/AgregarJuegoResponse.cs`
- Create: `services/partidas/src/Umbral.Partidas.Application/Commands/AgregarJuegoTriviaCommand.cs`
- Create: `services/partidas/src/Umbral.Partidas.Application/Handlers/Commands/AgregarJuegoTriviaCommandHandler.cs`
- Create: `services/partidas/src/Umbral.Partidas.Application/Validators/AgregarJuegoTriviaCommandValidator.cs`
- Create: `services/partidas/tests/Umbral.Partidas.UnitTests/Application/Fakes/FakeJuegoTriviaRepository.cs`
- Test: `services/partidas/tests/Umbral.Partidas.UnitTests/Application/AgregarJuegoTriviaCommandHandlerTests.cs`
- Test: `services/partidas/tests/Umbral.Partidas.UnitTests/Application/AgregarJuegoTriviaCommandValidatorTests.cs`

**Interfaces:**
- Consumes: `IPartidaRepository`, `IPartidasUnitOfWork` (Task 5), `PartidaNoEncontradaException` (Task 6), `Partida`, `JuegoTrivia`, `PreguntaSpec`, `OpcionSpec`, `PartidaId`, `TipoJuego` (Tasks 1-3), `FakePartidaRepository`, `FakePartidasUnitOfWork` (Task 5).
- Produces:
  - `IJuegoTriviaRepository` — `void Add(JuegoTrivia juego)`, `Task<IReadOnlyList<JuegoTrivia>> GetByPartidaIdAsync(PartidaId partidaId, CancellationToken ct)`.
  - `OpcionRequest(string Texto, bool EsCorrecta)`, `PreguntaRequest(string Texto, IReadOnlyList<OpcionRequest> Opciones, int Puntaje, int TiempoLimiteSegundos)`.
  - `AgregarJuegoResponse(Guid JuegoId)` (shared by Trivia and BDT add-game commands).
  - `AgregarJuegoTriviaCommand(Guid PartidaId, int Orden, IReadOnlyList<PreguntaRequest> Preguntas) : IRequest<AgregarJuegoResponse>`.
  - `FakeJuegoTriviaRepository` (test double, reused by Task 9).

- [ ] **Step 1: Create `IJuegoTriviaRepository`**

```csharp
// Domain/Abstractions/Persistence/IJuegoTriviaRepository.cs
using Umbral.Partidas.Domain.Entities;
using Umbral.Partidas.Domain.ValueObjects;

namespace Umbral.Partidas.Domain.Abstractions.Persistence;

public interface IJuegoTriviaRepository
{
    void Add(JuegoTrivia juego);
    Task<IReadOnlyList<JuegoTrivia>> GetByPartidaIdAsync(PartidaId partidaId, CancellationToken cancellationToken);
}
```

- [ ] **Step 2: Write the failing handler + validator tests**

```csharp
// tests/Umbral.Partidas.UnitTests/Application/Fakes/FakeJuegoTriviaRepository.cs
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbral.Partidas.Domain.Abstractions.Persistence;
using Umbral.Partidas.Domain.Entities;
using Umbral.Partidas.Domain.ValueObjects;

namespace Umbral.Partidas.UnitTests.Application.Fakes;

public sealed class FakeJuegoTriviaRepository : IJuegoTriviaRepository
{
    public readonly List<JuegoTrivia> Store = new();

    public void Add(JuegoTrivia juego) => Store.Add(juego);

    public Task<IReadOnlyList<JuegoTrivia>> GetByPartidaIdAsync(PartidaId partidaId, CancellationToken cancellationToken)
        => Task.FromResult((IReadOnlyList<JuegoTrivia>)Store.Where(j => j.PartidaId == partidaId).ToList());
}
```

```csharp
// tests/Umbral.Partidas.UnitTests/Application/AgregarJuegoTriviaCommandHandlerTests.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbral.Partidas.Application.Commands;
using Umbral.Partidas.Application.DTOs;
using Umbral.Partidas.Application.Exceptions;
using Umbral.Partidas.Application.Handlers.Commands;
using Umbral.Partidas.Domain.Entities;
using Umbral.Partidas.Domain.Enums;
using Umbral.Partidas.Domain.ValueObjects;
using Umbral.Partidas.UnitTests.Application.Fakes;

namespace Umbral.Partidas.UnitTests.Application;

public class AgregarJuegoTriviaCommandHandlerTests
{
    private static Partida NewPartida()
        => Partida.Crear(NombrePartida.Crear("Copa"), Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10);

    private static AgregarJuegoTriviaCommand Command(Guid partidaId, int orden = 1) =>
        new(partidaId, orden, new List<PreguntaRequest>
        {
            new("Capital?", new List<OpcionRequest> { new("Paris", true), new("Londres", false) }, 10, 30)
        });

    [Fact]
    public async Task Handle_adds_game_to_both_aggregates_and_saves_once()
    {
        var partidas = new FakePartidaRepository();
        var juegos = new FakeJuegoTriviaRepository();
        var uow = new FakePartidasUnitOfWork();
        var partida = NewPartida();
        partidas.Add(partida);

        var handler = new AgregarJuegoTriviaCommandHandler(partidas, juegos, uow);
        var response = await handler.Handle(Command(partida.PartidaId.Valor), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, response.JuegoId);
        Assert.Single(juegos.Store);
        Assert.Single(partida.Juegos);
        Assert.Equal(TipoJuego.Trivia, partida.Juegos[0].TipoJuego);
        Assert.Equal(1, uow.SaveCount);
    }

    [Fact]
    public async Task Handle_throws_when_partida_not_found()
    {
        var handler = new AgregarJuegoTriviaCommandHandler(
            new FakePartidaRepository(), new FakeJuegoTriviaRepository(), new FakePartidasUnitOfWork());

        await Assert.ThrowsAsync<PartidaNoEncontradaException>(
            () => handler.Handle(Command(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_does_not_save_when_orden_collides()
    {
        var partidas = new FakePartidaRepository();
        var juegos = new FakeJuegoTriviaRepository();
        var uow = new FakePartidasUnitOfWork();
        var partida = NewPartida();
        partida.AgregarJuego(JuegoId.New(), 1, TipoJuego.BusquedaDelTesoro); // orden 1 already taken
        partidas.Add(partida);

        var handler = new AgregarJuegoTriviaCommandHandler(partidas, juegos, uow);

        await Assert.ThrowsAsync<Umbral.Partidas.Domain.Exceptions.OrdenJuegoDuplicadoException>(
            () => handler.Handle(Command(partida.PartidaId.Valor, orden: 1), CancellationToken.None));
        Assert.Empty(juegos.Store);
        Assert.Equal(0, uow.SaveCount);
    }
}
```

```csharp
// tests/Umbral.Partidas.UnitTests/Application/AgregarJuegoTriviaCommandValidatorTests.cs
using System;
using System.Collections.Generic;
using Umbral.Partidas.Application.Commands;
using Umbral.Partidas.Application.DTOs;
using Umbral.Partidas.Application.Validators;

namespace Umbral.Partidas.UnitTests.Application;

public class AgregarJuegoTriviaCommandValidatorTests
{
    private readonly AgregarJuegoTriviaCommandValidator _validator = new();

    private static AgregarJuegoTriviaCommand WithQuestions(IReadOnlyList<PreguntaRequest> preguntas)
        => new(Guid.NewGuid(), 1, preguntas);

    [Fact]
    public void Valid_command_passes()
    {
        var cmd = WithQuestions(new List<PreguntaRequest>
        {
            new("Q", new List<OpcionRequest> { new("A", true), new("B", false) }, 10, 30)
        });
        Assert.True(_validator.Validate(cmd).IsValid);
    }

    [Fact]
    public void Empty_questions_fails()
    {
        Assert.False(_validator.Validate(WithQuestions(new List<PreguntaRequest>())).IsValid);
    }

    [Fact]
    public void Question_without_two_options_fails()
    {
        var cmd = WithQuestions(new List<PreguntaRequest>
        {
            new("Q", new List<OpcionRequest> { new("A", true) }, 10, 30)
        });
        Assert.False(_validator.Validate(cmd).IsValid);
    }

    [Fact]
    public void Question_without_exactly_one_correct_fails()
    {
        var cmd = WithQuestions(new List<PreguntaRequest>
        {
            new("Q", new List<OpcionRequest> { new("A", true), new("B", true) }, 10, 30)
        });
        Assert.False(_validator.Validate(cmd).IsValid);
    }

    [Fact]
    public void Non_positive_puntaje_or_time_fails()
    {
        var cmd = WithQuestions(new List<PreguntaRequest>
        {
            new("Q", new List<OpcionRequest> { new("A", true), new("B", false) }, 0, 0)
        });
        Assert.False(_validator.Validate(cmd).IsValid);
    }
}
```

- [ ] **Step 3: Run the tests to verify they fail**

Run: `dotnet test "services/partidas/tests/Umbral.Partidas.UnitTests/Umbral.Partidas.UnitTests.csproj" --filter "FullyQualifiedName~AgregarJuegoTrivia"`
Expected: FAIL — command, DTOs, handler, validator do not exist.

- [ ] **Step 4: Implement the DTOs, command, handler and validator**

```csharp
// Application/DTOs/PreguntaRequest.cs
namespace Umbral.Partidas.Application.DTOs;

public sealed record OpcionRequest(string Texto, bool EsCorrecta);

public sealed record PreguntaRequest(
    string Texto,
    IReadOnlyList<OpcionRequest> Opciones,
    int Puntaje,
    int TiempoLimiteSegundos);
```

```csharp
// Application/DTOs/AgregarJuegoResponse.cs
namespace Umbral.Partidas.Application.DTOs;

public sealed record AgregarJuegoResponse(Guid JuegoId);
```

```csharp
// Application/Commands/AgregarJuegoTriviaCommand.cs
using MediatR;
using Umbral.Partidas.Application.DTOs;

namespace Umbral.Partidas.Application.Commands;

public sealed record AgregarJuegoTriviaCommand(
    Guid PartidaId,
    int Orden,
    IReadOnlyList<PreguntaRequest> Preguntas) : IRequest<AgregarJuegoResponse>;
```

```csharp
// Application/Handlers/Commands/AgregarJuegoTriviaCommandHandler.cs
using System.Linq;
using MediatR;
using Umbral.Partidas.Application.Commands;
using Umbral.Partidas.Application.DTOs;
using Umbral.Partidas.Application.Exceptions;
using Umbral.Partidas.Domain.Abstractions.Persistence;
using Umbral.Partidas.Domain.Entities;
using Umbral.Partidas.Domain.Enums;
using Umbral.Partidas.Domain.ValueObjects;

namespace Umbral.Partidas.Application.Handlers.Commands;

public sealed class AgregarJuegoTriviaCommandHandler : IRequestHandler<AgregarJuegoTriviaCommand, AgregarJuegoResponse>
{
    private readonly IPartidaRepository _partidas;
    private readonly IJuegoTriviaRepository _juegos;
    private readonly IPartidasUnitOfWork _unitOfWork;

    public AgregarJuegoTriviaCommandHandler(
        IPartidaRepository partidas,
        IJuegoTriviaRepository juegos,
        IPartidasUnitOfWork unitOfWork)
    {
        _partidas = partidas;
        _juegos = juegos;
        _unitOfWork = unitOfWork;
    }

    public async Task<AgregarJuegoResponse> Handle(AgregarJuegoTriviaCommand request, CancellationToken cancellationToken)
    {
        var partidaId = PartidaId.From(request.PartidaId);
        var partida = await _partidas.GetByIdAsync(partidaId, cancellationToken)
            ?? throw new PartidaNoEncontradaException(request.PartidaId);

        var preguntas = request.Preguntas
            .Select(p => new PreguntaSpec(
                p.Texto,
                p.Opciones.Select(o => new OpcionSpec(o.Texto, o.EsCorrecta)).ToList(),
                p.Puntaje,
                p.TiempoLimiteSegundos))
            .ToList();

        // Build the game first (validates question content), then register the ordered
        // reference on the Partida (validates orden uniqueness). If either throws, nothing
        // is staged, so the single SaveChanges below is never reached.
        var juego = JuegoTrivia.Crear(partidaId, request.Orden, preguntas);
        partida.AgregarJuego(juego.JuegoId, request.Orden, TipoJuego.Trivia);

        _juegos.Add(juego);
        _partidas.Update(partida);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AgregarJuegoResponse(juego.JuegoId.Valor);
    }
}
```

```csharp
// Application/Validators/AgregarJuegoTriviaCommandValidator.cs
using FluentValidation;
using Umbral.Partidas.Application.Commands;
using Umbral.Partidas.Application.DTOs;

namespace Umbral.Partidas.Application.Validators;

public sealed class AgregarJuegoTriviaCommandValidator : AbstractValidator<AgregarJuegoTriviaCommand>
{
    public AgregarJuegoTriviaCommandValidator()
    {
        RuleFor(x => x.PartidaId).NotEmpty();
        RuleFor(x => x.Orden).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Preguntas).NotEmpty();
        RuleForEach(x => x.Preguntas).SetValidator(new PreguntaRequestValidator());
    }

    private sealed class PreguntaRequestValidator : AbstractValidator<PreguntaRequest>
    {
        public PreguntaRequestValidator()
        {
            RuleFor(p => p.Texto).NotEmpty();
            RuleFor(p => p.Puntaje).GreaterThan(0);
            RuleFor(p => p.TiempoLimiteSegundos).GreaterThan(0);
            RuleFor(p => p.Opciones).NotNull().Must(o => o is { Count: >= 2 })
                .WithMessage("Se requieren al menos 2 opciones.");
            RuleFor(p => p.Opciones).Must(o => o != null && o.Count(x => x.EsCorrecta) == 1)
                .WithMessage("Debe haber exactamente una opcion correcta.");
            RuleForEach(p => p.Opciones).ChildRules(o => o.RuleFor(x => x.Texto).NotEmpty());
        }
    }
}
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test "services/partidas/tests/Umbral.Partidas.UnitTests/Umbral.Partidas.UnitTests.csproj" --filter "FullyQualifiedName~AgregarJuegoTrivia"`
Expected: PASS (8 tests).

- [ ] **Step 6: Commit**

```bash
git add services/partidas/src/Umbral.Partidas.Domain/Abstractions services/partidas/src/Umbral.Partidas.Application services/partidas/tests/Umbral.Partidas.UnitTests/Application
git commit -m "feat(partidas): add AgregarJuegoTrivia command with two-aggregate write"
```

---

### Task 8: Application — `AgregarJuegoBDT`

**Files:**
- Create: `services/partidas/src/Umbral.Partidas.Domain/Abstractions/Persistence/IJuegoBDTRepository.cs`
- Create: `services/partidas/src/Umbral.Partidas.Application/DTOs/EtapaRequest.cs`
- Create: `services/partidas/src/Umbral.Partidas.Application/Commands/AgregarJuegoBDTCommand.cs`
- Create: `services/partidas/src/Umbral.Partidas.Application/Handlers/Commands/AgregarJuegoBDTCommandHandler.cs`
- Create: `services/partidas/src/Umbral.Partidas.Application/Validators/AgregarJuegoBDTCommandValidator.cs`
- Create: `services/partidas/tests/Umbral.Partidas.UnitTests/Application/Fakes/FakeJuegoBDTRepository.cs`
- Test: `services/partidas/tests/Umbral.Partidas.UnitTests/Application/AgregarJuegoBDTCommandHandlerTests.cs`
- Test: `services/partidas/tests/Umbral.Partidas.UnitTests/Application/AgregarJuegoBDTCommandValidatorTests.cs`

**Interfaces:**
- Consumes: `IPartidaRepository`, `IPartidasUnitOfWork` (Task 5), `PartidaNoEncontradaException` (Task 6), `AgregarJuegoResponse` (Task 7), `JuegoBDT`, `EtapaSpec`, `Partida`, `PartidaId`, `TipoJuego` (Tasks 1-4).
- Produces:
  - `IJuegoBDTRepository` — `void Add(JuegoBDT juego)`, `Task<IReadOnlyList<JuegoBDT>> GetByPartidaIdAsync(PartidaId partidaId, CancellationToken ct)`.
  - `EtapaRequest(int Orden, string CodigoQREsperado, int Puntaje, int TiempoLimiteSegundos)`.
  - `AgregarJuegoBDTCommand(Guid PartidaId, int Orden, string AreaBusqueda, IReadOnlyList<EtapaRequest> Etapas) : IRequest<AgregarJuegoResponse>`.
  - `FakeJuegoBDTRepository` (test double, reused by Task 9).

- [ ] **Step 1: Create `IJuegoBDTRepository`**

```csharp
// Domain/Abstractions/Persistence/IJuegoBDTRepository.cs
using Umbral.Partidas.Domain.Entities;
using Umbral.Partidas.Domain.ValueObjects;

namespace Umbral.Partidas.Domain.Abstractions.Persistence;

public interface IJuegoBDTRepository
{
    void Add(JuegoBDT juego);
    Task<IReadOnlyList<JuegoBDT>> GetByPartidaIdAsync(PartidaId partidaId, CancellationToken cancellationToken);
}
```

- [ ] **Step 2: Write the failing handler + validator tests**

```csharp
// tests/Umbral.Partidas.UnitTests/Application/Fakes/FakeJuegoBDTRepository.cs
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbral.Partidas.Domain.Abstractions.Persistence;
using Umbral.Partidas.Domain.Entities;
using Umbral.Partidas.Domain.ValueObjects;

namespace Umbral.Partidas.UnitTests.Application.Fakes;

public sealed class FakeJuegoBDTRepository : IJuegoBDTRepository
{
    public readonly List<JuegoBDT> Store = new();

    public void Add(JuegoBDT juego) => Store.Add(juego);

    public Task<IReadOnlyList<JuegoBDT>> GetByPartidaIdAsync(PartidaId partidaId, CancellationToken cancellationToken)
        => Task.FromResult((IReadOnlyList<JuegoBDT>)Store.Where(j => j.PartidaId == partidaId).ToList());
}
```

```csharp
// tests/Umbral.Partidas.UnitTests/Application/AgregarJuegoBDTCommandHandlerTests.cs
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Umbral.Partidas.Application.Commands;
using Umbral.Partidas.Application.DTOs;
using Umbral.Partidas.Application.Exceptions;
using Umbral.Partidas.Application.Handlers.Commands;
using Umbral.Partidas.Domain.Entities;
using Umbral.Partidas.Domain.Enums;
using Umbral.Partidas.Domain.ValueObjects;
using Umbral.Partidas.UnitTests.Application.Fakes;

namespace Umbral.Partidas.UnitTests.Application;

public class AgregarJuegoBDTCommandHandlerTests
{
    private static Partida NewPartida()
        => Partida.Crear(NombrePartida.Crear("Copa"), Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10);

    private static AgregarJuegoBDTCommand Command(Guid partidaId, int orden = 1) =>
        new(partidaId, orden, "Plaza central", new List<EtapaRequest>
        {
            new(1, "QR-1", 50, 120)
        });

    [Fact]
    public async Task Handle_adds_bdt_game_to_both_aggregates_and_saves_once()
    {
        var partidas = new FakePartidaRepository();
        var juegos = new FakeJuegoBDTRepository();
        var uow = new FakePartidasUnitOfWork();
        var partida = NewPartida();
        partidas.Add(partida);

        var handler = new AgregarJuegoBDTCommandHandler(partidas, juegos, uow);
        var response = await handler.Handle(Command(partida.PartidaId.Valor), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, response.JuegoId);
        Assert.Single(juegos.Store);
        Assert.Single(partida.Juegos);
        Assert.Equal(TipoJuego.BusquedaDelTesoro, partida.Juegos[0].TipoJuego);
        Assert.Equal(1, uow.SaveCount);
    }

    [Fact]
    public async Task Handle_throws_when_partida_not_found()
    {
        var handler = new AgregarJuegoBDTCommandHandler(
            new FakePartidaRepository(), new FakeJuegoBDTRepository(), new FakePartidasUnitOfWork());

        await Assert.ThrowsAsync<PartidaNoEncontradaException>(
            () => handler.Handle(Command(Guid.NewGuid()), CancellationToken.None));
    }
}
```

```csharp
// tests/Umbral.Partidas.UnitTests/Application/AgregarJuegoBDTCommandValidatorTests.cs
using System;
using System.Collections.Generic;
using Umbral.Partidas.Application.Commands;
using Umbral.Partidas.Application.DTOs;
using Umbral.Partidas.Application.Validators;

namespace Umbral.Partidas.UnitTests.Application;

public class AgregarJuegoBDTCommandValidatorTests
{
    private readonly AgregarJuegoBDTCommandValidator _validator = new();

    private static AgregarJuegoBDTCommand With(string area, IReadOnlyList<EtapaRequest> etapas)
        => new(Guid.NewGuid(), 1, area, etapas);

    [Fact]
    public void Valid_command_passes()
    {
        var cmd = With("Plaza", new List<EtapaRequest> { new(1, "QR", 50, 120) });
        Assert.True(_validator.Validate(cmd).IsValid);
    }

    [Fact]
    public void Blank_area_fails()
    {
        var cmd = With("  ", new List<EtapaRequest> { new(1, "QR", 50, 120) });
        Assert.False(_validator.Validate(cmd).IsValid);
    }

    [Fact]
    public void Empty_stages_fails()
    {
        Assert.False(_validator.Validate(With("Plaza", new List<EtapaRequest>())).IsValid);
    }

    [Fact]
    public void Stage_with_blank_qr_or_non_positive_values_fails()
    {
        var cmd = With("Plaza", new List<EtapaRequest> { new(1, "", 0, 0) });
        Assert.False(_validator.Validate(cmd).IsValid);
    }
}
```

- [ ] **Step 3: Run the tests to verify they fail**

Run: `dotnet test "services/partidas/tests/Umbral.Partidas.UnitTests/Umbral.Partidas.UnitTests.csproj" --filter "FullyQualifiedName~AgregarJuegoBDT"`
Expected: FAIL — command, DTOs, handler, validator do not exist.

- [ ] **Step 4: Implement the DTO, command, handler and validator**

```csharp
// Application/DTOs/EtapaRequest.cs
namespace Umbral.Partidas.Application.DTOs;

public sealed record EtapaRequest(
    int Orden,
    string CodigoQREsperado,
    int Puntaje,
    int TiempoLimiteSegundos);
```

```csharp
// Application/Commands/AgregarJuegoBDTCommand.cs
using MediatR;
using Umbral.Partidas.Application.DTOs;

namespace Umbral.Partidas.Application.Commands;

public sealed record AgregarJuegoBDTCommand(
    Guid PartidaId,
    int Orden,
    string AreaBusqueda,
    IReadOnlyList<EtapaRequest> Etapas) : IRequest<AgregarJuegoResponse>;
```

```csharp
// Application/Handlers/Commands/AgregarJuegoBDTCommandHandler.cs
using System.Linq;
using MediatR;
using Umbral.Partidas.Application.Commands;
using Umbral.Partidas.Application.DTOs;
using Umbral.Partidas.Application.Exceptions;
using Umbral.Partidas.Domain.Abstractions.Persistence;
using Umbral.Partidas.Domain.Entities;
using Umbral.Partidas.Domain.Enums;
using Umbral.Partidas.Domain.ValueObjects;

namespace Umbral.Partidas.Application.Handlers.Commands;

public sealed class AgregarJuegoBDTCommandHandler : IRequestHandler<AgregarJuegoBDTCommand, AgregarJuegoResponse>
{
    private readonly IPartidaRepository _partidas;
    private readonly IJuegoBDTRepository _juegos;
    private readonly IPartidasUnitOfWork _unitOfWork;

    public AgregarJuegoBDTCommandHandler(
        IPartidaRepository partidas,
        IJuegoBDTRepository juegos,
        IPartidasUnitOfWork unitOfWork)
    {
        _partidas = partidas;
        _juegos = juegos;
        _unitOfWork = unitOfWork;
    }

    public async Task<AgregarJuegoResponse> Handle(AgregarJuegoBDTCommand request, CancellationToken cancellationToken)
    {
        var partidaId = PartidaId.From(request.PartidaId);
        var partida = await _partidas.GetByIdAsync(partidaId, cancellationToken)
            ?? throw new PartidaNoEncontradaException(request.PartidaId);

        var etapas = request.Etapas
            .Select(e => new EtapaSpec(e.Orden, e.CodigoQREsperado, e.Puntaje, e.TiempoLimiteSegundos))
            .ToList();

        var juego = JuegoBDT.Crear(partidaId, request.Orden, request.AreaBusqueda, etapas);
        partida.AgregarJuego(juego.JuegoId, request.Orden, TipoJuego.BusquedaDelTesoro);

        _juegos.Add(juego);
        _partidas.Update(partida);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AgregarJuegoResponse(juego.JuegoId.Valor);
    }
}
```

```csharp
// Application/Validators/AgregarJuegoBDTCommandValidator.cs
using FluentValidation;
using Umbral.Partidas.Application.Commands;
using Umbral.Partidas.Application.DTOs;

namespace Umbral.Partidas.Application.Validators;

public sealed class AgregarJuegoBDTCommandValidator : AbstractValidator<AgregarJuegoBDTCommand>
{
    public AgregarJuegoBDTCommandValidator()
    {
        RuleFor(x => x.PartidaId).NotEmpty();
        RuleFor(x => x.Orden).GreaterThanOrEqualTo(1);
        RuleFor(x => x.AreaBusqueda).NotEmpty();
        RuleFor(x => x.Etapas).NotEmpty();
        RuleForEach(x => x.Etapas).SetValidator(new EtapaRequestValidator());
    }

    private sealed class EtapaRequestValidator : AbstractValidator<EtapaRequest>
    {
        public EtapaRequestValidator()
        {
            RuleFor(e => e.Orden).GreaterThanOrEqualTo(1);
            RuleFor(e => e.CodigoQREsperado).NotEmpty();
            RuleFor(e => e.Puntaje).GreaterThan(0);
            RuleFor(e => e.TiempoLimiteSegundos).GreaterThan(0);
        }
    }
}
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test "services/partidas/tests/Umbral.Partidas.UnitTests/Umbral.Partidas.UnitTests.csproj" --filter "FullyQualifiedName~AgregarJuegoBDT"`
Expected: PASS (6 tests).

- [ ] **Step 6: Commit**

```bash
git add services/partidas/src/Umbral.Partidas.Domain/Abstractions services/partidas/src/Umbral.Partidas.Application services/partidas/tests/Umbral.Partidas.UnitTests/Application
git commit -m "feat(partidas): add AgregarJuegoBDT command with two-aggregate write"
```

---

### Task 9: Application — review queries (`GetPartidaById`, `ListPartidas`)

**Files:**
- Create: `services/partidas/src/Umbral.Partidas.Application/DTOs/PartidaSummaryDto.cs`
- Create: `services/partidas/src/Umbral.Partidas.Application/DTOs/PartidaDetailDto.cs`
- Create: `services/partidas/src/Umbral.Partidas.Application/Queries/GetPartidaByIdQuery.cs`
- Create: `services/partidas/src/Umbral.Partidas.Application/Queries/ListPartidasQuery.cs`
- Create: `services/partidas/src/Umbral.Partidas.Application/Handlers/Queries/GetPartidaByIdQueryHandler.cs`
- Create: `services/partidas/src/Umbral.Partidas.Application/Handlers/Queries/ListPartidasQueryHandler.cs`
- Test: `services/partidas/tests/Umbral.Partidas.UnitTests/Application/GetPartidaByIdQueryHandlerTests.cs`
- Test: `services/partidas/tests/Umbral.Partidas.UnitTests/Application/ListPartidasQueryHandlerTests.cs`

**Interfaces:**
- Consumes: `IPartidaRepository`, `IJuegoTriviaRepository`, `IJuegoBDTRepository` (Tasks 5,7,8), `PartidaNoEncontradaException` (Task 6), all aggregates + fakes.
- Produces:
  - `GetPartidaByIdQuery(Guid PartidaId) : IRequest<PartidaDetailDto>`.
  - `ListPartidasQuery() : IRequest<IReadOnlyList<PartidaSummaryDto>>`.
  - DTO graph: `PartidaSummaryDto`, `PartidaDetailDto`, `JuegoDto`, `TriviaContenidoDto`, `PreguntaDto`, `OpcionDto`, `BDTContenidoDto`, `EtapaDto` (enums serialized as their `.ToString()` name; `Estado` is `null` when unpublished).

- [ ] **Step 1: Write the failing query handler tests**

```csharp
// tests/Umbral.Partidas.UnitTests/Application/GetPartidaByIdQueryHandlerTests.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbral.Partidas.Application.Exceptions;
using Umbral.Partidas.Application.Handlers.Queries;
using Umbral.Partidas.Application.Queries;
using Umbral.Partidas.Domain.Entities;
using Umbral.Partidas.Domain.Enums;
using Umbral.Partidas.Domain.ValueObjects;
using Umbral.Partidas.UnitTests.Application.Fakes;

namespace Umbral.Partidas.UnitTests.Application;

public class GetPartidaByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_returns_detail_with_ordered_games_and_content()
    {
        var partidas = new FakePartidaRepository();
        var trivias = new FakeJuegoTriviaRepository();
        var bdts = new FakeJuegoBDTRepository();

        var partida = Partida.Crear(NombrePartida.Crear("Copa"), Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10);
        var trivia = JuegoTrivia.Crear(partida.PartidaId, 1, new[]
        {
            new PreguntaSpec("Q", new List<OpcionSpec> { new("A", true), new("B", false) }, 10, 30)
        });
        var bdt = JuegoBDT.Crear(partida.PartidaId, 2, "Plaza", new[] { new EtapaSpec(1, "QR", 50, 120) });
        partida.AgregarJuego(trivia.JuegoId, 1, TipoJuego.Trivia);
        partida.AgregarJuego(bdt.JuegoId, 2, TipoJuego.BusquedaDelTesoro);
        partidas.Add(partida);
        trivias.Add(trivia);
        bdts.Add(bdt);

        var handler = new GetPartidaByIdQueryHandler(partidas, trivias, bdts);
        var detail = await handler.Handle(new GetPartidaByIdQuery(partida.PartidaId.Valor), CancellationToken.None);

        Assert.Equal("Copa", detail.NombrePartida);
        Assert.Null(detail.Estado);
        Assert.Equal(2, detail.Juegos.Count);
        Assert.Equal(1, detail.Juegos[0].Orden);
        Assert.Equal("Trivia", detail.Juegos[0].TipoJuego);
        Assert.NotNull(detail.Juegos[0].Trivia);
        Assert.Single(detail.Juegos[0].Trivia!.Preguntas);
        Assert.Equal("BusquedaDelTesoro", detail.Juegos[1].TipoJuego);
        Assert.NotNull(detail.Juegos[1].BDT);
        Assert.Equal("Plaza", detail.Juegos[1].BDT!.AreaBusqueda);
    }

    [Fact]
    public async Task Handle_throws_when_partida_not_found()
    {
        var handler = new GetPartidaByIdQueryHandler(
            new FakePartidaRepository(), new FakeJuegoTriviaRepository(), new FakeJuegoBDTRepository());

        await Assert.ThrowsAsync<PartidaNoEncontradaException>(
            () => handler.Handle(new GetPartidaByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }
}
```

```csharp
// tests/Umbral.Partidas.UnitTests/Application/ListPartidasQueryHandlerTests.cs
using System.Threading;
using System.Threading.Tasks;
using Umbral.Partidas.Application.Handlers.Queries;
using Umbral.Partidas.Application.Queries;
using Umbral.Partidas.Domain.Entities;
using Umbral.Partidas.Domain.Enums;
using Umbral.Partidas.Domain.ValueObjects;
using Umbral.Partidas.UnitTests.Application.Fakes;

namespace Umbral.Partidas.UnitTests.Application;

public class ListPartidasQueryHandlerTests
{
    [Fact]
    public async Task Handle_returns_summaries_with_game_counts()
    {
        var partidas = new FakePartidaRepository();
        var partida = Partida.Crear(NombrePartida.Crear("Copa"), Modalidad.Equipo, ModoInicioPartida.Manual, null, 2, 8);
        partida.AgregarJuego(JuegoId.New(), 1, TipoJuego.Trivia);
        partidas.Add(partida);

        var handler = new ListPartidasQueryHandler(partidas);
        var result = await handler.Handle(new ListPartidasQuery(), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("Copa", result[0].NombrePartida);
        Assert.Equal("Equipo", result[0].Modalidad);
        Assert.Equal(1, result[0].CantidadJuegos);
        Assert.Null(result[0].Estado);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test "services/partidas/tests/Umbral.Partidas.UnitTests/Umbral.Partidas.UnitTests.csproj" --filter "FullyQualifiedName~QueryHandlerTests"`
Expected: FAIL — queries, handlers, DTOs do not exist.

- [ ] **Step 3: Implement the DTOs**

```csharp
// Application/DTOs/PartidaSummaryDto.cs
namespace Umbral.Partidas.Application.DTOs;

public sealed record PartidaSummaryDto(
    Guid PartidaId,
    string NombrePartida,
    string Modalidad,
    string ModoInicioPartida,
    DateTime? TiempoInicio,
    int MinimosParticipacion,
    int MaximosParticipacion,
    string? Estado,
    int CantidadJuegos);
```

```csharp
// Application/DTOs/PartidaDetailDto.cs
namespace Umbral.Partidas.Application.DTOs;

public sealed record PartidaDetailDto(
    Guid PartidaId,
    string NombrePartida,
    string Modalidad,
    string ModoInicioPartida,
    DateTime? TiempoInicio,
    int MinimosParticipacion,
    int MaximosParticipacion,
    string? Estado,
    IReadOnlyList<JuegoDto> Juegos);

public sealed record JuegoDto(
    Guid JuegoId,
    int Orden,
    string TipoJuego,
    string Estado,
    TriviaContenidoDto? Trivia,
    BDTContenidoDto? BDT);

public sealed record TriviaContenidoDto(IReadOnlyList<PreguntaDto> Preguntas);

public sealed record PreguntaDto(
    Guid PreguntaId,
    string Texto,
    int PuntajeAsignado,
    int TiempoLimiteSegundos,
    IReadOnlyList<OpcionDto> Opciones);

public sealed record OpcionDto(Guid OpcionId, string Texto, bool EsCorrecta);

public sealed record BDTContenidoDto(string AreaBusqueda, IReadOnlyList<EtapaDto> Etapas);

public sealed record EtapaDto(
    Guid EtapaBDTId,
    int Orden,
    string CodigoQREsperado,
    int PuntajeAsignado,
    int TiempoLimiteSegundos);
```

- [ ] **Step 4: Implement the queries**

```csharp
// Application/Queries/GetPartidaByIdQuery.cs
using MediatR;
using Umbral.Partidas.Application.DTOs;

namespace Umbral.Partidas.Application.Queries;

public sealed record GetPartidaByIdQuery(Guid PartidaId) : IRequest<PartidaDetailDto>;
```

```csharp
// Application/Queries/ListPartidasQuery.cs
using MediatR;
using Umbral.Partidas.Application.DTOs;

namespace Umbral.Partidas.Application.Queries;

public sealed record ListPartidasQuery() : IRequest<IReadOnlyList<PartidaSummaryDto>>;
```

- [ ] **Step 5: Implement the query handlers**

```csharp
// Application/Handlers/Queries/GetPartidaByIdQueryHandler.cs
using System.Linq;
using MediatR;
using Umbral.Partidas.Application.DTOs;
using Umbral.Partidas.Application.Exceptions;
using Umbral.Partidas.Domain.Abstractions.Persistence;
using Umbral.Partidas.Domain.Entities;
using Umbral.Partidas.Domain.Enums;
using Umbral.Partidas.Domain.ValueObjects;

namespace Umbral.Partidas.Application.Handlers.Queries;

public sealed class GetPartidaByIdQueryHandler : IRequestHandler<GetPartidaByIdQuery, PartidaDetailDto>
{
    private readonly IPartidaRepository _partidas;
    private readonly IJuegoTriviaRepository _trivias;
    private readonly IJuegoBDTRepository _bdts;

    public GetPartidaByIdQueryHandler(
        IPartidaRepository partidas,
        IJuegoTriviaRepository trivias,
        IJuegoBDTRepository bdts)
    {
        _partidas = partidas;
        _trivias = trivias;
        _bdts = bdts;
    }

    public async Task<PartidaDetailDto> Handle(GetPartidaByIdQuery request, CancellationToken cancellationToken)
    {
        var partidaId = PartidaId.From(request.PartidaId);
        var partida = await _partidas.GetByIdAsync(partidaId, cancellationToken)
            ?? throw new PartidaNoEncontradaException(request.PartidaId);

        var trivias = (await _trivias.GetByPartidaIdAsync(partidaId, cancellationToken)).ToDictionary(j => j.JuegoId);
        var bdts = (await _bdts.GetByPartidaIdAsync(partidaId, cancellationToken)).ToDictionary(j => j.JuegoId);

        var juegos = partida.Juegos
            .OrderBy(j => j.Orden)
            .Select(reference =>
            {
                if (reference.TipoJuego == TipoJuego.Trivia && trivias.TryGetValue(reference.JuegoId, out var trivia))
                {
                    return new JuegoDto(
                        reference.JuegoId.Valor,
                        reference.Orden,
                        reference.TipoJuego.ToString(),
                        trivia.Estado.ToString(),
                        new TriviaContenidoDto(trivia.Preguntas.Select(MapPregunta).ToList()),
                        null);
                }

                if (reference.TipoJuego == TipoJuego.BusquedaDelTesoro && bdts.TryGetValue(reference.JuegoId, out var bdt))
                {
                    return new JuegoDto(
                        reference.JuegoId.Valor,
                        reference.Orden,
                        reference.TipoJuego.ToString(),
                        bdt.Estado.ToString(),
                        null,
                        new BDTContenidoDto(bdt.AreaBusqueda, bdt.Etapas.OrderBy(e => e.Orden).Select(MapEtapa).ToList()));
                }

                // Reference present but content aggregate missing — surface the reference with no content.
                return new JuegoDto(reference.JuegoId.Valor, reference.Orden, reference.TipoJuego.ToString(),
                    EstadoJuego.Pendiente.ToString(), null, null);
            })
            .ToList();

        return new PartidaDetailDto(
            partida.PartidaId.Valor,
            partida.NombrePartida.Valor,
            partida.Modalidad.ToString(),
            partida.ModoInicioPartida.ToString(),
            partida.TiempoInicio,
            partida.MinimosParticipacion,
            partida.MaximosParticipacion,
            partida.Estado?.ToString(),
            juegos);
    }

    private static PreguntaDto MapPregunta(Pregunta p) => new(
        p.PreguntaId,
        p.Texto,
        p.PuntajeAsignado.Valor,
        p.TiempoLimiteSegundos,
        p.Opciones.Select(o => new OpcionDto(o.OpcionId, o.Texto, o.EsCorrecta)).ToList());

    private static EtapaDto MapEtapa(EtapaBDT e) => new(
        e.EtapaBDTId,
        e.Orden,
        e.CodigoQREsperado,
        e.PuntajeAsignado.Valor,
        e.TiempoLimiteSegundos);
}
```

```csharp
// Application/Handlers/Queries/ListPartidasQueryHandler.cs
using System.Linq;
using MediatR;
using Umbral.Partidas.Application.DTOs;
using Umbral.Partidas.Domain.Abstractions.Persistence;

namespace Umbral.Partidas.Application.Handlers.Queries;

public sealed class ListPartidasQueryHandler : IRequestHandler<ListPartidasQuery, IReadOnlyList<PartidaSummaryDto>>
{
    private readonly IPartidaRepository _partidas;

    public ListPartidasQueryHandler(IPartidaRepository partidas)
    {
        _partidas = partidas;
    }

    public async Task<IReadOnlyList<PartidaSummaryDto>> Handle(ListPartidasQuery request, CancellationToken cancellationToken)
    {
        var partidas = await _partidas.ListAsync(cancellationToken);
        return partidas
            .Select(p => new PartidaSummaryDto(
                p.PartidaId.Valor,
                p.NombrePartida.Valor,
                p.Modalidad.ToString(),
                p.ModoInicioPartida.ToString(),
                p.TiempoInicio,
                p.MinimosParticipacion,
                p.MaximosParticipacion,
                p.Estado?.ToString(),
                p.Juegos.Count))
            .ToList();
    }
}
```

- [ ] **Step 6: Run the tests to verify they pass**

Run: `dotnet test "services/partidas/tests/Umbral.Partidas.UnitTests/Umbral.Partidas.UnitTests.csproj" --filter "FullyQualifiedName~QueryHandlerTests"`
Expected: PASS (3 tests).

- [ ] **Step 7: Commit**

```bash
git add services/partidas/src/Umbral.Partidas.Application services/partidas/tests/Umbral.Partidas.UnitTests/Application
git commit -m "feat(partidas): add GetPartidaById and ListPartidas review queries"
```

---

### Task 10: Infrastructure — DbContext mapping + EF migration

**Files:**
- Modify: `services/partidas/src/Umbral.Partidas.Infrastructure/Persistence/PartidasDbContext.cs`
- Create: `services/partidas/src/Umbral.Partidas.Infrastructure/Persistence/PartidasDbContextDesignTimeFactory.cs`
- Create (generated): `services/partidas/src/Umbral.Partidas.Infrastructure/Persistence/Migrations/*`
- Test: `services/partidas/tests/Umbral.Partidas.IntegrationTests/PartidaPersistenceTests.cs`

**Interfaces:**
- Consumes: all aggregates + VOs (Tasks 1-4).
- Produces: `PartidasDbContext` with `DbSet<Partida> Partidas`, `DbSet<JuegoTrivia> JuegosTrivia`, `DbSet<JuegoBDT> JuegosBDT`; VO `HasConversion` mappings; owned-child relationships. `PartidasDbContextDesignTimeFactory` (Npgsql, design-time only).

- [ ] **Step 1: Write the failing round-trip persistence test**

```csharp
// tests/Umbral.Partidas.IntegrationTests/PartidaPersistenceTests.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Umbral.Partidas.Domain.Entities;
using Umbral.Partidas.Domain.Enums;
using Umbral.Partidas.Domain.ValueObjects;
using Umbral.Partidas.Infrastructure.Persistence;

namespace Umbral.Partidas.IntegrationTests;

public class PartidaPersistenceTests
{
    private static PartidasDbContext NewContext(string dbName) =>
        new(new DbContextOptionsBuilder<PartidasDbContext>().UseInMemoryDatabase(dbName).Options);

    [Fact]
    public async Task Partida_with_trivia_and_bdt_games_round_trips()
    {
        var dbName = Guid.NewGuid().ToString();
        var partida = Partida.Crear(NombrePartida.Crear("Copa"), Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10);
        var trivia = JuegoTrivia.Crear(partida.PartidaId, 1, new[]
        {
            new PreguntaSpec("Q", new List<OpcionSpec> { new("A", true), new("B", false) }, 10, 30)
        });
        var bdt = JuegoBDT.Crear(partida.PartidaId, 2, "Plaza", new[] { new EtapaSpec(1, "QR", 50, 120) });
        partida.AgregarJuego(trivia.JuegoId, 1, TipoJuego.Trivia);
        partida.AgregarJuego(bdt.JuegoId, 2, TipoJuego.BusquedaDelTesoro);

        await using (var ctx = NewContext(dbName))
        {
            ctx.Partidas.Add(partida);
            ctx.JuegosTrivia.Add(trivia);
            ctx.JuegosBDT.Add(bdt);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = NewContext(dbName))
        {
            var loaded = await ctx.Partidas.Include(p => p.Juegos)
                .FirstAsync(p => p.PartidaId == partida.PartidaId);
            Assert.Equal("Copa", loaded.NombrePartida.Valor);
            Assert.Null(loaded.Estado);
            Assert.Equal(2, loaded.Juegos.Count);

            var loadedTrivia = await ctx.JuegosTrivia
                .Include(j => j.Preguntas).ThenInclude(p => p.Opciones)
                .FirstAsync(j => j.JuegoId == trivia.JuegoId);
            Assert.Single(loadedTrivia.Preguntas);
            Assert.Equal(10, loadedTrivia.Preguntas[0].PuntajeAsignado.Valor);
            Assert.Equal(2, loadedTrivia.Preguntas[0].Opciones.Count);

            var loadedBdt = await ctx.JuegosBDT.Include(j => j.Etapas)
                .FirstAsync(j => j.JuegoId == bdt.JuegoId);
            Assert.Equal("Plaza", loadedBdt.AreaBusqueda);
            Assert.Single(loadedBdt.Etapas);
            Assert.Equal("QR", loadedBdt.Etapas[0].CodigoQREsperado);
        }
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test "services/partidas/tests/Umbral.Partidas.IntegrationTests/Umbral.Partidas.IntegrationTests.csproj" --filter "FullyQualifiedName~PartidaPersistenceTests"`
Expected: FAIL — `DbSet`s and entity mappings do not exist (DbContext is the empty shell).

- [ ] **Step 3: Implement the DbContext mapping**

```csharp
// Infrastructure/Persistence/PartidasDbContext.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Umbral.Partidas.Domain.Entities;
using Umbral.Partidas.Domain.ValueObjects;

namespace Umbral.Partidas.Infrastructure.Persistence;

public sealed class PartidasDbContext : DbContext
{
    public PartidasDbContext(DbContextOptions<PartidasDbContext> options) : base(options)
    {
    }

    public DbSet<Partida> Partidas => Set<Partida>();
    public DbSet<JuegoTrivia> JuegosTrivia => Set<JuegoTrivia>();
    public DbSet<JuegoBDT> JuegosBDT => Set<JuegoBDT>();

    private static readonly ValueConverter<PartidaId, Guid> PartidaIdConverter =
        new(v => v.Valor, v => PartidaId.From(v));
    private static readonly ValueConverter<JuegoId, Guid> JuegoIdConverter =
        new(v => v.Valor, v => JuegoId.From(v));
    private static readonly ValueConverter<NombrePartida, string> NombrePartidaConverter =
        new(v => v.Valor, v => NombrePartida.Crear(v));
    private static readonly ValueConverter<PuntajeAsignado, int> PuntajeConverter =
        new(v => v.Valor, v => PuntajeAsignado.Crear(v));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Partida>(entity =>
        {
            entity.ToTable("partidas");
            entity.HasKey(x => x.PartidaId);
            entity.Property(x => x.PartidaId).HasColumnName("partidaid").HasConversion(PartidaIdConverter);
            entity.Property(x => x.NombrePartida).HasColumnName("nombrepartida")
                .HasConversion(NombrePartidaConverter).IsRequired().HasMaxLength(NombrePartida.LongitudMaxima);
            entity.Property(x => x.Estado).HasColumnName("estado"); // nullable enum
            entity.Property(x => x.Modalidad).HasColumnName("modalidad").IsRequired();
            entity.Property(x => x.ModoInicioPartida).HasColumnName("modoinicio").IsRequired();
            entity.Property(x => x.TiempoInicio).HasColumnName("tiempoinicio");
            entity.Property(x => x.MinimosParticipacion).HasColumnName("minimos").IsRequired();
            entity.Property(x => x.MaximosParticipacion).HasColumnName("maximos").IsRequired();
            entity.HasMany(x => x.Juegos).WithOne().HasForeignKey("partidaid").OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(x => x.Juegos).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<JuegoReferencia>(entity =>
        {
            entity.ToTable("partida_juegos");
            entity.HasKey(x => x.JuegoId);
            entity.Property(x => x.JuegoId).HasColumnName("juegoid").HasConversion(JuegoIdConverter);
            entity.Property(x => x.Orden).HasColumnName("orden").IsRequired();
            entity.Property(x => x.TipoJuego).HasColumnName("tipojuego").IsRequired();
        });

        modelBuilder.Entity<JuegoTrivia>(entity =>
        {
            entity.ToTable("juegos_trivia");
            entity.HasKey(x => x.JuegoId);
            entity.Property(x => x.JuegoId).HasColumnName("juegoid").HasConversion(JuegoIdConverter);
            entity.Property(x => x.PartidaId).HasColumnName("partidaid").HasConversion(PartidaIdConverter).IsRequired();
            entity.Property(x => x.Orden).HasColumnName("orden").IsRequired();
            entity.Property(x => x.Estado).HasColumnName("estado").IsRequired();
            entity.HasMany(x => x.Preguntas).WithOne().HasForeignKey("juegoid").OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(x => x.Preguntas).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.HasIndex(x => x.PartidaId).HasDatabaseName("ix_juegos_trivia_partidaid");
        });

        modelBuilder.Entity<Pregunta>(entity =>
        {
            entity.ToTable("preguntas");
            entity.HasKey(x => x.PreguntaId);
            entity.Property(x => x.PreguntaId).HasColumnName("preguntaid");
            entity.Property(x => x.Texto).HasColumnName("texto").IsRequired();
            entity.Property(x => x.PuntajeAsignado).HasColumnName("puntaje").HasConversion(PuntajeConverter).IsRequired();
            entity.Property(x => x.TiempoLimiteSegundos).HasColumnName("tiempolimite").IsRequired();
            entity.HasMany(x => x.Opciones).WithOne().HasForeignKey("preguntaid").OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(x => x.Opciones).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<Opcion>(entity =>
        {
            entity.ToTable("opciones");
            entity.HasKey(x => x.OpcionId);
            entity.Property(x => x.OpcionId).HasColumnName("opcionid");
            entity.Property(x => x.Texto).HasColumnName("texto").IsRequired();
            entity.Property(x => x.EsCorrecta).HasColumnName("escorrecta").IsRequired();
        });

        modelBuilder.Entity<JuegoBDT>(entity =>
        {
            entity.ToTable("juegos_bdt");
            entity.HasKey(x => x.JuegoId);
            entity.Property(x => x.JuegoId).HasColumnName("juegoid").HasConversion(JuegoIdConverter);
            entity.Property(x => x.PartidaId).HasColumnName("partidaid").HasConversion(PartidaIdConverter).IsRequired();
            entity.Property(x => x.Orden).HasColumnName("orden").IsRequired();
            entity.Property(x => x.Estado).HasColumnName("estado").IsRequired();
            entity.Property(x => x.AreaBusqueda).HasColumnName("areabusqueda").IsRequired();
            entity.HasMany(x => x.Etapas).WithOne().HasForeignKey("juegoid").OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(x => x.Etapas).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.HasIndex(x => x.PartidaId).HasDatabaseName("ix_juegos_bdt_partidaid");
        });

        modelBuilder.Entity<EtapaBDT>(entity =>
        {
            entity.ToTable("etapas_bdt");
            entity.HasKey(x => x.EtapaBDTId);
            entity.Property(x => x.EtapaBDTId).HasColumnName("etapabdtid");
            entity.Property(x => x.Orden).HasColumnName("orden").IsRequired();
            entity.Property(x => x.CodigoQREsperado).HasColumnName("codigoqr").IsRequired();
            entity.Property(x => x.PuntajeAsignado).HasColumnName("puntaje").HasConversion(PuntajeConverter).IsRequired();
            entity.Property(x => x.TiempoLimiteSegundos).HasColumnName("tiempolimite").IsRequired();
        });
    }
}
```

- [ ] **Step 4: Run the round-trip test to verify it passes**

Run: `dotnet test "services/partidas/tests/Umbral.Partidas.IntegrationTests/Umbral.Partidas.IntegrationTests.csproj" --filter "FullyQualifiedName~PartidaPersistenceTests"`
Expected: PASS (1 test).

> If the InMemory provider rejects a VO key converter, apply systematic-debugging — do NOT weaken the test. The record-struct VOs have value equality, so the failure (if any) is in mapping config, not the model.

- [ ] **Step 5: Add the design-time factory (for Npgsql migrations)**

```csharp
// Infrastructure/Persistence/PartidasDbContextDesignTimeFactory.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Umbral.Partidas.Infrastructure.Persistence;

public sealed class PartidasDbContextDesignTimeFactory : IDesignTimeDbContextFactory<PartidasDbContext>
{
    public PartidasDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PartidasDbContext>()
            .UseNpgsql("Host=localhost;Port=55432;Database=umbral_partidas;Username=umbral;Password=16102005")
            .Options;
        return new PartidasDbContext(options);
    }
}
```

- [ ] **Step 6: Generate the initial Npgsql migration**

Run (ensure `dotnet ef` is available: `dotnet tool install --global dotnet-ef` if needed):

```bash
dotnet ef migrations add InitialPartidasModel \
  --project services/partidas/src/Umbral.Partidas.Infrastructure/Umbral.Partidas.Infrastructure.csproj \
  --startup-project services/partidas/src/Umbral.Partidas.Api/Umbral.Partidas.Api.csproj \
  --output-dir Persistence/Migrations
```

Expected: a `Persistence/Migrations/*_InitialPartidasModel.cs` + `PartidasDbContextModelSnapshot.cs` are created; `dotnet build "services/partidas/Umbral.Partidas.sln"` succeeds.

- [ ] **Step 7: Run the full partidas suite to confirm nothing regressed**

Run: `dotnet test "services/partidas/Umbral.Partidas.sln"`
Expected: PASS (all prior tests + the new round-trip test).

- [ ] **Step 8: Commit**

```bash
git add services/partidas/src/Umbral.Partidas.Infrastructure services/partidas/tests/Umbral.Partidas.IntegrationTests/PartidaPersistenceTests.cs
git commit -m "feat(partidas): map Partida/JuegoTrivia/JuegoBDT in EF and add initial migration"
```

---

### Task 11: Infrastructure — repositories + unit of work + DI

**Files:**
- Create: `services/partidas/src/Umbral.Partidas.Infrastructure/Persistence/PartidaRepository.cs`
- Create: `services/partidas/src/Umbral.Partidas.Infrastructure/Persistence/JuegoTriviaRepository.cs`
- Create: `services/partidas/src/Umbral.Partidas.Infrastructure/Persistence/JuegoBDTRepository.cs`
- Create: `services/partidas/src/Umbral.Partidas.Infrastructure/Persistence/PartidasUnitOfWork.cs`
- Modify: `services/partidas/src/Umbral.Partidas.Infrastructure/DependencyInjection.cs`
- Test: `services/partidas/tests/Umbral.Partidas.IntegrationTests/PartidaRepositoryTests.cs`

**Interfaces:**
- Consumes: `IPartidaRepository`, `IJuegoTriviaRepository`, `IJuegoBDTRepository`, `IPartidasUnitOfWork` (Tasks 5,7,8), `PartidasDbContext` (Task 10).
- Produces: concrete repos + `PartidasUnitOfWork`; all registered scoped in `AddPartidasInfrastructure`, all sharing the one scoped `PartidasDbContext` so a single `SaveChangesAsync` commits cross-aggregate writes atomically.

- [ ] **Step 1: Write the failing repository + unit-of-work tests**

```csharp
// tests/Umbral.Partidas.IntegrationTests/PartidaRepositoryTests.cs
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Umbral.Partidas.Domain.Entities;
using Umbral.Partidas.Domain.Enums;
using Umbral.Partidas.Domain.ValueObjects;
using Umbral.Partidas.Infrastructure.Persistence;

namespace Umbral.Partidas.IntegrationTests;

public class PartidaRepositoryTests
{
    private static PartidasDbContext NewContext(string dbName) =>
        new(new DbContextOptionsBuilder<PartidasDbContext>().UseInMemoryDatabase(dbName).Options);

    [Fact]
    public async Task Add_and_GetById_round_trips_partida()
    {
        var dbName = Guid.NewGuid().ToString();
        var partida = Partida.Crear(NombrePartida.Crear("Copa"), Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10);

        await using (var ctx = NewContext(dbName))
        {
            var repo = new PartidaRepository(ctx);
            var uow = new PartidasUnitOfWork(ctx);
            repo.Add(partida);
            await uow.SaveChangesAsync(CancellationToken.None);
        }

        await using (var ctx = NewContext(dbName))
        {
            var repo = new PartidaRepository(ctx);
            var loaded = await repo.GetByIdAsync(partida.PartidaId, CancellationToken.None);
            Assert.NotNull(loaded);
            Assert.Equal("Copa", loaded!.NombrePartida.Valor);
        }
    }

    [Fact]
    public async Task UnitOfWork_commits_partida_and_trivia_in_one_save()
    {
        var dbName = Guid.NewGuid().ToString();
        var partida = Partida.Crear(NombrePartida.Crear("Copa"), Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10);

        await using (var ctx = NewContext(dbName))
        {
            new PartidaRepository(ctx).Add(partida);
            await new PartidasUnitOfWork(ctx).SaveChangesAsync(CancellationToken.None);
        }

        await using (var ctx = NewContext(dbName))
        {
            var partidaRepo = new PartidaRepository(ctx);
            var triviaRepo = new JuegoTriviaRepository(ctx);
            var uow = new PartidasUnitOfWork(ctx);

            var loaded = await partidaRepo.GetByIdAsync(partida.PartidaId, CancellationToken.None);
            var trivia = JuegoTrivia.Crear(loaded!.PartidaId, 1, new[]
            {
                new PreguntaSpec("Q", new List<OpcionSpec> { new("A", true), new("B", false) }, 10, 30)
            });
            loaded.AgregarJuego(trivia.JuegoId, 1, TipoJuego.Trivia);
            triviaRepo.Add(trivia);
            partidaRepo.Update(loaded);
            await uow.SaveChangesAsync(CancellationToken.None);
        }

        await using (var ctx = NewContext(dbName))
        {
            var reloaded = await new PartidaRepository(ctx).GetByIdAsync(partida.PartidaId, CancellationToken.None);
            Assert.Single(reloaded!.Juegos);
            var trivias = await new JuegoTriviaRepository(ctx).GetByPartidaIdAsync(partida.PartidaId, CancellationToken.None);
            Assert.Single(trivias);
        }
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test "services/partidas/tests/Umbral.Partidas.IntegrationTests/Umbral.Partidas.IntegrationTests.csproj" --filter "FullyQualifiedName~PartidaRepositoryTests"`
Expected: FAIL — `PartidaRepository`, `JuegoTriviaRepository`, `PartidasUnitOfWork` do not exist.

- [ ] **Step 3: Implement the repositories**

```csharp
// Infrastructure/Persistence/PartidaRepository.cs
using Microsoft.EntityFrameworkCore;
using Umbral.Partidas.Domain.Abstractions.Persistence;
using Umbral.Partidas.Domain.Entities;
using Umbral.Partidas.Domain.ValueObjects;

namespace Umbral.Partidas.Infrastructure.Persistence;

public sealed class PartidaRepository : IPartidaRepository
{
    private readonly PartidasDbContext _dbContext;

    public PartidaRepository(PartidasDbContext dbContext) => _dbContext = dbContext;

    public void Add(Partida partida) => _dbContext.Partidas.Add(partida);

    public void Update(Partida partida) => _dbContext.Partidas.Update(partida);

    public Task<Partida?> GetByIdAsync(PartidaId id, CancellationToken cancellationToken)
        => _dbContext.Partidas.Include(p => p.Juegos)
            .FirstOrDefaultAsync(p => p.PartidaId == id, cancellationToken);

    public async Task<IReadOnlyList<Partida>> ListAsync(CancellationToken cancellationToken)
        => await _dbContext.Partidas.Include(p => p.Juegos).ToListAsync(cancellationToken);
}
```

```csharp
// Infrastructure/Persistence/JuegoTriviaRepository.cs
using Microsoft.EntityFrameworkCore;
using Umbral.Partidas.Domain.Abstractions.Persistence;
using Umbral.Partidas.Domain.Entities;
using Umbral.Partidas.Domain.ValueObjects;

namespace Umbral.Partidas.Infrastructure.Persistence;

public sealed class JuegoTriviaRepository : IJuegoTriviaRepository
{
    private readonly PartidasDbContext _dbContext;

    public JuegoTriviaRepository(PartidasDbContext dbContext) => _dbContext = dbContext;

    public void Add(JuegoTrivia juego) => _dbContext.JuegosTrivia.Add(juego);

    public async Task<IReadOnlyList<JuegoTrivia>> GetByPartidaIdAsync(PartidaId partidaId, CancellationToken cancellationToken)
        => await _dbContext.JuegosTrivia
            .Include(j => j.Preguntas).ThenInclude(p => p.Opciones)
            .Where(j => j.PartidaId == partidaId)
            .ToListAsync(cancellationToken);
}
```

```csharp
// Infrastructure/Persistence/JuegoBDTRepository.cs
using Microsoft.EntityFrameworkCore;
using Umbral.Partidas.Domain.Abstractions.Persistence;
using Umbral.Partidas.Domain.Entities;
using Umbral.Partidas.Domain.ValueObjects;

namespace Umbral.Partidas.Infrastructure.Persistence;

public sealed class JuegoBDTRepository : IJuegoBDTRepository
{
    private readonly PartidasDbContext _dbContext;

    public JuegoBDTRepository(PartidasDbContext dbContext) => _dbContext = dbContext;

    public void Add(JuegoBDT juego) => _dbContext.JuegosBDT.Add(juego);

    public async Task<IReadOnlyList<JuegoBDT>> GetByPartidaIdAsync(PartidaId partidaId, CancellationToken cancellationToken)
        => await _dbContext.JuegosBDT
            .Include(j => j.Etapas)
            .Where(j => j.PartidaId == partidaId)
            .ToListAsync(cancellationToken);
}
```

```csharp
// Infrastructure/Persistence/PartidasUnitOfWork.cs
using Umbral.Partidas.Domain.Abstractions.Persistence;

namespace Umbral.Partidas.Infrastructure.Persistence;

public sealed class PartidasUnitOfWork : IPartidasUnitOfWork
{
    private readonly PartidasDbContext _dbContext;

    public PartidasUnitOfWork(PartidasDbContext dbContext) => _dbContext = dbContext;

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
```

- [ ] **Step 4: Register repositories and unit of work in DI**

Modify `services/partidas/src/Umbral.Partidas.Infrastructure/DependencyInjection.cs` — add the using and the registrations inside `AddPartidasInfrastructure`, after the `AddDbContext` block and before `return services;`:

```csharp
using Umbral.Partidas.Domain.Abstractions.Persistence;
```

```csharp
        services.AddScoped<IPartidaRepository, PartidaRepository>();
        services.AddScoped<IJuegoTriviaRepository, JuegoTriviaRepository>();
        services.AddScoped<IJuegoBDTRepository, JuegoBDTRepository>();
        services.AddScoped<IPartidasUnitOfWork, PartidasUnitOfWork>();
```

The full method body after edit:

```csharp
    public static IServiceCollection AddPartidasInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PartidasDatabase");

        services.AddDbContext<PartidasDbContext>(options =>
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                options.UseInMemoryDatabase("partidas-dev");
            }
            else
            {
                options.UseNpgsql(connectionString);
            }
        });

        services.AddScoped<IPartidaRepository, PartidaRepository>();
        services.AddScoped<IJuegoTriviaRepository, JuegoTriviaRepository>();
        services.AddScoped<IJuegoBDTRepository, JuegoBDTRepository>();
        services.AddScoped<IPartidasUnitOfWork, PartidasUnitOfWork>();

        return services;
    }
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test "services/partidas/tests/Umbral.Partidas.IntegrationTests/Umbral.Partidas.IntegrationTests.csproj" --filter "FullyQualifiedName~PartidaRepositoryTests"`
Expected: PASS (2 tests).

- [ ] **Step 6: Commit**

```bash
git add services/partidas/src/Umbral.Partidas.Infrastructure services/partidas/tests/Umbral.Partidas.IntegrationTests/PartidaRepositoryTests.cs
git commit -m "feat(partidas): add repositories, unit of work and DI registration"
```

---

### Task 12: Api — `PartidasController` + controller unit tests

**Files:**
- Create: `services/partidas/src/Umbral.Partidas.Api/Contracts/PartidaRequests.cs`
- Create: `services/partidas/src/Umbral.Partidas.Api/Controllers/PartidasController.cs`
- Create: `services/partidas/tests/Umbral.Partidas.UnitTests/Api/FakeSender.cs`
- Test: `services/partidas/tests/Umbral.Partidas.UnitTests/Api/PartidasControllerTests.cs`

**Interfaces:**
- Consumes: commands, queries, DTOs, validators (Tasks 5,7,8,9), `ISender` (MediatR).
- Produces: `PartidasController : ControllerBase` exposing `POST /partidas`, `POST /partidas/{partidaId}/juegos/trivia`, `POST /partidas/{partidaId}/juegos/bdt`, `GET /partidas/{partidaId}` (action name `GetPartida`), `GET /partidas`. Validation via injected `IValidator<T>` → 400 `ValidationProblemDetails`. Request records `CrearPartidaRequest`, `AgregarJuegoTriviaRequest`, `AgregarJuegoBDTRequest`. `Program.cs` is already wired (`AddPartidasApplication` registers validators + MediatR; `AddControllers` + `MapControllers` discover the controller) — no change needed.

- [ ] **Step 1: Write the failing controller unit tests**

```csharp
// tests/Umbral.Partidas.UnitTests/Api/FakeSender.cs
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Umbral.Partidas.UnitTests.Api;

public sealed class FakeSender : ISender
{
    private readonly object? _response;
    public object? LastRequest { get; private set; }

    public FakeSender(object? response) => _response = response;

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        LastRequest = request;
        return Task.FromResult((TResponse)_response!);
    }

    public Task<object?> Send(object request, CancellationToken cancellationToken = default)
    {
        LastRequest = request;
        return Task.FromResult(_response);
    }

    // MediatR 12 ISender exposes a generic void-request Send; SP-2 commands all return a
    // response so this overload is never exercised, but the interface still requires it.
    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest
    {
        LastRequest = request;
        return Task.CompletedTask;
    }

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
}
```

> Note: the exact `ISender` member set is MediatR-12-specific. If the build reports an unimplemented `ISender` member, add the missing overload exactly as the compiler names it — do not change the controller.

```csharp
// tests/Umbral.Partidas.UnitTests/Api/PartidasControllerTests.cs
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Umbral.Partidas.Api.Contracts;
using Umbral.Partidas.Api.Controllers;
using Umbral.Partidas.Application.DTOs;
using Umbral.Partidas.Application.Validators;
using Umbral.Partidas.Domain.Enums;

namespace Umbral.Partidas.UnitTests.Api;

public class PartidasControllerTests
{
    [Fact]
    public async Task CrearPartida_valid_returns_201_created()
    {
        var response = new CrearPartidaResponse(Guid.NewGuid());
        var controller = new PartidasController(new FakeSender(response));
        var request = new CrearPartidaRequest("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10);

        var result = await controller.CrearPartida(request, new CrearPartidaCommandValidator(), CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, created.StatusCode);
        Assert.Same(response, created.Value);
    }

    [Fact]
    public async Task CrearPartida_invalid_returns_400()
    {
        var controller = new PartidasController(new FakeSender(new CrearPartidaResponse(Guid.NewGuid())));
        var request = new CrearPartidaRequest("", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10);

        var result = await controller.CrearPartida(request, new CrearPartidaCommandValidator(), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task AgregarJuegoTrivia_valid_returns_201()
    {
        var response = new AgregarJuegoResponse(Guid.NewGuid());
        var controller = new PartidasController(new FakeSender(response));
        var request = new AgregarJuegoTriviaRequest(1, new List<PreguntaRequest>
        {
            new("Q", new List<OpcionRequest> { new("A", true), new("B", false) }, 10, 30)
        });

        var result = await controller.AgregarJuegoTrivia(Guid.NewGuid(), request, new AgregarJuegoTriviaCommandValidator(), CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(result);
    }

    [Fact]
    public async Task AgregarJuegoBDT_valid_returns_201()
    {
        var response = new AgregarJuegoResponse(Guid.NewGuid());
        var controller = new PartidasController(new FakeSender(response));
        var request = new AgregarJuegoBDTRequest(1, "Plaza", new List<EtapaRequest> { new(1, "QR", 50, 120) });

        var result = await controller.AgregarJuegoBDT(Guid.NewGuid(), request, new AgregarJuegoBDTCommandValidator(), CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(result);
    }

    [Fact]
    public async Task GetPartida_returns_200()
    {
        var detail = new PartidaDetailDto(Guid.NewGuid(), "Copa", "Individual", "Manual", null, 1, 10, null, new List<JuegoDto>());
        var controller = new PartidasController(new FakeSender(detail));

        var result = await controller.GetPartida(Guid.NewGuid(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(detail, ok.Value);
    }

    [Fact]
    public async Task ListPartidas_returns_200()
    {
        IReadOnlyList<PartidaSummaryDto> list = new List<PartidaSummaryDto>();
        var controller = new PartidasController(new FakeSender(list));

        var result = await controller.ListPartidas(CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test "services/partidas/tests/Umbral.Partidas.UnitTests/Umbral.Partidas.UnitTests.csproj" --filter "FullyQualifiedName~PartidasControllerTests"`
Expected: FAIL — `PartidasController`, request records do not exist.

- [ ] **Step 3: Implement the request records**

```csharp
// Api/Contracts/PartidaRequests.cs
using Umbral.Partidas.Application.DTOs;
using Umbral.Partidas.Domain.Enums;

namespace Umbral.Partidas.Api.Contracts;

public sealed record CrearPartidaRequest(
    string NombrePartida,
    Modalidad Modalidad,
    ModoInicioPartida ModoInicioPartida,
    DateTime? TiempoInicio,
    int MinimosParticipacion,
    int MaximosParticipacion);

public sealed record AgregarJuegoTriviaRequest(
    int Orden,
    IReadOnlyList<PreguntaRequest> Preguntas);

public sealed record AgregarJuegoBDTRequest(
    int Orden,
    string AreaBusqueda,
    IReadOnlyList<EtapaRequest> Etapas);
```

- [ ] **Step 4: Implement `PartidasController`**

```csharp
// Api/Controllers/PartidasController.cs
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Umbral.Partidas.Api.Contracts;
using Umbral.Partidas.Application.Commands;
using Umbral.Partidas.Application.Queries;

namespace Umbral.Partidas.Api.Controllers;

[ApiController]
[Route("partidas")]
public sealed class PartidasController : ControllerBase
{
    private readonly ISender _mediator;

    public PartidasController(ISender mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> CrearPartida(
        [FromBody] CrearPartidaRequest request,
        [FromServices] IValidator<CrearPartidaCommand> validator,
        CancellationToken cancellationToken)
    {
        var command = new CrearPartidaCommand(
            request.NombrePartida,
            request.Modalidad,
            request.ModoInicioPartida,
            request.TiempoInicio,
            request.MinimosParticipacion,
            request.MaximosParticipacion);

        var validationResult = await ValidateAsync(validator, command, cancellationToken);
        if (validationResult is not null) return validationResult;

        var response = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetPartida), new { partidaId = response.PartidaId }, response);
    }

    [HttpPost("{partidaId:guid}/juegos/trivia")]
    public async Task<IActionResult> AgregarJuegoTrivia(
        Guid partidaId,
        [FromBody] AgregarJuegoTriviaRequest request,
        [FromServices] IValidator<AgregarJuegoTriviaCommand> validator,
        CancellationToken cancellationToken)
    {
        var command = new AgregarJuegoTriviaCommand(partidaId, request.Orden, request.Preguntas);

        var validationResult = await ValidateAsync(validator, command, cancellationToken);
        if (validationResult is not null) return validationResult;

        var response = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetPartida), new { partidaId }, response);
    }

    [HttpPost("{partidaId:guid}/juegos/bdt")]
    public async Task<IActionResult> AgregarJuegoBDT(
        Guid partidaId,
        [FromBody] AgregarJuegoBDTRequest request,
        [FromServices] IValidator<AgregarJuegoBDTCommand> validator,
        CancellationToken cancellationToken)
    {
        var command = new AgregarJuegoBDTCommand(partidaId, request.Orden, request.AreaBusqueda, request.Etapas);

        var validationResult = await ValidateAsync(validator, command, cancellationToken);
        if (validationResult is not null) return validationResult;

        var response = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetPartida), new { partidaId }, response);
    }

    [HttpGet("{partidaId:guid}")]
    public async Task<IActionResult> GetPartida(Guid partidaId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPartidaByIdQuery(partidaId), cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> ListPartidas(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ListPartidasQuery(), cancellationToken);
        return Ok(result);
    }

    private async Task<IActionResult?> ValidateAsync<TCommand>(
        IValidator<TCommand> validator,
        TCommand command,
        CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(command, cancellationToken);
        if (result.IsValid) return null;

        foreach (var error in result.Errors)
            ModelState.AddModelError(error.PropertyName, error.ErrorMessage);

        return new BadRequestObjectResult(new ValidationProblemDetails(ModelState));
    }
}
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test "services/partidas/tests/Umbral.Partidas.UnitTests/Umbral.Partidas.UnitTests.csproj" --filter "FullyQualifiedName~PartidasControllerTests"`
Expected: PASS (6 tests).

- [ ] **Step 6: Commit**

```bash
git add services/partidas/src/Umbral.Partidas.Api services/partidas/tests/Umbral.Partidas.UnitTests/Api
git commit -m "feat(partidas): add PartidasController with config endpoints and unit tests"
```

---

### Task 13: Contracts — `partidas-config.md` + contract tests

**Files:**
- Create: `contracts/http/partidas-config.md`
- Test: `services/partidas/tests/Umbral.Partidas.ContractTests/PartidasConfigEndpointsTests.cs`

**Interfaces:**
- Consumes: the running Api via `WebApplicationFactory<Program>` (InMemory fallback — no `PartidasDatabase` connection string in tests).
- Produces: the HTTP contract document + end-to-end shape/status assertions for all five endpoints.

- [ ] **Step 1: Write the contract document**

Create `contracts/http/partidas-config.md`:

```markdown
# Partidas — Configuration HTTP Contract (SP-2)

Service: **Partidas** (`umbral_partidas`). Base path: `/partidas` (through the YARP gateway in production). Configuration only: create a partida header, add fully-formed games. No publish/lobby/runtime (SP-3), no scoring (SP-4).

Enums are serialized as their string name. `estado` is `null` until the partida is published (SP-3).

## POST /partidas
Create a partida header (no games yet).

Request:
```json
{
  "nombrePartida": "Copa UMBRAL",
  "modalidad": "Individual",
  "modoInicioPartida": "Manual",
  "tiempoInicio": null,
  "minimosParticipacion": 1,
  "maximosParticipacion": 10
}
```
- `modalidad`: `Individual` | `Equipo`
- `modoInicioPartida`: `Manual` | `Automatico` | `ManualYAutomatico`
- `tiempoInicio`: required iff `modoInicioPartida` is `Automatico` or `ManualYAutomatico`; must be null for `Manual`.
- `maximosParticipacion >= minimosParticipacion >= 1`.

Responses:
- `201 Created` → `{ "partidaId": "<guid>" }`, `Location: /partidas/{partidaId}`
- `400 Bad Request` → `{ "message": "..." }` or `ValidationProblemDetails` on invalid input.

## POST /partidas/{partidaId}/juegos/trivia
Add one Trivia game with its full question set.

Request:
```json
{
  "orden": 1,
  "preguntas": [
    {
      "texto": "Capital de Francia?",
      "opciones": [ { "texto": "Paris", "esCorrecta": true }, { "texto": "Londres", "esCorrecta": false } ],
      "puntaje": 10,
      "tiempoLimiteSegundos": 30
    }
  ]
}
```
- At least one question; each question: ≥2 options, exactly one `esCorrecta: true`, `puntaje > 0`, `tiempoLimiteSegundos > 0`.

Responses:
- `201 Created` → `{ "juegoId": "<guid>" }`, `Location: /partidas/{partidaId}`
- `400` invalid content · `404` partida not found · `409` duplicate `orden`/game in the partida.

## POST /partidas/{partidaId}/juegos/bdt
Add one Búsqueda del Tesoro game with its full stage set.

Request:
```json
{
  "orden": 2,
  "areaBusqueda": "Plaza central",
  "etapas": [
    { "orden": 1, "codigoQREsperado": "QR-TEXT", "puntaje": 50, "tiempoLimiteSegundos": 120 }
  ]
}
```
- Non-empty `areaBusqueda`; at least one stage with contiguous `orden` from 1; each stage: non-empty `codigoQREsperado`, `puntaje > 0`, `tiempoLimiteSegundos > 0`.

Responses:
- `201 Created` → `{ "juegoId": "<guid>" }` · `400` · `404` · `409`.

## GET /partidas/{partidaId}
Review a partida and its configured games (ordered).

Response `200 OK`:
```json
{
  "partidaId": "<guid>",
  "nombrePartida": "Copa UMBRAL",
  "modalidad": "Individual",
  "modoInicioPartida": "Manual",
  "tiempoInicio": null,
  "minimosParticipacion": 1,
  "maximosParticipacion": 10,
  "estado": null,
  "juegos": [
    { "juegoId": "<guid>", "orden": 1, "tipoJuego": "Trivia", "estado": "Pendiente",
      "trivia": { "preguntas": [ { "preguntaId": "<guid>", "texto": "...", "puntajeAsignado": 10, "tiempoLimiteSegundos": 30, "opciones": [ { "opcionId": "<guid>", "texto": "...", "esCorrecta": true } ] } ] },
      "bdt": null },
    { "juegoId": "<guid>", "orden": 2, "tipoJuego": "BusquedaDelTesoro", "estado": "Pendiente",
      "trivia": null,
      "bdt": { "areaBusqueda": "Plaza central", "etapas": [ { "etapaBDTId": "<guid>", "orden": 1, "codigoQREsperado": "QR-TEXT", "puntajeAsignado": 50, "tiempoLimiteSegundos": 120 } ] } }
  ]
}
```
- `404 Not Found` when the partida does not exist.

## GET /partidas
List partida summaries.

Response `200 OK`:
```json
[ { "partidaId": "<guid>", "nombrePartida": "Copa UMBRAL", "modalidad": "Individual", "modoInicioPartida": "Manual", "tiempoInicio": null, "minimosParticipacion": 1, "maximosParticipacion": 10, "estado": null, "cantidadJuegos": 2 } ]
```
```

- [ ] **Step 2: Write the failing contract tests**

```csharp
// tests/Umbral.Partidas.ContractTests/PartidasConfigEndpointsTests.cs
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Umbral.Partidas.Application.DTOs;

namespace Umbral.Partidas.ContractTests;

public class PartidasConfigEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public PartidasConfigEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private static object CrearPartidaBody(string nombre = "Copa") => new
    {
        nombrePartida = nombre,
        modalidad = "Individual",
        modoInicioPartida = "Manual",
        tiempoInicio = (DateTime?)null,
        minimosParticipacion = 1,
        maximosParticipacion = 10
    };

    [Fact]
    public async Task Full_config_flow_returns_expected_shapes()
    {
        var create = await _client.PostAsJsonAsync("/partidas", CrearPartidaBody("Copa-" + Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<CrearPartidaResponse>();
        Assert.NotNull(created);
        var partidaId = created!.PartidaId;

        var triviaBody = new
        {
            orden = 1,
            preguntas = new[]
            {
                new { texto = "Q", opciones = new[] { new { texto = "A", esCorrecta = true }, new { texto = "B", esCorrecta = false } }, puntaje = 10, tiempoLimiteSegundos = 30 }
            }
        };
        var addTrivia = await _client.PostAsJsonAsync($"/partidas/{partidaId}/juegos/trivia", triviaBody);
        Assert.Equal(HttpStatusCode.Created, addTrivia.StatusCode);

        var bdtBody = new
        {
            orden = 2,
            areaBusqueda = "Plaza",
            etapas = new[] { new { orden = 1, codigoQREsperado = "QR", puntaje = 50, tiempoLimiteSegundos = 120 } }
        };
        var addBdt = await _client.PostAsJsonAsync($"/partidas/{partidaId}/juegos/bdt", bdtBody);
        Assert.Equal(HttpStatusCode.Created, addBdt.StatusCode);

        var detail = await _client.GetFromJsonAsync<PartidaDetailDto>($"/partidas/{partidaId}");
        Assert.NotNull(detail);
        Assert.Null(detail!.Estado);
        Assert.Equal(2, detail.Juegos.Count);
        Assert.Equal("Trivia", detail.Juegos[0].TipoJuego);
        Assert.Equal("BusquedaDelTesoro", detail.Juegos[1].TipoJuego);
        Assert.NotNull(detail.Juegos[0].Trivia);
        Assert.NotNull(detail.Juegos[1].BDT);

        var list = await _client.GetFromJsonAsync<List<PartidaSummaryDto>>("/partidas");
        Assert.NotNull(list);
        Assert.Contains(list!, p => p.PartidaId == partidaId && p.CantidadJuegos == 2);
    }

    [Fact]
    public async Task Add_game_to_missing_partida_returns_404()
    {
        var triviaBody = new
        {
            orden = 1,
            preguntas = new[]
            {
                new { texto = "Q", opciones = new[] { new { texto = "A", esCorrecta = true }, new { texto = "B", esCorrecta = false } }, puntaje = 10, tiempoLimiteSegundos = 30 }
            }
        };
        var response = await _client.PostAsJsonAsync($"/partidas/{Guid.NewGuid()}/juegos/trivia", triviaBody);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_partida_with_blank_name_returns_400()
    {
        var response = await _client.PostAsJsonAsync("/partidas", CrearPartidaBody(""));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_missing_partida_returns_404()
    {
        var response = await _client.GetAsync($"/partidas/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
```

- [ ] **Step 3: Run the tests to verify they fail, then pass**

Run: `dotnet test "services/partidas/tests/Umbral.Partidas.ContractTests/Umbral.Partidas.ContractTests.csproj"`
Expected: initially FAIL if any wiring is incomplete; after confirming the endpoints/middleware from Tasks 6-12 are in place, PASS (4 tests). The contract tests exercise the real DI graph end-to-end on the InMemory provider (no Postgres). Use systematic-debugging for any failure; do not relax assertions.

- [ ] **Step 4: Commit**

```bash
git add contracts/http/partidas-config.md services/partidas/tests/Umbral.Partidas.ContractTests/PartidasConfigEndpointsTests.cs
git commit -m "feat(partidas): add partidas-config HTTP contract and end-to-end contract tests"
```

---

### Task 14: R1 structural gate + full suite + traceability

**Files:**
- Modify: `docs/04-sdd/traceability-matrix.md` (add SP-2 rows if the file tracks migration slices; otherwise skip with a note)
- No source changes (verification + docs only).

**Interfaces:**
- Consumes: the completed service from Tasks 1-13.
- Produces: a passing R1 structural gate and a green full suite; recorded in the ledger.

- [ ] **Step 1: Verify the graded structure (R1 checklist)**

Confirm by inspection (each must hold):
- `Api/Controllers/` holds `PartidasController` + `HealthController`; `Program.cs` registers controllers only via `MapControllers` (no minimal-API route handlers).
- `Application/` top-level folders are exactly `Commands/`, `Queries/`, `Interfaces/`, `Validators/`, `DTOs/`, `Handlers/`, `Handlers/Commands/`, `Handlers/Queries/`, `Exceptions/` (no per-feature slice folders).
- Repository interfaces live in `Domain/Abstractions/Persistence/`.
- `Infrastructure/` contains `Persistence/` and `Services/` (create an empty `Services/.gitkeep` if no service classes exist yet, to match the standardized layout).
- `ExceptionHandlingMiddleware` is registered in `Program.cs` and maps domain/application exceptions.
- Every controller has unit tests (`HealthControllerTests`, `PartidasControllerTests`).

Run a quick structural check:

```bash
ls services/partidas/src/Umbral.Partidas.Application
ls services/partidas/src/Umbral.Partidas.Domain/Abstractions/Persistence
ls services/partidas/src/Umbral.Partidas.Infrastructure
grep -n "MapControllers" services/partidas/src/Umbral.Partidas.Api/Program.cs
```
Expected: folder sets match the checklist; `MapControllers` present.

- [ ] **Step 2: Run the full Partidas solution test suite**

Run: `dotnet test "services/partidas/Umbral.Partidas.sln"`
Expected: PASS — all domain, application, controller, repository, persistence and contract tests green; 0 failed.

- [ ] **Step 3: Confirm old services are untouched**

Run: `git status --short services/trivia-game-service services/bdt-game-service`
Expected: no changes (SP-2 must not modify the old services).

- [ ] **Step 4: Update traceability (if applicable)**

If `docs/04-sdd/traceability-matrix.md` tracks migration slices, add a row noting SP-2 delivered the Partida/Juego model + Partidas configuration in `services/partidas`. If it tracks only HUs (not migration slices), leave it and note that in the commit message.

- [ ] **Step 5: Commit**

```bash
git add -A docs/04-sdd/traceability-matrix.md
git commit -m "docs(sp2): record SP-2 Partidas config completion and R1 gate pass" --allow-empty
```

- [ ] **Step 6: Hand off to finishing-a-development-branch**

After the full suite is green and the R1 gate passes, invoke the `superpowers:finishing-a-development-branch` skill to run the final whole-branch review and decide merge/PR for `feature/code-migration-SP-2`.

---

## Self-Review Notes (author)

- **Spec coverage:** §3 IN items map as — model+aggregates → Tasks 1-4; create/config commands → Tasks 5,7,8; review queries → Task 9; persistence+migration → Tasks 10-11; HTTP contract → Task 13; tests (unit/application/contract/controller) → every task + 13; R1 gate → Task 14. §3 OUT items (publish/lobby/runtime, scoring, client repoint) are explicitly excluded and unreferenced by any task. SEAM (§4.3) realized in Tasks 2, 9, 10. Inherited minors (§11): A deferred (middleware keeps `ex.Message` for 500), B note-only (no gateway change). 
- **Placeholder scan:** none — every code/test step carries full source.
- **Type consistency:** repository/UoW signatures defined in Task 5 are reused verbatim by Tasks 7-11; `AgregarJuegoResponse` defined in Task 7 reused in Task 8; DTO names in Task 9 reused in Tasks 12-13; `PartidaId`/`JuegoId`/`PuntajeAsignado`/`NombrePartida` VO members consistent across domain, EF mapping and DTO projection.
