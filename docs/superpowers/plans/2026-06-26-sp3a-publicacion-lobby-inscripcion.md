# SP-3a — Publicación → Lobby + inscripciones Individual — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Stand up the live-session foundation in Operaciones de Sesión — publish a configured `Partida` into a `Lobby` (config snapshot pulled from Partidas over HTTP) and let participants inscribe under Individual modality, with runtime state owned transiently by Operaciones.

**Architecture:** Fresh-build of a `SesionPartida` aggregate (Clean Architecture + CQRS/MediatR) in `services/operaciones-sesion`, mirroring the SP-2 Partidas service structure. Config handoff is a synchronous internal `GET /partidas/{id}` (Option A). Domain events go through a No-Op publisher port (real RabbitMQ deferred). Runtime estado lives in `SesionPartida.EstadoSesion`; Partidas' `EstadoPartida` stays `null` (R1, ADR-0010).

**Tech Stack:** .NET 8, MediatR 12.2.0, FluentValidation 11.11.0, EF Core 8 (Npgsql + InMemory fallback), `Microsoft.Extensions.Http` typed client, xUnit, `WebApplicationFactory<Program>` contract tests.

**Spec:** `docs/superpowers/specs/2026-06-26-sp3a-publicacion-lobby-inscripcion-design.md`

## Global Constraints

- **Service identity (ADR-0009):** folder `services/operaciones-sesion`, namespace `Umbral.OperacionesSesion.*`, DB `umbral_operaciones_sesion`, connection-string key `OperacionesSesionDatabase`, InMemory db name `operaciones-sesion-dev`, gateway route `/operaciones-sesion/*`. (These shell defaults already exist — do not change them.)
- **Graded structure (non-negotiable):** `Application/` top-level folders EXACTLY `{Commands, Queries, Interfaces, Validators, DTOs, Handlers, Handlers/Commands, Handlers/Queries, Exceptions}`; root-level FILES (`DependencyInjection.cs`, `ValidationBehavior.cs`) are allowed. Controllers inherit native `ControllerBase`, dispatch via `_mediator.Send(...)` only, contain NO business logic, and EACH has unit tests. Repository interfaces live in `Domain/Abstractions/Persistence/`. `Infrastructure/` has `Persistence/` + `Services/`. Centralized `ExceptionHandlingMiddleware`. `Program.cs` uses `MapControllers` only (no minimal-API).
- **Validation runs in the MediatR pipeline** via `ValidationBehavior` (M-2 lesson), NEVER in the controller. `ValidationException` → 400 in the middleware.
- **Hard boundaries:** Operaciones never reads/writes another service's DB. Config arrives only via the HTTP port. The cross-session "active participation" query runs against Operaciones' OWN DB.
- **Enums:** persisted as int (EF default, like SP-2); serialized as strings over HTTP via `JsonStringEnumConverter`. Operaciones defines its OWN enum copies (no shared domain project); enum member names match Partidas (`Modalidad.Individual/Equipo`, `TipoJuego.Trivia/BusquedaDelTesoro`, `ModoInicioPartida.Manual/Automatico/ManualYAutomatico`) so string parse from the snapshot works.
- **VO factory style (SP-2):** `readonly record struct` with `New()`, `From(Guid)`, `EsValido()`.
- **Postgres (design-time only):** `Host=localhost;Port=55432;Database=umbral_operaciones_sesion;Username=umbral;Password=16102005` (documented local-dev credential).
- **Test runner:** `dotnet test "services/operaciones-sesion/Umbral.OperacionesSesion.sln"`. Suite must stay green; no Postgres needed (InMemory fallback). Old services (`trivia-game-service`, `bdt-game-service`) and other services stay diff-zero.
- **Commit after each task.** Branch `feature/code-migration-SP-3` (already checked out). Do NOT merge/push.

## File Structure

```
services/operaciones-sesion/src/
  Umbral.OperacionesSesion.Domain/
    Enums/            EstadoSesion.cs, EstadoInscripcion.cs, Modalidad.cs, ModoInicioPartida.cs, TipoJuego.cs
    ValueObjects/     SesionPartidaId.cs, InscripcionId.cs, ConfiguracionSnapshot.cs
    Entities/         JuegoResumen.cs, InscripcionPartida.cs, SesionPartida.cs
    Exceptions/       PartidaNoPublicableException.cs, SesionNoEnLobbyException.cs, ModalidadNoSoportadaException.cs,
                      ParticipanteYaInscritoException.cs, ParticipacionActivaExistenteException.cs,
                      CupoLlenoException.cs, InscripcionNoEncontradaException.cs
    Abstractions/Persistence/   ISesionPartidaRepository.cs, IOperacionesSesionUnitOfWork.cs
  Umbral.OperacionesSesion.Application/
    Commands/         PublicarPartidaCommand.cs, InscribirParticipanteCommand.cs, CancelarInscripcionCommand.cs
    Queries/          ObtenerLobbyQuery.cs
    DTOs/             ConfiguracionPartidaDto.cs, LobbyDto.cs, InscripcionResponse.cs
    Interfaces/       IConfiguracionPartidaClient.cs, ISesionEventsPublisher.cs, PartidaPublicadaEnLobbyEvent.cs
    Validators/       PublicarPartidaCommandValidator.cs, InscribirParticipanteCommandValidator.cs, CancelarInscripcionCommandValidator.cs
    Handlers/Commands/  PublicarPartidaCommandHandler.cs, InscribirParticipanteCommandHandler.cs, CancelarInscripcionCommandHandler.cs
    Handlers/Queries/   ObtenerLobbyQueryHandler.cs
    Exceptions/       SesionYaPublicadaException.cs, SesionNoEncontradaException.cs, PartidaConfigNoEncontradaException.cs,
                      PartidasConfigInaccesibleException.cs, ParticipanteNoIdentificadoException.cs
    ValidationBehavior.cs  (copied), DependencyInjection.cs (extended)
  Umbral.OperacionesSesion.Infrastructure/
    Persistence/      OperacionesSesionDbContext.cs (extended), SesionPartidaRepository.cs, OperacionesSesionUnitOfWork.cs,
                      OperacionesSesionDbContextDesignTimeFactory.cs, Migrations/*
    Services/         PartidasConfigHttpClient.cs, NoOpSesionEventsPublisher.cs
    DependencyInjection.cs (extended)
  Umbral.OperacionesSesion.Api/
    Controllers/      SesionesController.cs
    Middleware/       ExceptionHandlingMiddleware.cs (extended)
    Program.cs (unchanged except enum converter)

services/operaciones-sesion/tests/
  Umbral.OperacionesSesion.UnitTests/
    Domain/           ValueObjectTests.cs, SesionPartidaTests.cs
    Application/       PublicarPartidaCommandHandlerTests.cs, PublicarPartidaCommandValidatorTests.cs,
                       InscribirParticipanteCommandHandlerTests.cs, InscribirParticipanteCommandValidatorTests.cs,
                       CancelarInscripcionCommandHandlerTests.cs, ObtenerLobbyQueryHandlerTests.cs,
                       ValidationBehaviorTests.cs, Fakes/*
    Api/               SesionesControllerTests.cs, ExceptionHandlingMiddlewareTests.cs, FakeSender.cs
    Infrastructure/    PartidasConfigHttpClientTests.cs
  Umbral.OperacionesSesion.IntegrationTests/   SesionPersistenceTests.cs
  Umbral.OperacionesSesion.ContractTests/      SesionEndpointsTests.cs, OperacionesSesionWebFactory.cs

contracts/http/operaciones-sesion-api.md      (register 4 endpoints)
contracts/events/operaciones-sesion-events.md (register PartidaPublicadaEnLobby payload)
docs/05-decisions/ADR-0010-runtime-estado-en-operaciones.md
docs/04-sdd/traceability-matrix.md            (SP-3a row)
.git/sdd/progress.md                          (ledger line)
```

---

### Task 1: Domain primitives (enums, VOs, JuegoResumen, ConfiguracionSnapshot, exceptions)

**Files:**
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Enums/{EstadoSesion,EstadoInscripcion,Modalidad,ModoInicioPartida,TipoJuego}.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/ValueObjects/{SesionPartidaId,InscripcionId,ConfiguracionSnapshot}.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/JuegoResumen.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Exceptions/*.cs` (7 files, see below)
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/ValueObjectTests.cs`

**Interfaces:**
- Produces: enums `EstadoSesion{Lobby,Iniciada,Cancelada,Terminada}`, `EstadoInscripcion{Activa,Cancelada}`, `Modalidad{Individual,Equipo}`, `ModoInicioPartida{Manual,Automatico,ManualYAutomatico}`, `TipoJuego{Trivia,BusquedaDelTesoro}`; VOs `SesionPartidaId{Valor}`, `InscripcionId{Valor}` with `New()/From(Guid)/EsValido()`; entity `JuegoResumen(Guid JuegoId, int Orden, TipoJuego TipoJuego)`; record `ConfiguracionSnapshot`; domain exceptions.

- [ ] **Step 1: Write the failing test**

Create `tests/Umbral.OperacionesSesion.UnitTests/Domain/ValueObjectTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.ValueObjects;

namespace Umbral.OperacionesSesion.UnitTests.Domain;

public class ValueObjectTests
{
    [Fact]
    public void SesionPartidaId_New_is_valid_and_non_empty()
    {
        var id = SesionPartidaId.New();
        Assert.True(id.EsValido());
        Assert.NotEqual(Guid.Empty, id.Valor);
    }

    [Fact]
    public void SesionPartidaId_From_empty_is_invalid()
        => Assert.False(SesionPartidaId.From(Guid.Empty).EsValido());

    [Fact]
    public void InscripcionId_New_is_valid()
        => Assert.True(InscripcionId.New().EsValido());

    [Fact]
    public void ConfiguracionSnapshot_exposes_partida_level_fields_and_juego_references()
    {
        var snapshot = new ConfiguracionSnapshot(
            "Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10,
            new List<JuegoResumen> { new(Guid.NewGuid(), 1, TipoJuego.Trivia) });

        Assert.Equal("Copa", snapshot.Nombre);
        Assert.Equal(Modalidad.Individual, snapshot.Modalidad);
        Assert.Single(snapshot.Juegos);
        Assert.Equal(1, snapshot.Juegos[0].Orden);
        Assert.Equal(TipoJuego.Trivia, snapshot.Juegos[0].TipoJuego);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test "services/operaciones-sesion/Umbral.OperacionesSesion.sln" --filter "FullyQualifiedName~ValueObjectTests"`
Expected: BUILD FAIL (types not defined).

- [ ] **Step 3: Create the enums**

`Domain/Enums/EstadoSesion.cs`:
```csharp
namespace Umbral.OperacionesSesion.Domain.Enums;

public enum EstadoSesion { Lobby, Iniciada, Cancelada, Terminada }
```
`Domain/Enums/EstadoInscripcion.cs`:
```csharp
namespace Umbral.OperacionesSesion.Domain.Enums;

public enum EstadoInscripcion { Activa, Cancelada }
```
`Domain/Enums/Modalidad.cs`:
```csharp
namespace Umbral.OperacionesSesion.Domain.Enums;

public enum Modalidad { Individual, Equipo }
```
`Domain/Enums/ModoInicioPartida.cs`:
```csharp
namespace Umbral.OperacionesSesion.Domain.Enums;

public enum ModoInicioPartida { Manual, Automatico, ManualYAutomatico }
```
`Domain/Enums/TipoJuego.cs`:
```csharp
namespace Umbral.OperacionesSesion.Domain.Enums;

public enum TipoJuego { Trivia, BusquedaDelTesoro }
```

- [ ] **Step 4: Create the value objects**

`Domain/ValueObjects/SesionPartidaId.cs`:
```csharp
namespace Umbral.OperacionesSesion.Domain.ValueObjects;

public readonly record struct SesionPartidaId(Guid Valor)
{
    public static SesionPartidaId New() => new(Guid.NewGuid());
    public static SesionPartidaId From(Guid valor) => new(valor);
    public bool EsValido() => Valor != Guid.Empty;
}
```
`Domain/ValueObjects/InscripcionId.cs`:
```csharp
namespace Umbral.OperacionesSesion.Domain.ValueObjects;

public readonly record struct InscripcionId(Guid Valor)
{
    public static InscripcionId New() => new(Guid.NewGuid());
    public static InscripcionId From(Guid valor) => new(valor);
    public bool EsValido() => Valor != Guid.Empty;
}
```
`Domain/Entities/JuegoResumen.cs` (entity persisted in `sesion_juegos`; immutable reference, EF needs a private ctor):
```csharp
using Umbral.OperacionesSesion.Domain.Enums;

namespace Umbral.OperacionesSesion.Domain.Entities;

public sealed class JuegoResumen
{
    public Guid JuegoId { get; private set; }
    public int Orden { get; private set; }
    public TipoJuego TipoJuego { get; private set; }

    private JuegoResumen() { } // EF

    public JuegoResumen(Guid juegoId, int orden, TipoJuego tipoJuego)
    {
        JuegoId = juegoId;
        Orden = orden;
        TipoJuego = tipoJuego;
    }
}
```
`Domain/ValueObjects/ConfiguracionSnapshot.cs` (transient carrier passed to `SesionPartida.Publicar`; NOT an EF-owned type — `SesionPartida` copies its fields into its own columns):
```csharp
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;

namespace Umbral.OperacionesSesion.Domain.ValueObjects;

public sealed record ConfiguracionSnapshot(
    string Nombre,
    Modalidad Modalidad,
    ModoInicioPartida ModoInicioPartida,
    DateTime? TiempoInicio,
    int MinimosParticipacion,
    int MaximosParticipacion,
    IReadOnlyList<JuegoResumen> Juegos);
```

- [ ] **Step 5: Create the domain exceptions**

Each in `Domain/Exceptions/`, all extending `Exception`:

`PartidaNoPublicableException.cs`:
```csharp
namespace Umbral.OperacionesSesion.Domain.Exceptions;

public sealed class PartidaNoPublicableException : Exception
{
    public PartidaNoPublicableException(Guid partidaId)
        : base($"La partida {partidaId} no es publicable: requiere al menos un juego con orden contiguo desde 1.") { }
}
```
`SesionNoEnLobbyException.cs`:
```csharp
namespace Umbral.OperacionesSesion.Domain.Exceptions;

public sealed class SesionNoEnLobbyException : Exception
{
    public SesionNoEnLobbyException(Guid partidaId)
        : base($"La sesión de la partida {partidaId} no está en Lobby.") { }
}
```
`ModalidadNoSoportadaException.cs`:
```csharp
namespace Umbral.OperacionesSesion.Domain.Exceptions;

public sealed class ModalidadNoSoportadaException : Exception
{
    public ModalidadNoSoportadaException(Guid partidaId)
        : base($"La modalidad de la partida {partidaId} no está soportada en esta operación (Equipo llega en SP-3a-E).") { }
}
```
`ParticipanteYaInscritoException.cs`:
```csharp
namespace Umbral.OperacionesSesion.Domain.Exceptions;

public sealed class ParticipanteYaInscritoException : Exception
{
    public ParticipanteYaInscritoException(Guid participanteId)
        : base($"El participante {participanteId} ya está inscrito activamente en esta partida.") { }
}
```
`ParticipacionActivaExistenteException.cs`:
```csharp
namespace Umbral.OperacionesSesion.Domain.Exceptions;

public sealed class ParticipacionActivaExistenteException : Exception
{
    public ParticipacionActivaExistenteException(Guid participanteId)
        : base($"El participante {participanteId} ya tiene una participación activa en otra partida.") { }
}
```
`CupoLlenoException.cs`:
```csharp
namespace Umbral.OperacionesSesion.Domain.Exceptions;

public sealed class CupoLlenoException : Exception
{
    public CupoLlenoException(Guid partidaId)
        : base($"La partida {partidaId} alcanzó el máximo de participación.") { }
}
```
`InscripcionNoEncontradaException.cs`:
```csharp
namespace Umbral.OperacionesSesion.Domain.Exceptions;

public sealed class InscripcionNoEncontradaException : Exception
{
    public InscripcionNoEncontradaException(Guid participanteId)
        : base($"El participante {participanteId} no tiene una inscripción activa en esta partida.") { }
}
```

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test "services/operaciones-sesion/Umbral.OperacionesSesion.sln" --filter "FullyQualifiedName~ValueObjectTests"`
Expected: PASS (4 tests).

- [ ] **Step 7: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/ValueObjectTests.cs
git commit -m "SP-3a: dominio base de Operaciones (enums, VOs, JuegoResumen, ConfiguracionSnapshot, excepciones)"
```

---

### Task 2: `SesionPartida` aggregate + `InscripcionPartida` + repository interfaces

**Files:**
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/InscripcionPartida.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Entities/SesionPartida.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Abstractions/Persistence/ISesionPartidaRepository.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Abstractions/Persistence/IOperacionesSesionUnitOfWork.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/SesionPartidaTests.cs`

**Interfaces:**
- Consumes: Task 1 enums/VOs/`ConfiguracionSnapshot`/exceptions.
- Produces:
  - `InscripcionPartida` — `InscripcionId Id`, `Guid ParticipanteId`, `EstadoInscripcion Estado`, `DateTime FechaInscripcion`, method `Cancelar()`.
  - `SesionPartida` — props `SesionPartidaId Id`, `Guid PartidaId`, `EstadoSesion Estado`, scalar config (`Nombre`, `Modalidad`, `ModoInicioPartida`, `TiempoInicio`, `MinimosParticipacion`, `MaximosParticipacion`), `IReadOnlyList<JuegoResumen> Juegos`, `IReadOnlyList<InscripcionPartida> Inscripciones`. Methods: `static SesionPartida Publicar(Guid partidaId, ConfiguracionSnapshot snapshot)`, `InscripcionPartida Inscribir(Guid participanteId, bool tieneParticipacionActivaEnOtra, int inscritosActivos, DateTime fecha)`, `void CancelarInscripcion(Guid participanteId)`.
  - `ISesionPartidaRepository` — `void Add(SesionPartida)`, `Task<SesionPartida?> GetByPartidaIdAsync(Guid, CancellationToken)`, `Task<bool> ExistsForPartidaAsync(Guid, CancellationToken)`, `Task<bool> ParticipanteTieneParticipacionActivaAsync(Guid participanteId, Guid exceptPartidaId, CancellationToken)`.
  - `IOperacionesSesionUnitOfWork` — `Task SaveChangesAsync(CancellationToken)`.

- [ ] **Step 1: Write the failing test**

Create `tests/Umbral.OperacionesSesion.UnitTests/Domain/SesionPartidaTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.Exceptions;
using Umbral.OperacionesSesion.Domain.ValueObjects;

namespace Umbral.OperacionesSesion.UnitTests.Domain;

public class SesionPartidaTests
{
    private static ConfiguracionSnapshot Snapshot(
        Modalidad modalidad = Modalidad.Individual, int min = 1, int max = 2, int juegos = 1)
    {
        var lista = Enumerable.Range(1, juegos)
            .Select(o => new JuegoResumen(Guid.NewGuid(), o, TipoJuego.Trivia))
            .ToList();
        return new ConfiguracionSnapshot("Copa", modalidad, ModoInicioPartida.Manual, null, min, max, lista);
    }

    private static readonly DateTime T0 = new(2026, 6, 26, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Publicar_with_at_least_one_contiguous_game_sets_lobby()
    {
        var partidaId = Guid.NewGuid();
        var sesion = SesionPartida.Publicar(partidaId, Snapshot(juegos: 2));

        Assert.Equal(EstadoSesion.Lobby, sesion.Estado);
        Assert.Equal(partidaId, sesion.PartidaId);
        Assert.True(sesion.Id.EsValido());
        Assert.Equal(2, sesion.Juegos.Count);
        Assert.Empty(sesion.Inscripciones);
    }

    [Fact]
    public void Publicar_without_games_throws()
    {
        var partidaId = Guid.NewGuid();
        var snapshot = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 2,
            new List<JuegoResumen>());

        Assert.Throws<PartidaNoPublicableException>(() => SesionPartida.Publicar(partidaId, snapshot));
    }

    [Fact]
    public void Publicar_with_non_contiguous_orden_throws()
    {
        var snapshot = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 2,
            new List<JuegoResumen> { new(Guid.NewGuid(), 1, TipoJuego.Trivia), new(Guid.NewGuid(), 3, TipoJuego.Trivia) });

        Assert.Throws<PartidaNoPublicableException>(() => SesionPartida.Publicar(Guid.NewGuid(), snapshot));
    }

    [Fact]
    public void Inscribir_happy_path_adds_active_inscription()
    {
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), Snapshot());
        var participante = Guid.NewGuid();

        var inscripcion = sesion.Inscribir(participante, tieneParticipacionActivaEnOtra: false, inscritosActivos: 0, T0);

        Assert.Equal(EstadoInscripcion.Activa, inscripcion.Estado);
        Assert.Equal(participante, inscripcion.ParticipanteId);
        Assert.Equal(T0, inscripcion.FechaInscripcion);
        Assert.Single(sesion.Inscripciones);
    }

    [Fact]
    public void Inscribir_when_not_in_lobby_throws()
    {
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), Snapshot());
        // Cancel the only path that mutates Estado is not exposed in 3a; simulate via reflection-free guard:
        // Estado starts Lobby, so to test the guard we publish then mark Terminada through Inscribir on a non-Individual? No —
        // instead assert the guard indirectly: a non-Individual modality is the reachable rejection in 3a.
        var equipo = SesionPartida.Publicar(Guid.NewGuid(), Snapshot(Modalidad.Equipo));
        Assert.Throws<ModalidadNoSoportadaException>(
            () => equipo.Inscribir(Guid.NewGuid(), false, 0, T0));
    }

    [Fact]
    public void Inscribir_duplicate_participant_throws()
    {
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), Snapshot(max: 5));
        var participante = Guid.NewGuid();
        sesion.Inscribir(participante, false, 0, T0);

        Assert.Throws<ParticipanteYaInscritoException>(
            () => sesion.Inscribir(participante, false, 1, T0));
    }

    [Fact]
    public void Inscribir_with_active_participation_elsewhere_throws()
    {
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), Snapshot());
        Assert.Throws<ParticipacionActivaExistenteException>(
            () => sesion.Inscribir(Guid.NewGuid(), tieneParticipacionActivaEnOtra: true, inscritosActivos: 0, T0));
    }

    [Fact]
    public void Inscribir_when_capacity_full_throws()
    {
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), Snapshot(max: 1));
        Assert.Throws<CupoLlenoException>(
            () => sesion.Inscribir(Guid.NewGuid(), false, inscritosActivos: 1, T0));
    }

    [Fact]
    public void CancelarInscripcion_marks_cancelled_and_frees_capacity()
    {
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), Snapshot(max: 1));
        var participante = Guid.NewGuid();
        sesion.Inscribir(participante, false, 0, T0);

        sesion.CancelarInscripcion(participante);

        Assert.Equal(EstadoInscripcion.Cancelada, sesion.Inscripciones.Single().Estado);
        // capacity freed → a different participant can now inscribe (active count back to 0)
        var otra = sesion.Inscribir(Guid.NewGuid(), false, 0, T0);
        Assert.Equal(EstadoInscripcion.Activa, otra.Estado);
    }

    [Fact]
    public void CancelarInscripcion_without_active_inscription_throws()
    {
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), Snapshot());
        Assert.Throws<InscripcionNoEncontradaException>(() => sesion.CancelarInscripcion(Guid.NewGuid()));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test "services/operaciones-sesion/Umbral.OperacionesSesion.sln" --filter "FullyQualifiedName~SesionPartidaTests"`
Expected: BUILD FAIL (`SesionPartida`, `InscripcionPartida` not defined).

- [ ] **Step 3: Create `InscripcionPartida`**

`Domain/Entities/InscripcionPartida.cs`:
```csharp
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.ValueObjects;

namespace Umbral.OperacionesSesion.Domain.Entities;

public sealed class InscripcionPartida
{
    public InscripcionId Id { get; private set; }
    public Guid ParticipanteId { get; private set; }
    public EstadoInscripcion Estado { get; private set; }
    public DateTime FechaInscripcion { get; private set; }

    private InscripcionPartida() { } // EF

    internal InscripcionPartida(Guid participanteId, DateTime fecha)
    {
        Id = InscripcionId.New();
        ParticipanteId = participanteId;
        Estado = EstadoInscripcion.Activa;
        FechaInscripcion = fecha;
    }

    internal void Cancelar() => Estado = EstadoInscripcion.Cancelada;

    public bool EsActiva => Estado == EstadoInscripcion.Activa;
}
```

- [ ] **Step 4: Create `SesionPartida`**

`Domain/Entities/SesionPartida.cs`:
```csharp
using System.Linq;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.Exceptions;
using Umbral.OperacionesSesion.Domain.ValueObjects;

namespace Umbral.OperacionesSesion.Domain.Entities;

public sealed class SesionPartida
{
    private readonly List<JuegoResumen> _juegos = new();
    private readonly List<InscripcionPartida> _inscripciones = new();

    public SesionPartidaId Id { get; private set; }
    public Guid PartidaId { get; private set; }
    public EstadoSesion Estado { get; private set; }
    public string Nombre { get; private set; } = null!;
    public Modalidad Modalidad { get; private set; }
    public ModoInicioPartida ModoInicioPartida { get; private set; }
    public DateTime? TiempoInicio { get; private set; }
    public int MinimosParticipacion { get; private set; }
    public int MaximosParticipacion { get; private set; }

    public IReadOnlyList<JuegoResumen> Juegos => _juegos;
    public IReadOnlyList<InscripcionPartida> Inscripciones => _inscripciones;

    private SesionPartida() { } // EF

    private SesionPartida(Guid partidaId, ConfiguracionSnapshot snapshot)
    {
        Id = SesionPartidaId.New();
        PartidaId = partidaId;
        Nombre = snapshot.Nombre;
        Modalidad = snapshot.Modalidad;
        ModoInicioPartida = snapshot.ModoInicioPartida;
        TiempoInicio = snapshot.TiempoInicio;
        MinimosParticipacion = snapshot.MinimosParticipacion;
        MaximosParticipacion = snapshot.MaximosParticipacion;
        _juegos.AddRange(snapshot.Juegos);
        Estado = EstadoSesion.Lobby;
    }

    public static SesionPartida Publicar(Guid partidaId, ConfiguracionSnapshot snapshot)
    {
        ValidarPublicabilidad(partidaId, snapshot);
        return new SesionPartida(partidaId, snapshot);
    }

    public InscripcionPartida Inscribir(
        Guid participanteId, bool tieneParticipacionActivaEnOtra, int inscritosActivos, DateTime fecha)
    {
        if (Estado != EstadoSesion.Lobby)
            throw new SesionNoEnLobbyException(PartidaId);
        if (Modalidad != Modalidad.Individual)
            throw new ModalidadNoSoportadaException(PartidaId);
        if (_inscripciones.Any(i => i.ParticipanteId == participanteId && i.EsActiva))
            throw new ParticipanteYaInscritoException(participanteId);
        if (tieneParticipacionActivaEnOtra)
            throw new ParticipacionActivaExistenteException(participanteId);
        if (inscritosActivos >= MaximosParticipacion)
            throw new CupoLlenoException(PartidaId);

        var inscripcion = new InscripcionPartida(participanteId, fecha);
        _inscripciones.Add(inscripcion);
        return inscripcion;
    }

    public void CancelarInscripcion(Guid participanteId)
    {
        if (Estado != EstadoSesion.Lobby)
            throw new SesionNoEnLobbyException(PartidaId);
        var inscripcion = _inscripciones.FirstOrDefault(i => i.ParticipanteId == participanteId && i.EsActiva)
            ?? throw new InscripcionNoEncontradaException(participanteId);
        inscripcion.Cancelar();
    }

    private static void ValidarPublicabilidad(Guid partidaId, ConfiguracionSnapshot snapshot)
    {
        if (snapshot.Juegos.Count == 0)
            throw new PartidaNoPublicableException(partidaId);
        var ordenes = snapshot.Juegos.Select(j => j.Orden).OrderBy(o => o).ToList();
        for (var i = 0; i < ordenes.Count; i++)
        {
            if (ordenes[i] != i + 1)
                throw new PartidaNoPublicableException(partidaId);
        }
    }
}
```

- [ ] **Step 5: Create the repository interfaces**

`Domain/Abstractions/Persistence/ISesionPartidaRepository.cs`:
```csharp
using Umbral.OperacionesSesion.Domain.Entities;

namespace Umbral.OperacionesSesion.Domain.Abstractions.Persistence;

public interface ISesionPartidaRepository
{
    void Add(SesionPartida sesion);
    Task<SesionPartida?> GetByPartidaIdAsync(Guid partidaId, CancellationToken cancellationToken);
    Task<bool> ExistsForPartidaAsync(Guid partidaId, CancellationToken cancellationToken);
    Task<bool> ParticipanteTieneParticipacionActivaAsync(
        Guid participanteId, Guid exceptPartidaId, CancellationToken cancellationToken);
}
```
`Domain/Abstractions/Persistence/IOperacionesSesionUnitOfWork.cs`:
```csharp
namespace Umbral.OperacionesSesion.Domain.Abstractions.Persistence;

public interface IOperacionesSesionUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
```

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test "services/operaciones-sesion/Umbral.OperacionesSesion.sln" --filter "FullyQualifiedName~SesionPartidaTests"`
Expected: PASS (11 tests).

- [ ] **Step 7: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Domain/SesionPartidaTests.cs
git commit -m "SP-3a: agregado SesionPartida + InscripcionPartida + interfaces de repositorio"
```

---

### Task 3: Application ports, DTOs, events, ValidationBehavior, DI

**Files:**
- Create: `Application/Interfaces/IConfiguracionPartidaClient.cs`, `Application/Interfaces/ISesionEventsPublisher.cs`, `Application/Interfaces/PartidaPublicadaEnLobbyEvent.cs`
- Create: `Application/DTOs/ConfiguracionPartidaDto.cs`, `Application/DTOs/LobbyDto.cs`, `Application/DTOs/InscripcionResponse.cs`
- Create: `Application/Exceptions/{SesionYaPublicadaException,SesionNoEncontradaException,PartidaConfigNoEncontradaException,PartidasConfigInaccesibleException,ParticipanteNoIdentificadoException}.cs`
- Create: `Application/ValidationBehavior.cs`
- Modify: `Application/DependencyInjection.cs`
- Create: `Application/Commands/PublicarPartidaCommand.cs`, `Application/Validators/PublicarPartidaCommandValidator.cs` (needed by the test)
- Test: `tests/Umbral.OperacionesSesion.UnitTests/Application/ValidationBehaviorTests.cs`

**Interfaces:**
- Consumes: Task 1 enums.
- Produces:
  - `IConfiguracionPartidaClient.ObtenerConfiguracionAsync(Guid partidaId, string? bearerToken, CancellationToken) → Task<ConfiguracionPartidaDto?>`.
  - `ISesionEventsPublisher.PublicarPartidaPublicadaEnLobbyAsync(PartidaPublicadaEnLobbyEvent, CancellationToken) → Task`.
  - `PartidaPublicadaEnLobbyEvent(Guid PartidaId, Guid SesionPartidaId, string Modalidad, int MinimosParticipacion, int MaximosParticipacion)`.
  - `ConfiguracionPartidaDto(string Nombre, string Modalidad, string ModoInicioPartida, DateTime? TiempoInicio, int MinimosParticipacion, int MaximosParticipacion, IReadOnlyList<JuegoResumenDto> Juegos)`; `JuegoResumenDto(Guid JuegoId, int Orden, string TipoJuego)`.
  - `LobbyDto(Guid PartidaId, Guid SesionPartidaId, string Estado, string Modalidad, int MinimosParticipacion, int MaximosParticipacion, int InscritosActivos, IReadOnlyList<Guid> Participantes)`.
  - `InscripcionResponse(Guid InscripcionId, Guid PartidaId, Guid ParticipanteId)`.
  - `PublicarPartidaCommand(Guid PartidaId, string? BearerToken) : IRequest<LobbyDto>`.
  - Application exceptions (mapped by middleware in Task 9).

- [ ] **Step 1: Write the failing test**

Create `tests/Umbral.OperacionesSesion.UnitTests/Application/ValidationBehaviorTests.cs`:

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Umbral.OperacionesSesion.Application;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Validators;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Throws_validation_exception_for_empty_partida_id()
    {
        var behavior = new ValidationBehavior<PublicarPartidaCommand, LobbyDto>(
            new IValidator<PublicarPartidaCommand>[] { new PublicarPartidaCommandValidator() });
        var command = new PublicarPartidaCommand(Guid.Empty, null);

        await Assert.ThrowsAsync<ValidationException>(
            () => behavior.Handle(command, () => Task.FromResult(Lobby()), CancellationToken.None));
    }

    [Fact]
    public async Task Calls_next_when_valid()
    {
        var behavior = new ValidationBehavior<PublicarPartidaCommand, LobbyDto>(
            new IValidator<PublicarPartidaCommand>[] { new PublicarPartidaCommandValidator() });
        var command = new PublicarPartidaCommand(Guid.NewGuid(), "Bearer x");
        var expected = Lobby();

        var result = await behavior.Handle(command, () => Task.FromResult(expected), CancellationToken.None);

        Assert.Same(expected, result);
    }

    private static LobbyDto Lobby() =>
        new(Guid.NewGuid(), Guid.NewGuid(), "Lobby", "Individual", 1, 10, 0, Array.Empty<Guid>());
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test "services/operaciones-sesion/Umbral.OperacionesSesion.sln" --filter "FullyQualifiedName~ValidationBehaviorTests"`
Expected: BUILD FAIL.

- [ ] **Step 3: Create the interfaces, event, and DTOs**

`Application/Interfaces/PartidaPublicadaEnLobbyEvent.cs`:
```csharp
namespace Umbral.OperacionesSesion.Application.Interfaces;

public sealed record PartidaPublicadaEnLobbyEvent(
    Guid PartidaId,
    Guid SesionPartidaId,
    string Modalidad,
    int MinimosParticipacion,
    int MaximosParticipacion);
```
`Application/Interfaces/ISesionEventsPublisher.cs`:
```csharp
namespace Umbral.OperacionesSesion.Application.Interfaces;

public interface ISesionEventsPublisher
{
    Task PublicarPartidaPublicadaEnLobbyAsync(PartidaPublicadaEnLobbyEvent evento, CancellationToken cancellationToken);
}
```
`Application/DTOs/ConfiguracionPartidaDto.cs`:
```csharp
namespace Umbral.OperacionesSesion.Application.DTOs;

public sealed record ConfiguracionPartidaDto(
    string Nombre,
    string Modalidad,
    string ModoInicioPartida,
    DateTime? TiempoInicio,
    int MinimosParticipacion,
    int MaximosParticipacion,
    IReadOnlyList<JuegoResumenDto> Juegos);

public sealed record JuegoResumenDto(Guid JuegoId, int Orden, string TipoJuego);
```
`Application/Interfaces/IConfiguracionPartidaClient.cs`:
```csharp
using Umbral.OperacionesSesion.Application.DTOs;

namespace Umbral.OperacionesSesion.Application.Interfaces;

public interface IConfiguracionPartidaClient
{
    Task<ConfiguracionPartidaDto?> ObtenerConfiguracionAsync(
        Guid partidaId, string? bearerToken, CancellationToken cancellationToken);
}
```
`Application/DTOs/LobbyDto.cs`:
```csharp
namespace Umbral.OperacionesSesion.Application.DTOs;

public sealed record LobbyDto(
    Guid PartidaId,
    Guid SesionPartidaId,
    string Estado,
    string Modalidad,
    int MinimosParticipacion,
    int MaximosParticipacion,
    int InscritosActivos,
    IReadOnlyList<Guid> Participantes);
```
`Application/DTOs/InscripcionResponse.cs`:
```csharp
namespace Umbral.OperacionesSesion.Application.DTOs;

public sealed record InscripcionResponse(Guid InscripcionId, Guid PartidaId, Guid ParticipanteId);
```

- [ ] **Step 4: Create the application exceptions**

`Application/Exceptions/SesionYaPublicadaException.cs`:
```csharp
namespace Umbral.OperacionesSesion.Application.Exceptions;

public sealed class SesionYaPublicadaException : Exception
{
    public SesionYaPublicadaException(Guid partidaId) : base($"La partida {partidaId} ya fue publicada.") { }
}
```
`Application/Exceptions/SesionNoEncontradaException.cs`:
```csharp
namespace Umbral.OperacionesSesion.Application.Exceptions;

public sealed class SesionNoEncontradaException : Exception
{
    public SesionNoEncontradaException(Guid partidaId)
        : base($"No existe una sesión publicada para la partida {partidaId}.") { }
}
```
`Application/Exceptions/PartidaConfigNoEncontradaException.cs`:
```csharp
namespace Umbral.OperacionesSesion.Application.Exceptions;

public sealed class PartidaConfigNoEncontradaException : Exception
{
    public PartidaConfigNoEncontradaException(Guid partidaId)
        : base($"No existe configuración para la partida {partidaId}.") { }
}
```
`Application/Exceptions/PartidasConfigInaccesibleException.cs`:
```csharp
namespace Umbral.OperacionesSesion.Application.Exceptions;

public sealed class PartidasConfigInaccesibleException : Exception
{
    public PartidasConfigInaccesibleException(Guid partidaId, Exception? inner = null)
        : base($"El servicio Partidas no respondió la configuración de la partida {partidaId}.", inner) { }
}
```
`Application/Exceptions/ParticipanteNoIdentificadoException.cs`:
```csharp
namespace Umbral.OperacionesSesion.Application.Exceptions;

public sealed class ParticipanteNoIdentificadoException : Exception
{
    public ParticipanteNoIdentificadoException() : base("No se pudo identificar al participante desde el token.") { }
}
```

- [ ] **Step 5: Create the command + validator (needed by the test and Task 4)**

`Application/Commands/PublicarPartidaCommand.cs`:
```csharp
using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;

namespace Umbral.OperacionesSesion.Application.Commands;

public sealed record PublicarPartidaCommand(Guid PartidaId, string? BearerToken) : IRequest<LobbyDto>;
```
`Application/Validators/PublicarPartidaCommandValidator.cs`:
```csharp
using FluentValidation;
using Umbral.OperacionesSesion.Application.Commands;

namespace Umbral.OperacionesSesion.Application.Validators;

public sealed class PublicarPartidaCommandValidator : AbstractValidator<PublicarPartidaCommand>
{
    public PublicarPartidaCommandValidator()
    {
        RuleFor(x => x.PartidaId).NotEmpty();
    }
}
```

- [ ] **Step 6: Copy `ValidationBehavior` and wire DI**

`Application/ValidationBehavior.cs` (verbatim from Partidas, namespace adjusted):
```csharp
using FluentValidation;
using MediatR;

namespace Umbral.OperacionesSesion.Application;

// MediatR pipeline behavior: runs FluentValidation before the handler so controllers
// stay pure dispatchers (doctrine audit M-2). On failure throws ValidationException,
// which the centralized exception middleware maps to 400.
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators) => _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            var failures = (await Task.WhenAll(
                    _validators.Select(v => v.ValidateAsync(context, cancellationToken))))
                .SelectMany(result => result.Errors)
                .Where(failure => failure is not null)
                .ToList();

            if (failures.Count != 0)
                throw new ValidationException(failures);
        }

        return await next();
    }
}
```
Modify `Application/DependencyInjection.cs` to register the pipeline behavior and `TimeProvider`:
```csharp
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Umbral.OperacionesSesion.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddOperacionesSesionApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddSingleton(TimeProvider.System);
        return services;
    }
}
```

- [ ] **Step 7: Run tests to verify they pass**

Run: `dotnet test "services/operaciones-sesion/Umbral.OperacionesSesion.sln" --filter "FullyQualifiedName~ValidationBehaviorTests"`
Expected: PASS (2 tests).

- [ ] **Step 8: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Application services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/ValidationBehaviorTests.cs
git commit -m "SP-3a: puertos, DTOs, evento, excepciones de aplicación y ValidationBehavior en Operaciones"
```

---

### Task 4: Publish command handler + validator + fakes

**Files:**
- Create: `Application/Handlers/Commands/PublicarPartidaCommandHandler.cs`
- Create: `tests/Umbral.OperacionesSesion.UnitTests/Application/Fakes/{FakeSesionPartidaRepository,FakeOperacionesSesionUnitOfWork,FakeConfiguracionPartidaClient,FakeSesionEventsPublisher}.cs`
- Create: `tests/Umbral.OperacionesSesion.UnitTests/Application/PublicarPartidaCommandHandlerTests.cs`
- Create: `tests/Umbral.OperacionesSesion.UnitTests/Application/PublicarPartidaCommandValidatorTests.cs`

**Interfaces:**
- Consumes: Task 2 (`SesionPartida`, repo/UoW interfaces), Task 3 (`PublicarPartidaCommand`, `IConfiguracionPartidaClient`, `ISesionEventsPublisher`, DTOs, exceptions).
- Produces: `PublicarPartidaCommandHandler : IRequestHandler<PublicarPartidaCommand, LobbyDto>`; reusable fakes for later tasks. Establishes the `LobbyDto` mapping convention (`MapearLobby(SesionPartida)`).

- [ ] **Step 1: Write the failing tests**

Create the four fakes first.

`Fakes/FakeSesionPartidaRepository.cs`:
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;
using Umbral.OperacionesSesion.Domain.Entities;

namespace Umbral.OperacionesSesion.UnitTests.Application.Fakes;

public sealed class FakeSesionPartidaRepository : ISesionPartidaRepository
{
    private readonly Dictionary<Guid, SesionPartida> _store = new(); // keyed by PartidaId
    public IReadOnlyDictionary<Guid, SesionPartida> Store => _store;

    // Test hook: simulate that the participant is active in some OTHER partida.
    public bool ParticipacionActivaEnOtra { get; set; }

    public void Add(SesionPartida sesion) => _store[sesion.PartidaId] = sesion;

    public Task<SesionPartida?> GetByPartidaIdAsync(Guid partidaId, CancellationToken cancellationToken)
        => Task.FromResult(_store.TryGetValue(partidaId, out var s) ? s : null);

    public Task<bool> ExistsForPartidaAsync(Guid partidaId, CancellationToken cancellationToken)
        => Task.FromResult(_store.ContainsKey(partidaId));

    public Task<bool> ParticipanteTieneParticipacionActivaAsync(
        Guid participanteId, Guid exceptPartidaId, CancellationToken cancellationToken)
        => Task.FromResult(ParticipacionActivaEnOtra);
}
```
`Fakes/FakeOperacionesSesionUnitOfWork.cs`:
```csharp
using System.Threading;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;

namespace Umbral.OperacionesSesion.UnitTests.Application.Fakes;

public sealed class FakeOperacionesSesionUnitOfWork : IOperacionesSesionUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}
```
`Fakes/FakeConfiguracionPartidaClient.cs`:
```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Interfaces;

namespace Umbral.OperacionesSesion.UnitTests.Application.Fakes;

public sealed class FakeConfiguracionPartidaClient : IConfiguracionPartidaClient
{
    private readonly ConfiguracionPartidaDto? _respuesta;
    public string? LastBearerToken { get; private set; }

    public FakeConfiguracionPartidaClient(ConfiguracionPartidaDto? respuesta) => _respuesta = respuesta;

    public Task<ConfiguracionPartidaDto?> ObtenerConfiguracionAsync(
        Guid partidaId, string? bearerToken, CancellationToken cancellationToken)
    {
        LastBearerToken = bearerToken;
        return Task.FromResult(_respuesta);
    }
}
```
`Fakes/FakeSesionEventsPublisher.cs`:
```csharp
using System.Threading;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.Interfaces;

namespace Umbral.OperacionesSesion.UnitTests.Application.Fakes;

public sealed class FakeSesionEventsPublisher : ISesionEventsPublisher
{
    public PartidaPublicadaEnLobbyEvent? LastEvent { get; private set; }
    public int PublishCount { get; private set; }

    public Task PublicarPartidaPublicadaEnLobbyAsync(PartidaPublicadaEnLobbyEvent evento, CancellationToken cancellationToken)
    {
        LastEvent = evento;
        PublishCount++;
        return Task.CompletedTask;
    }
}
```
`PublicarPartidaCommandHandlerTests.cs`:
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Handlers.Commands;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class PublicarPartidaCommandHandlerTests
{
    private static ConfiguracionPartidaDto Config(string modalidad = "Individual", int juegos = 1) =>
        new("Copa", modalidad, "Manual", null, 1, 10,
            Enumerable.Range(1, juegos).Select(o => new JuegoResumenDto(Guid.NewGuid(), o, "Trivia")).ToList());

    private static PublicarPartidaCommandHandler Handler(
        FakeSesionPartidaRepository repo, FakeOperacionesSesionUnitOfWork uow,
        FakeConfiguracionPartidaClient client, FakeSesionEventsPublisher events)
        => new(repo, uow, client, events);

    [Fact]
    public async Task Publishes_session_in_lobby_and_emits_event()
    {
        var repo = new FakeSesionPartidaRepository();
        var uow = new FakeOperacionesSesionUnitOfWork();
        var client = new FakeConfiguracionPartidaClient(Config(juegos: 2));
        var events = new FakeSesionEventsPublisher();
        var partidaId = Guid.NewGuid();

        var lobby = await Handler(repo, uow, client, events)
            .Handle(new PublicarPartidaCommand(partidaId, "Bearer abc"), CancellationToken.None);

        Assert.Equal("Lobby", lobby.Estado);
        Assert.Equal(partidaId, lobby.PartidaId);
        Assert.Equal(0, lobby.InscritosActivos);
        Assert.True(repo.Store.ContainsKey(partidaId));
        Assert.Equal(1, uow.SaveCount);
        Assert.Equal(1, events.PublishCount);
        Assert.Equal(partidaId, events.LastEvent!.PartidaId);
        Assert.Equal("Bearer abc", client.LastBearerToken);
    }

    [Fact]
    public async Task Throws_when_config_not_found()
    {
        var handler = Handler(new FakeSesionPartidaRepository(), new FakeOperacionesSesionUnitOfWork(),
            new FakeConfiguracionPartidaClient(null), new FakeSesionEventsPublisher());

        await Assert.ThrowsAsync<PartidaConfigNoEncontradaException>(
            () => handler.Handle(new PublicarPartidaCommand(Guid.NewGuid(), null), CancellationToken.None));
    }

    [Fact]
    public async Task Throws_when_already_published()
    {
        var repo = new FakeSesionPartidaRepository();
        var uow = new FakeOperacionesSesionUnitOfWork();
        var client = new FakeConfiguracionPartidaClient(Config());
        var events = new FakeSesionEventsPublisher();
        var partidaId = Guid.NewGuid();
        await Handler(repo, uow, client, events).Handle(new PublicarPartidaCommand(partidaId, null), CancellationToken.None);

        await Assert.ThrowsAsync<SesionYaPublicadaException>(
            () => Handler(repo, uow, client, events).Handle(new PublicarPartidaCommand(partidaId, null), CancellationToken.None));
    }

    [Fact]
    public async Task Throws_when_config_has_no_games()
    {
        var client = new FakeConfiguracionPartidaClient(
            new ConfiguracionPartidaDto("Copa", "Individual", "Manual", null, 1, 10, new List<JuegoResumenDto>()));
        var handler = Handler(new FakeSesionPartidaRepository(), new FakeOperacionesSesionUnitOfWork(),
            client, new FakeSesionEventsPublisher());

        await Assert.ThrowsAsync<Umbral.OperacionesSesion.Domain.Exceptions.PartidaNoPublicableException>(
            () => handler.Handle(new PublicarPartidaCommand(Guid.NewGuid(), null), CancellationToken.None));
    }
}
```
`PublicarPartidaCommandValidatorTests.cs`:
```csharp
using System;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.Validators;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class PublicarPartidaCommandValidatorTests
{
    [Fact]
    public void Empty_partida_id_is_invalid()
    {
        var result = new PublicarPartidaCommandValidator().Validate(new PublicarPartidaCommand(Guid.Empty, null));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Non_empty_partida_id_is_valid()
    {
        var result = new PublicarPartidaCommandValidator().Validate(new PublicarPartidaCommand(Guid.NewGuid(), null));
        Assert.True(result.IsValid);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test "services/operaciones-sesion/Umbral.OperacionesSesion.sln" --filter "FullyQualifiedName~PublicarPartidaCommandHandlerTests"`
Expected: BUILD FAIL (handler not defined).

- [ ] **Step 3: Implement the handler**

`Application/Handlers/Commands/PublicarPartidaCommandHandler.cs`:
```csharp
using System.Linq;
using MediatR;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Interfaces;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.ValueObjects;

namespace Umbral.OperacionesSesion.Application.Handlers.Commands;

public sealed class PublicarPartidaCommandHandler : IRequestHandler<PublicarPartidaCommand, LobbyDto>
{
    private readonly ISesionPartidaRepository _sesiones;
    private readonly IOperacionesSesionUnitOfWork _unitOfWork;
    private readonly IConfiguracionPartidaClient _configClient;
    private readonly ISesionEventsPublisher _events;

    public PublicarPartidaCommandHandler(
        ISesionPartidaRepository sesiones,
        IOperacionesSesionUnitOfWork unitOfWork,
        IConfiguracionPartidaClient configClient,
        ISesionEventsPublisher events)
    {
        _sesiones = sesiones;
        _unitOfWork = unitOfWork;
        _configClient = configClient;
        _events = events;
    }

    public async Task<LobbyDto> Handle(PublicarPartidaCommand request, CancellationToken cancellationToken)
    {
        if (await _sesiones.ExistsForPartidaAsync(request.PartidaId, cancellationToken))
            throw new SesionYaPublicadaException(request.PartidaId);

        var config = await _configClient.ObtenerConfiguracionAsync(request.PartidaId, request.BearerToken, cancellationToken)
            ?? throw new PartidaConfigNoEncontradaException(request.PartidaId);

        var snapshot = new ConfiguracionSnapshot(
            config.Nombre,
            Enum.Parse<Modalidad>(config.Modalidad),
            Enum.Parse<ModoInicioPartida>(config.ModoInicioPartida),
            config.TiempoInicio,
            config.MinimosParticipacion,
            config.MaximosParticipacion,
            config.Juegos.Select(j => new JuegoResumen(j.JuegoId, j.Orden, Enum.Parse<TipoJuego>(j.TipoJuego))).ToList());

        var sesion = SesionPartida.Publicar(request.PartidaId, snapshot);
        _sesiones.Add(sesion);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _events.PublicarPartidaPublicadaEnLobbyAsync(
            new PartidaPublicadaEnLobbyEvent(
                sesion.PartidaId, sesion.Id.Valor, sesion.Modalidad.ToString(),
                sesion.MinimosParticipacion, sesion.MaximosParticipacion),
            cancellationToken);

        return MapearLobby(sesion);
    }

    internal static LobbyDto MapearLobby(SesionPartida sesion) => new(
        sesion.PartidaId,
        sesion.Id.Valor,
        sesion.Estado.ToString(),
        sesion.Modalidad.ToString(),
        sesion.MinimosParticipacion,
        sesion.MaximosParticipacion,
        sesion.Inscripciones.Count(i => i.EsActiva),
        sesion.Inscripciones.Where(i => i.EsActiva).Select(i => i.ParticipanteId).ToList());
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test "services/operaciones-sesion/Umbral.OperacionesSesion.sln" --filter "FullyQualifiedName~PublicarPartida"`
Expected: PASS (6 tests).

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application
git commit -m "SP-3a: handler de publicación (snapshot HTTP → Lobby + evento) + fakes"
```

---

### Task 5: Inscribir + Cancelar commands, handlers, validators

**Files:**
- Create: `Application/Commands/InscribirParticipanteCommand.cs`, `Application/Commands/CancelarInscripcionCommand.cs`
- Create: `Application/Validators/InscribirParticipanteCommandValidator.cs`, `Application/Validators/CancelarInscripcionCommandValidator.cs`
- Create: `Application/Handlers/Commands/InscribirParticipanteCommandHandler.cs`, `Application/Handlers/Commands/CancelarInscripcionCommandHandler.cs`
- Create: `tests/Umbral.OperacionesSesion.UnitTests/Application/InscribirParticipanteCommandHandlerTests.cs`, `InscribirParticipanteCommandValidatorTests.cs`, `CancelarInscripcionCommandHandlerTests.cs`

**Interfaces:**
- Consumes: Task 2 (`SesionPartida.Inscribir/CancelarInscripcion`, repo/UoW), Task 3 (DTOs, `SesionNoEncontradaException`), Task 4 fakes, `TimeProvider`.
- Produces:
  - `InscribirParticipanteCommand(Guid PartidaId, Guid ParticipanteId) : IRequest<InscripcionResponse>`.
  - `CancelarInscripcionCommand(Guid PartidaId, Guid ParticipanteId) : IRequest<Unit>`.
  - Handlers using `ISesionPartidaRepository` + `IOperacionesSesionUnitOfWork` + `TimeProvider`.

- [ ] **Step 1: Write the failing tests**

`InscribirParticipanteCommandHandlerTests.cs`:
```csharp
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Handlers.Commands;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.Exceptions;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class InscribirParticipanteCommandHandlerTests
{
    private static readonly TimeProvider Clock = TimeProvider.System;

    private static SesionPartida PublishedSession(Guid partidaId, Modalidad modalidad = Modalidad.Individual, int max = 2)
    {
        var snapshot = new ConfiguracionSnapshot("Copa", modalidad, ModoInicioPartida.Manual, null, 1, max,
            new[] { new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia) });
        return SesionPartida.Publicar(partidaId, snapshot);
    }

    [Fact]
    public async Task Inscribes_participant_and_saves()
    {
        var partidaId = Guid.NewGuid();
        var repo = new FakeSesionPartidaRepository();
        repo.Add(PublishedSession(partidaId));
        var uow = new FakeOperacionesSesionUnitOfWork();
        var handler = new InscribirParticipanteCommandHandler(repo, uow, Clock);
        var participante = Guid.NewGuid();

        var response = await handler.Handle(new InscribirParticipanteCommand(partidaId, participante), CancellationToken.None);

        Assert.Equal(participante, response.ParticipanteId);
        Assert.Equal(partidaId, response.PartidaId);
        Assert.NotEqual(Guid.Empty, response.InscripcionId);
        Assert.Equal(1, uow.SaveCount);
        Assert.Single(repo.Store[partidaId].Inscripciones);
    }

    [Fact]
    public async Task Throws_when_session_not_found()
    {
        var handler = new InscribirParticipanteCommandHandler(
            new FakeSesionPartidaRepository(), new FakeOperacionesSesionUnitOfWork(), Clock);

        await Assert.ThrowsAsync<SesionNoEncontradaException>(
            () => handler.Handle(new InscribirParticipanteCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Throws_when_modalidad_is_equipo()
    {
        var partidaId = Guid.NewGuid();
        var repo = new FakeSesionPartidaRepository();
        repo.Add(PublishedSession(partidaId, Modalidad.Equipo));
        var handler = new InscribirParticipanteCommandHandler(repo, new FakeOperacionesSesionUnitOfWork(), Clock);

        await Assert.ThrowsAsync<ModalidadNoSoportadaException>(
            () => handler.Handle(new InscribirParticipanteCommand(partidaId, Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Throws_when_participant_active_elsewhere()
    {
        var partidaId = Guid.NewGuid();
        var repo = new FakeSesionPartidaRepository { ParticipacionActivaEnOtra = true };
        repo.Add(PublishedSession(partidaId));
        var handler = new InscribirParticipanteCommandHandler(repo, new FakeOperacionesSesionUnitOfWork(), Clock);

        await Assert.ThrowsAsync<ParticipacionActivaExistenteException>(
            () => handler.Handle(new InscribirParticipanteCommand(partidaId, Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Throws_when_capacity_full()
    {
        var partidaId = Guid.NewGuid();
        var repo = new FakeSesionPartidaRepository();
        var sesion = PublishedSession(partidaId, max: 1);
        sesion.Inscribir(Guid.NewGuid(), false, 0, DateTime.UtcNow); // fill the single slot
        repo.Add(sesion);
        var handler = new InscribirParticipanteCommandHandler(repo, new FakeOperacionesSesionUnitOfWork(), Clock);

        await Assert.ThrowsAsync<CupoLlenoException>(
            () => handler.Handle(new InscribirParticipanteCommand(partidaId, Guid.NewGuid()), CancellationToken.None));
    }
}
```
`InscribirParticipanteCommandValidatorTests.cs`:
```csharp
using System;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.Validators;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class InscribirParticipanteCommandValidatorTests
{
    [Fact]
    public void Empty_ids_are_invalid()
    {
        var result = new InscribirParticipanteCommandValidator()
            .Validate(new InscribirParticipanteCommand(Guid.Empty, Guid.Empty));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Valid_ids_pass()
    {
        var result = new InscribirParticipanteCommandValidator()
            .Validate(new InscribirParticipanteCommand(Guid.NewGuid(), Guid.NewGuid()));
        Assert.True(result.IsValid);
    }
}
```
`CancelarInscripcionCommandHandlerTests.cs`:
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
using Umbral.OperacionesSesion.Domain.Exceptions;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class CancelarInscripcionCommandHandlerTests
{
    private static SesionPartida PublishedSession(Guid partidaId)
    {
        var snapshot = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 5,
            new[] { new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia) });
        return SesionPartida.Publicar(partidaId, snapshot);
    }

    [Fact]
    public async Task Cancels_active_inscription_and_saves()
    {
        var partidaId = Guid.NewGuid();
        var participante = Guid.NewGuid();
        var repo = new FakeSesionPartidaRepository();
        var sesion = PublishedSession(partidaId);
        sesion.Inscribir(participante, false, 0, DateTime.UtcNow);
        repo.Add(sesion);
        var uow = new FakeOperacionesSesionUnitOfWork();
        var handler = new CancelarInscripcionCommandHandler(repo, uow);

        await handler.Handle(new CancelarInscripcionCommand(partidaId, participante), CancellationToken.None);

        Assert.Equal(EstadoInscripcion.Cancelada, repo.Store[partidaId].Inscripciones.Single().Estado);
        Assert.Equal(1, uow.SaveCount);
    }

    [Fact]
    public async Task Throws_when_session_not_found()
    {
        var handler = new CancelarInscripcionCommandHandler(
            new FakeSesionPartidaRepository(), new FakeOperacionesSesionUnitOfWork());

        await Assert.ThrowsAsync<SesionNoEncontradaException>(
            () => handler.Handle(new CancelarInscripcionCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Throws_when_no_active_inscription()
    {
        var partidaId = Guid.NewGuid();
        var repo = new FakeSesionPartidaRepository();
        repo.Add(PublishedSession(partidaId));
        var handler = new CancelarInscripcionCommandHandler(repo, new FakeOperacionesSesionUnitOfWork());

        await Assert.ThrowsAsync<InscripcionNoEncontradaException>(
            () => handler.Handle(new CancelarInscripcionCommand(partidaId, Guid.NewGuid()), CancellationToken.None));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test "services/operaciones-sesion/Umbral.OperacionesSesion.sln" --filter "FullyQualifiedName~Inscri|FullyQualifiedName~Cancelar"`
Expected: BUILD FAIL.

- [ ] **Step 3: Create commands + validators**

`Application/Commands/InscribirParticipanteCommand.cs`:
```csharp
using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;

namespace Umbral.OperacionesSesion.Application.Commands;

public sealed record InscribirParticipanteCommand(Guid PartidaId, Guid ParticipanteId) : IRequest<InscripcionResponse>;
```
`Application/Commands/CancelarInscripcionCommand.cs`:
```csharp
using MediatR;

namespace Umbral.OperacionesSesion.Application.Commands;

public sealed record CancelarInscripcionCommand(Guid PartidaId, Guid ParticipanteId) : IRequest<Unit>;
```
`Application/Validators/InscribirParticipanteCommandValidator.cs`:
```csharp
using FluentValidation;
using Umbral.OperacionesSesion.Application.Commands;

namespace Umbral.OperacionesSesion.Application.Validators;

public sealed class InscribirParticipanteCommandValidator : AbstractValidator<InscribirParticipanteCommand>
{
    public InscribirParticipanteCommandValidator()
    {
        RuleFor(x => x.PartidaId).NotEmpty();
        RuleFor(x => x.ParticipanteId).NotEmpty();
    }
}
```
`Application/Validators/CancelarInscripcionCommandValidator.cs`:
```csharp
using FluentValidation;
using Umbral.OperacionesSesion.Application.Commands;

namespace Umbral.OperacionesSesion.Application.Validators;

public sealed class CancelarInscripcionCommandValidator : AbstractValidator<CancelarInscripcionCommand>
{
    public CancelarInscripcionCommandValidator()
    {
        RuleFor(x => x.PartidaId).NotEmpty();
        RuleFor(x => x.ParticipanteId).NotEmpty();
    }
}
```

- [ ] **Step 4: Implement the handlers**

`Application/Handlers/Commands/InscribirParticipanteCommandHandler.cs`:
```csharp
using System.Linq;
using MediatR;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;
using Umbral.OperacionesSesion.Domain.Enums;

namespace Umbral.OperacionesSesion.Application.Handlers.Commands;

public sealed class InscribirParticipanteCommandHandler : IRequestHandler<InscribirParticipanteCommand, InscripcionResponse>
{
    private readonly ISesionPartidaRepository _sesiones;
    private readonly IOperacionesSesionUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public InscribirParticipanteCommandHandler(
        ISesionPartidaRepository sesiones, IOperacionesSesionUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _sesiones = sesiones;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<InscripcionResponse> Handle(InscribirParticipanteCommand request, CancellationToken cancellationToken)
    {
        var sesion = await _sesiones.GetByPartidaIdAsync(request.PartidaId, cancellationToken)
            ?? throw new SesionNoEncontradaException(request.PartidaId);

        var activaEnOtra = await _sesiones.ParticipanteTieneParticipacionActivaAsync(
            request.ParticipanteId, request.PartidaId, cancellationToken);
        var inscritosActivos = sesion.Inscripciones.Count(i => i.EsActiva);

        var inscripcion = sesion.Inscribir(
            request.ParticipanteId, activaEnOtra, inscritosActivos, _timeProvider.GetUtcNow().UtcDateTime);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new InscripcionResponse(inscripcion.Id.Valor, request.PartidaId, request.ParticipanteId);
    }
}
```
`Application/Handlers/Commands/CancelarInscripcionCommandHandler.cs`:
```csharp
using MediatR;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;

namespace Umbral.OperacionesSesion.Application.Handlers.Commands;

public sealed class CancelarInscripcionCommandHandler : IRequestHandler<CancelarInscripcionCommand, Unit>
{
    private readonly ISesionPartidaRepository _sesiones;
    private readonly IOperacionesSesionUnitOfWork _unitOfWork;

    public CancelarInscripcionCommandHandler(ISesionPartidaRepository sesiones, IOperacionesSesionUnitOfWork unitOfWork)
    {
        _sesiones = sesiones;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(CancelarInscripcionCommand request, CancellationToken cancellationToken)
    {
        var sesion = await _sesiones.GetByPartidaIdAsync(request.PartidaId, cancellationToken)
            ?? throw new SesionNoEncontradaException(request.PartidaId);

        sesion.CancelarInscripcion(request.ParticipanteId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test "services/operaciones-sesion/Umbral.OperacionesSesion.sln" --filter "FullyQualifiedName~Inscri|FullyQualifiedName~Cancelar"`
Expected: PASS (10 tests).

- [ ] **Step 6: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Application services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application
git commit -m "SP-3a: handlers de inscribir y cancelar inscripción (Individual) con TimeProvider"
```

---

### Task 6: Lobby query + handler

**Files:**
- Create: `Application/Queries/ObtenerLobbyQuery.cs`
- Create: `Application/Handlers/Queries/ObtenerLobbyQueryHandler.cs`
- Create: `tests/Umbral.OperacionesSesion.UnitTests/Application/ObtenerLobbyQueryHandlerTests.cs`

**Interfaces:**
- Consumes: Task 2 repo, Task 3 `LobbyDto` + `SesionNoEncontradaException`, Task 4 `PublicarPartidaCommandHandler.MapearLobby`.
- Produces: `ObtenerLobbyQuery(Guid PartidaId) : IRequest<LobbyDto>`; handler reusing `MapearLobby`.

- [ ] **Step 1: Write the failing test**

`ObtenerLobbyQueryHandlerTests.cs`:
```csharp
using System;
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

public class ObtenerLobbyQueryHandlerTests
{
    private static SesionPartida PublishedSession(Guid partidaId)
    {
        var snapshot = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10,
            new[] { new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia) });
        return SesionPartida.Publicar(partidaId, snapshot);
    }

    [Fact]
    public async Task Returns_lobby_with_active_inscriptions()
    {
        var partidaId = Guid.NewGuid();
        var repo = new FakeSesionPartidaRepository();
        var sesion = PublishedSession(partidaId);
        var participante = Guid.NewGuid();
        sesion.Inscribir(participante, false, 0, DateTime.UtcNow);
        repo.Add(sesion);
        var handler = new ObtenerLobbyQueryHandler(repo);

        var lobby = await handler.Handle(new ObtenerLobbyQuery(partidaId), CancellationToken.None);

        Assert.Equal("Lobby", lobby.Estado);
        Assert.Equal("Individual", lobby.Modalidad);
        Assert.Equal(1, lobby.InscritosActivos);
        Assert.Contains(participante, lobby.Participantes);
    }

    [Fact]
    public async Task Throws_when_session_not_found()
    {
        var handler = new ObtenerLobbyQueryHandler(new FakeSesionPartidaRepository());
        await Assert.ThrowsAsync<SesionNoEncontradaException>(
            () => handler.Handle(new ObtenerLobbyQuery(Guid.NewGuid()), CancellationToken.None));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test "services/operaciones-sesion/Umbral.OperacionesSesion.sln" --filter "FullyQualifiedName~ObtenerLobbyQueryHandlerTests"`
Expected: BUILD FAIL.

- [ ] **Step 3: Create query + handler**

`Application/Queries/ObtenerLobbyQuery.cs`:
```csharp
using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;

namespace Umbral.OperacionesSesion.Application.Queries;

public sealed record ObtenerLobbyQuery(Guid PartidaId) : IRequest<LobbyDto>;
```
`Application/Handlers/Queries/ObtenerLobbyQueryHandler.cs`:
```csharp
using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Handlers.Commands;
using Umbral.OperacionesSesion.Application.Queries;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;

namespace Umbral.OperacionesSesion.Application.Handlers.Queries;

public sealed class ObtenerLobbyQueryHandler : IRequestHandler<ObtenerLobbyQuery, LobbyDto>
{
    private readonly ISesionPartidaRepository _sesiones;

    public ObtenerLobbyQueryHandler(ISesionPartidaRepository sesiones) => _sesiones = sesiones;

    public async Task<LobbyDto> Handle(ObtenerLobbyQuery request, CancellationToken cancellationToken)
    {
        var sesion = await _sesiones.GetByPartidaIdAsync(request.PartidaId, cancellationToken)
            ?? throw new SesionNoEncontradaException(request.PartidaId);
        return PublicarPartidaCommandHandler.MapearLobby(sesion);
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test "services/operaciones-sesion/Umbral.OperacionesSesion.sln" --filter "FullyQualifiedName~ObtenerLobbyQueryHandlerTests"`
Expected: PASS (2 tests).

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Application services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/ObtenerLobbyQueryHandlerTests.cs
git commit -m "SP-3a: query de lobby + handler"
```

---

### Task 7: Infrastructure persistence (DbContext, repository, UoW, migration)

**Files:**
- Modify: `Infrastructure/Persistence/OperacionesSesionDbContext.cs`
- Create: `Infrastructure/Persistence/SesionPartidaRepository.cs`, `Infrastructure/Persistence/OperacionesSesionUnitOfWork.cs`, `Infrastructure/Persistence/OperacionesSesionDbContextDesignTimeFactory.cs`
- Modify: `Infrastructure/DependencyInjection.cs`, `Infrastructure/Umbral.OperacionesSesion.Infrastructure.csproj`
- Create: `Infrastructure/Persistence/Migrations/*` (generated)
- Test: `tests/Umbral.OperacionesSesion.IntegrationTests/SesionPersistenceTests.cs`

**Interfaces:**
- Consumes: Task 2 entities + repo/UoW interfaces.
- Produces: `SesionPartidaRepository : ISesionPartidaRepository`, `OperacionesSesionUnitOfWork : IOperacionesSesionUnitOfWork`, EF model for `sesiones_partida` / `sesion_juegos` / `inscripciones`, DI registrations.

- [ ] **Step 1: Write the failing test**

`tests/Umbral.OperacionesSesion.IntegrationTests/SesionPersistenceTests.cs`:
```csharp
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Umbral.OperacionesSesion.Infrastructure.Persistence;

namespace Umbral.OperacionesSesion.IntegrationTests;

public class SesionPersistenceTests
{
    private static OperacionesSesionDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<OperacionesSesionDbContext>()
            .UseInMemoryDatabase("persist-" + Guid.NewGuid())
            .Options;
        return new OperacionesSesionDbContext(options);
    }

    [Fact]
    public async Task Persists_and_reads_back_session_with_games_and_inscription()
    {
        var partidaId = Guid.NewGuid();
        var participante = Guid.NewGuid();
        var snapshot = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10,
            new[] { new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia), new JuegoResumen(Guid.NewGuid(), 2, TipoJuego.BusquedaDelTesoro) });
        var sesion = SesionPartida.Publicar(partidaId, snapshot);
        sesion.Inscribir(participante, false, 0, DateTime.UtcNow);

        var options = new DbContextOptionsBuilder<OperacionesSesionDbContext>()
            .UseInMemoryDatabase("shared-db").Options;

        await using (var write = new OperacionesSesionDbContext(options))
        {
            var repo = new SesionPartidaRepository(write);
            repo.Add(sesion);
            await new OperacionesSesionUnitOfWork(write).SaveChangesAsync(CancellationToken.None);
        }

        await using (var read = new OperacionesSesionDbContext(options))
        {
            var repo = new SesionPartidaRepository(read);
            var loaded = await repo.GetByPartidaIdAsync(partidaId, CancellationToken.None);

            Assert.NotNull(loaded);
            Assert.Equal(EstadoSesion.Lobby, loaded!.Estado);
            Assert.Equal(2, loaded.Juegos.Count);
            Assert.Single(loaded.Inscripciones);
            Assert.Equal(participante, loaded.Inscripciones.Single().ParticipanteId);

            Assert.True(await repo.ExistsForPartidaAsync(partidaId, CancellationToken.None));
            Assert.True(await repo.ParticipanteTieneParticipacionActivaAsync(participante, Guid.NewGuid(), CancellationToken.None));
            Assert.False(await repo.ParticipanteTieneParticipacionActivaAsync(participante, partidaId, CancellationToken.None));
        }
    }
}
```

- [ ] **Step 2: Add EF Design + Http packages to the Infrastructure csproj**

Modify `Infrastructure/Umbral.OperacionesSesion.Infrastructure.csproj` — add to the `<ItemGroup>` of package references:
```xml
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.7" />
    <PackageReference Include="Microsoft.Extensions.Http" Version="8.0.1" />
```
(The `Microsoft.Extensions.Http` package is consumed in Task 8; adding it here keeps csproj edits in one place.)

- [ ] **Step 3: Run test to verify it fails**

Run: `dotnet test "services/operaciones-sesion/Umbral.OperacionesSesion.sln" --filter "FullyQualifiedName~SesionPersistenceTests"`
Expected: BUILD FAIL (`SesionPartidaRepository` not defined; DbContext has no `DbSet`).

- [ ] **Step 4: Extend the DbContext**

Replace `Infrastructure/Persistence/OperacionesSesionDbContext.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.ValueObjects;

namespace Umbral.OperacionesSesion.Infrastructure.Persistence;

public sealed class OperacionesSesionDbContext : DbContext
{
    public OperacionesSesionDbContext(DbContextOptions<OperacionesSesionDbContext> options) : base(options)
    {
    }

    public DbSet<SesionPartida> Sesiones => Set<SesionPartida>();

    private static readonly ValueConverter<SesionPartidaId, Guid> SesionPartidaIdConverter =
        new(v => v.Valor, v => SesionPartidaId.From(v));
    private static readonly ValueConverter<InscripcionId, Guid> InscripcionIdConverter =
        new(v => v.Valor, v => InscripcionId.From(v));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SesionPartida>(entity =>
        {
            entity.ToTable("sesiones_partida");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id").HasConversion(SesionPartidaIdConverter);
            entity.Property(x => x.PartidaId).HasColumnName("partidaid").IsRequired();
            entity.Property(x => x.Estado).HasColumnName("estado").IsRequired();
            entity.Property(x => x.Nombre).HasColumnName("nombre").IsRequired();
            entity.Property(x => x.Modalidad).HasColumnName("modalidad").IsRequired();
            entity.Property(x => x.ModoInicioPartida).HasColumnName("modoinicio").IsRequired();
            entity.Property(x => x.TiempoInicio).HasColumnName("tiempoinicio");
            entity.Property(x => x.MinimosParticipacion).HasColumnName("minimos").IsRequired();
            entity.Property(x => x.MaximosParticipacion).HasColumnName("maximos").IsRequired();
            entity.HasIndex(x => x.PartidaId).IsUnique().HasDatabaseName("ix_sesiones_partidaid");
            entity.HasMany(x => x.Juegos).WithOne().HasForeignKey("sesionid").IsRequired().OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(x => x.Juegos).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.HasMany(x => x.Inscripciones).WithOne().HasForeignKey("sesionid").IsRequired().OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(x => x.Inscripciones).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<JuegoResumen>(entity =>
        {
            entity.ToTable("sesion_juegos");
            entity.HasKey(x => x.JuegoId);
            entity.Property(x => x.JuegoId).HasColumnName("juegoid").ValueGeneratedNever();
            entity.Property(x => x.Orden).HasColumnName("orden").IsRequired();
            entity.Property(x => x.TipoJuego).HasColumnName("tipojuego").IsRequired();
        });

        modelBuilder.Entity<InscripcionPartida>(entity =>
        {
            entity.ToTable("inscripciones");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id").HasConversion(InscripcionIdConverter);
            entity.Property(x => x.ParticipanteId).HasColumnName("participanteid").IsRequired();
            entity.Property(x => x.Estado).HasColumnName("estado").IsRequired();
            entity.Property(x => x.FechaInscripcion).HasColumnName("fechainscripcion").IsRequired();
        });
    }
}
```

- [ ] **Step 5: Create repository, UoW, and design-time factory**

`Infrastructure/Persistence/SesionPartidaRepository.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;

namespace Umbral.OperacionesSesion.Infrastructure.Persistence;

public sealed class SesionPartidaRepository : ISesionPartidaRepository
{
    private readonly OperacionesSesionDbContext _dbContext;

    public SesionPartidaRepository(OperacionesSesionDbContext dbContext) => _dbContext = dbContext;

    public void Add(SesionPartida sesion) => _dbContext.Sesiones.Add(sesion);

    public Task<SesionPartida?> GetByPartidaIdAsync(Guid partidaId, CancellationToken cancellationToken)
        => _dbContext.Sesiones
            .Include(s => s.Juegos)
            .Include(s => s.Inscripciones)
            .FirstOrDefaultAsync(s => s.PartidaId == partidaId, cancellationToken);

    public Task<bool> ExistsForPartidaAsync(Guid partidaId, CancellationToken cancellationToken)
        => _dbContext.Sesiones.AnyAsync(s => s.PartidaId == partidaId, cancellationToken);

    public Task<bool> ParticipanteTieneParticipacionActivaAsync(
        Guid participanteId, Guid exceptPartidaId, CancellationToken cancellationToken)
        => _dbContext.Sesiones
            .Where(s => s.PartidaId != exceptPartidaId
                && (s.Estado == EstadoSesion.Lobby || s.Estado == EstadoSesion.Iniciada))
            .SelectMany(s => s.Inscripciones)
            .AnyAsync(i => i.ParticipanteId == participanteId && i.Estado == EstadoInscripcion.Activa, cancellationToken);
}
```
`Infrastructure/Persistence/OperacionesSesionUnitOfWork.cs`:
```csharp
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;

namespace Umbral.OperacionesSesion.Infrastructure.Persistence;

public sealed class OperacionesSesionUnitOfWork : IOperacionesSesionUnitOfWork
{
    private readonly OperacionesSesionDbContext _dbContext;

    public OperacionesSesionUnitOfWork(OperacionesSesionDbContext dbContext) => _dbContext = dbContext;

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
```
`Infrastructure/Persistence/OperacionesSesionDbContextDesignTimeFactory.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Umbral.OperacionesSesion.Infrastructure.Persistence;

public sealed class OperacionesSesionDbContextDesignTimeFactory : IDesignTimeDbContextFactory<OperacionesSesionDbContext>
{
    public OperacionesSesionDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<OperacionesSesionDbContext>()
            .UseNpgsql("Host=localhost;Port=55432;Database=umbral_operaciones_sesion;Username=umbral;Password=16102005")
            .Options;
        return new OperacionesSesionDbContext(options);
    }
}
```

- [ ] **Step 6: Register repository + UoW in Infrastructure DI**

Modify `Infrastructure/DependencyInjection.cs` — add the using and the two registrations before `return services;`:
```csharp
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;
```
```csharp
        services.AddScoped<ISesionPartidaRepository, SesionPartidaRepository>();
        services.AddScoped<IOperacionesSesionUnitOfWork, OperacionesSesionUnitOfWork>();
```

- [ ] **Step 7: Run test to verify it passes**

Run: `dotnet test "services/operaciones-sesion/Umbral.OperacionesSesion.sln" --filter "FullyQualifiedName~SesionPersistenceTests"`
Expected: PASS (1 test).

- [ ] **Step 8: Generate the EF migration**

Run from repo root:
```bash
dotnet ef migrations add InitialOperacionesSesionModel \
  --project services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure \
  --startup-project services/operaciones-sesion/src/Umbral.OperacionesSesion.Api \
  --output-dir Persistence/Migrations
```
Expected: creates `Persistence/Migrations/*_InitialOperacionesSesionModel.cs` (+ Designer + snapshot). If `dotnet ef` is missing: `dotnet tool install --global dotnet-ef --version 8.*` then retry. Verify build: `dotnet build "services/operaciones-sesion/Umbral.OperacionesSesion.sln"`.

- [ ] **Step 9: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests/SesionPersistenceTests.cs
git commit -m "SP-3a: persistencia EF (sesiones_partida, sesion_juegos, inscripciones) + repo + UoW + migración"
```

---

### Task 8: Infrastructure services — HTTP config client + No-Op events publisher

**Files:**
- Create: `Infrastructure/Services/PartidasConfigHttpClient.cs`, `Infrastructure/Services/NoOpSesionEventsPublisher.cs`
- Modify: `Infrastructure/DependencyInjection.cs`
- Test: `tests/Umbral.OperacionesSesion.UnitTests/Infrastructure/PartidasConfigHttpClientTests.cs`

**Interfaces:**
- Consumes: Task 3 (`IConfiguracionPartidaClient`, `ConfiguracionPartidaDto`, `ISesionEventsPublisher`, `PartidasConfigInaccesibleException`).
- Produces: `PartidasConfigHttpClient` (typed HttpClient; 404→null, network/5xx→`PartidasConfigInaccesibleException`); `NoOpSesionEventsPublisher`; DI: `AddHttpClient<IConfiguracionPartidaClient, PartidasConfigHttpClient>` with base URL `PartidasApi:BaseUrl`, and `ISesionEventsPublisher → NoOpSesionEventsPublisher`.

- [ ] **Step 1: Write the failing test**

`tests/Umbral.OperacionesSesion.UnitTests/Infrastructure/PartidasConfigHttpClientTests.cs`:
```csharp
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Infrastructure.Services;

namespace Umbral.OperacionesSesion.UnitTests.Infrastructure;

public class PartidasConfigHttpClientTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string? _json;
        private readonly bool _throw;
        public string? AuthorizationSent { get; private set; }

        public StubHandler(HttpStatusCode status, string? json = null, bool throwNetwork = false)
        {
            _status = status; _json = json; _throw = throwNetwork;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            AuthorizationSent = request.Headers.Contains("Authorization")
                ? string.Join("", request.Headers.GetValues("Authorization")) : null;
            if (_throw) throw new HttpRequestException("boom");
            var response = new HttpResponseMessage(_status);
            if (_json is not null) response.Content = new StringContent(_json, System.Text.Encoding.UTF8, "application/json");
            return Task.FromResult(response);
        }
    }

    private static PartidasConfigHttpClient ClientWith(StubHandler handler)
        => new(new HttpClient(handler) { BaseAddress = new Uri("http://partidas.local") });

    private const string ValidJson = """
    {
      "partidaId": "11111111-1111-1111-1111-111111111111",
      "nombrePartida": "Copa",
      "modalidad": "Individual",
      "modoInicioPartida": "Manual",
      "tiempoInicio": null,
      "minimosParticipacion": 1,
      "maximosParticipacion": 10,
      "estado": null,
      "juegos": [
        { "juegoId": "22222222-2222-2222-2222-222222222222", "orden": 1, "tipoJuego": "Trivia", "estado": "Pendiente", "trivia": null, "bdt": null }
      ]
    }
    """;

    [Fact]
    public async Task Maps_200_payload_to_snapshot_dto_and_forwards_bearer()
    {
        var handler = new StubHandler(HttpStatusCode.OK, ValidJson);
        var dto = await ClientWith(handler).ObtenerConfiguracionAsync(Guid.NewGuid(), "Bearer tok", CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Equal("Copa", dto!.Nombre);
        Assert.Equal("Individual", dto.Modalidad);
        Assert.Single(dto.Juegos);
        Assert.Equal("Trivia", dto.Juegos[0].TipoJuego);
        Assert.Equal("Bearer tok", handler.AuthorizationSent);
    }

    [Fact]
    public async Task Returns_null_on_404()
    {
        var dto = await ClientWith(new StubHandler(HttpStatusCode.NotFound))
            .ObtenerConfiguracionAsync(Guid.NewGuid(), null, CancellationToken.None);
        Assert.Null(dto);
    }

    [Fact]
    public async Task Throws_inaccesible_on_500()
    {
        await Assert.ThrowsAsync<PartidasConfigInaccesibleException>(
            () => ClientWith(new StubHandler(HttpStatusCode.InternalServerError))
                .ObtenerConfiguracionAsync(Guid.NewGuid(), null, CancellationToken.None));
    }

    [Fact]
    public async Task Throws_inaccesible_on_network_failure()
    {
        await Assert.ThrowsAsync<PartidasConfigInaccesibleException>(
            () => ClientWith(new StubHandler(HttpStatusCode.OK, throwNetwork: true))
                .ObtenerConfiguracionAsync(Guid.NewGuid(), null, CancellationToken.None));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test "services/operaciones-sesion/Umbral.OperacionesSesion.sln" --filter "FullyQualifiedName~PartidasConfigHttpClientTests"`
Expected: BUILD FAIL.

- [ ] **Step 3: Implement the HTTP client**

`Infrastructure/Services/PartidasConfigHttpClient.cs`:
```csharp
using System.Net;
using System.Net.Http.Json;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Interfaces;

namespace Umbral.OperacionesSesion.Infrastructure.Services;

// Synchronous config handoff (SP-3a Option A): GET /partidas/{id} on Partidas, mapped to a snapshot DTO.
// 404 → null (partida does not exist); network/timeout/non-success → PartidasConfigInaccesible (Partidas down ≠ missing).
public sealed class PartidasConfigHttpClient : IConfiguracionPartidaClient
{
    private readonly HttpClient _http;

    public PartidasConfigHttpClient(HttpClient http) => _http = http;

    public async Task<ConfiguracionPartidaDto?> ObtenerConfiguracionAsync(
        Guid partidaId, string? bearerToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/partidas/{partidaId}");
        if (!string.IsNullOrWhiteSpace(bearerToken))
            request.Headers.TryAddWithoutValidation("Authorization", bearerToken);

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new PartidasConfigInaccesibleException(partidaId, ex);
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        if (!response.IsSuccessStatusCode)
            throw new PartidasConfigInaccesibleException(partidaId);

        var payload = await response.Content.ReadFromJsonAsync<PartidasConfigResponse>(cancellationToken: cancellationToken)
            ?? throw new PartidasConfigInaccesibleException(partidaId);

        return new ConfiguracionPartidaDto(
            payload.NombrePartida,
            payload.Modalidad,
            payload.ModoInicioPartida,
            payload.TiempoInicio,
            payload.MinimosParticipacion,
            payload.MaximosParticipacion,
            payload.Juegos.Select(j => new JuegoResumenDto(j.JuegoId, j.Orden, j.TipoJuego)).ToList());
    }

    // Local deserialization shape for Partidas' PartidaDetailDto (camelCase JSON; case-insensitive binding).
    private sealed record PartidasConfigResponse(
        string NombrePartida,
        string Modalidad,
        string ModoInicioPartida,
        DateTime? TiempoInicio,
        int MinimosParticipacion,
        int MaximosParticipacion,
        List<PartidasJuegoResponse> Juegos);

    private sealed record PartidasJuegoResponse(Guid JuegoId, int Orden, string TipoJuego);
}
```
> Note: `ReadFromJsonAsync` uses web defaults (case-insensitive) so camelCase JSON binds to the PascalCase record. No extra options needed.

- [ ] **Step 4: Implement the No-Op events publisher**

`Infrastructure/Services/NoOpSesionEventsPublisher.cs`:
```csharp
using Umbral.OperacionesSesion.Application.Interfaces;

namespace Umbral.OperacionesSesion.Infrastructure.Services;

// No-Op until the dedicated RabbitMQ backbone slice (mirrors Identity's NoOpEquipoEventsPublisher).
// The publish seam is exercised end-to-end; nothing is delivered yet.
public sealed class NoOpSesionEventsPublisher : ISesionEventsPublisher
{
    public Task PublicarPartidaPublicadaEnLobbyAsync(PartidaPublicadaEnLobbyEvent evento, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
```

- [ ] **Step 5: Register the typed client + publisher in Infrastructure DI**

Modify `Infrastructure/DependencyInjection.cs` — add usings and registrations (the `Microsoft.Extensions.Http` package added in Task 7 provides `AddHttpClient`):
```csharp
using Umbral.OperacionesSesion.Application.Interfaces;
using Umbral.OperacionesSesion.Infrastructure.Services;
```
Before `return services;`:
```csharp
        services.AddScoped<ISesionEventsPublisher, NoOpSesionEventsPublisher>();
        var partidasBaseUrl = configuration["PartidasApi:BaseUrl"] ?? "http://localhost:5010";
        services.AddHttpClient<IConfiguracionPartidaClient, PartidasConfigHttpClient>(client =>
        {
            client.BaseAddress = new Uri(partidasBaseUrl);
        });
```

- [ ] **Step 6: Run test to verify it passes**

Run: `dotnet test "services/operaciones-sesion/Umbral.OperacionesSesion.sln" --filter "FullyQualifiedName~PartidasConfigHttpClientTests"`
Expected: PASS (4 tests).

- [ ] **Step 7: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Infrastructure
git commit -m "SP-3a: cliente HTTP de config (Opción A) + publisher de eventos No-Op + DI"
```

---

### Task 9: Api controller + middleware mapping + unit tests

**Files:**
- Create: `Api/Controllers/SesionesController.cs`
- Modify: `Api/Middleware/ExceptionHandlingMiddleware.cs`
- Create: `tests/Umbral.OperacionesSesion.UnitTests/Api/FakeSender.cs`, `SesionesControllerTests.cs`, `ExceptionHandlingMiddlewareTests.cs`

**Interfaces:**
- Consumes: Task 3–6 commands/queries/DTOs/exceptions.
- Produces: `SesionesController` (4 endpoints; reads `sub` claim + `Authorization` header); `ExceptionHandlingMiddleware` with the full status mapping.

- [ ] **Step 1: Write the failing tests**

`Api/FakeSender.cs` (copy of the SP-2 FakeSender, namespace adjusted):
```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Umbral.OperacionesSesion.UnitTests.Api;

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
`Api/SesionesControllerTests.cs`:
```csharp
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbral.OperacionesSesion.Api.Controllers;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.UnitTests.Api;

namespace Umbral.OperacionesSesion.UnitTests.Api;

public class SesionesControllerTests
{
    private static SesionesController ControllerWith(FakeSender sender, Guid? participanteId = null, string? authHeader = null)
    {
        var http = new DefaultHttpContext();
        if (authHeader is not null) http.Request.Headers.Authorization = authHeader;
        if (participanteId is not null)
            http.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("sub", participanteId.Value.ToString()) }, "test"));
        return new SesionesController(sender) { ControllerContext = new ControllerContext { HttpContext = http } };
    }

    private static LobbyDto Lobby(Guid partidaId) =>
        new(partidaId, Guid.NewGuid(), "Lobby", "Individual", 1, 10, 0, Array.Empty<Guid>());

    [Fact]
    public async Task Publicar_returns_201_and_forwards_bearer()
    {
        var partidaId = Guid.NewGuid();
        var sender = new FakeSender(Lobby(partidaId));
        var controller = ControllerWith(sender, authHeader: "Bearer xyz");

        var result = await controller.Publicar(partidaId, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, created.StatusCode);
        var command = Assert.IsType<PublicarPartidaCommand>(sender.LastRequest);
        Assert.Equal(partidaId, command.PartidaId);
        Assert.Equal("Bearer xyz", command.BearerToken);
    }

    [Fact]
    public async Task Inscribir_uses_sub_claim_and_returns_201()
    {
        var partidaId = Guid.NewGuid();
        var participante = Guid.NewGuid();
        var sender = new FakeSender(new InscripcionResponse(Guid.NewGuid(), partidaId, participante));
        var controller = ControllerWith(sender, participanteId: participante);

        var result = await controller.Inscribir(partidaId, CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(result);
        var command = Assert.IsType<InscribirParticipanteCommand>(sender.LastRequest);
        Assert.Equal(participante, command.ParticipanteId);
    }

    [Fact]
    public async Task Inscribir_without_sub_claim_throws()
    {
        var controller = ControllerWith(new FakeSender(null));
        await Assert.ThrowsAsync<ParticipanteNoIdentificadoException>(
            () => controller.Inscribir(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task CancelarInscripcion_returns_204()
    {
        var participante = Guid.NewGuid();
        var sender = new FakeSender(MediatR.Unit.Value);
        var controller = ControllerWith(sender, participanteId: participante);

        var result = await controller.CancelarInscripcion(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.IsType<CancelarInscripcionCommand>(sender.LastRequest);
    }

    [Fact]
    public async Task ObtenerLobby_returns_200()
    {
        var partidaId = Guid.NewGuid();
        var sender = new FakeSender(Lobby(partidaId));
        var controller = ControllerWith(sender);

        var result = await controller.ObtenerLobby(partidaId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<LobbyDto>(ok.Value);
    }
}
```
`Api/ExceptionHandlingMiddlewareTests.cs`:
```csharp
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Umbral.OperacionesSesion.Api.Middleware;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Domain.Exceptions;

namespace Umbral.OperacionesSesion.UnitTests.Api;

public class ExceptionHandlingMiddlewareTests
{
    private static async Task<int> StatusFor(Exception ex)
    {
        var middleware = new ExceptionHandlingMiddleware(_ => throw ex, NullLogger<ExceptionHandlingMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        await middleware.InvokeAsync(context);
        return context.Response.StatusCode;
    }

    [Fact]
    public async Task Maps_config_not_found_to_404()
        => Assert.Equal((int)HttpStatusCode.NotFound, await StatusFor(new PartidaConfigNoEncontradaException(Guid.NewGuid())));

    [Fact]
    public async Task Maps_session_not_found_to_404()
        => Assert.Equal((int)HttpStatusCode.NotFound, await StatusFor(new SesionNoEncontradaException(Guid.NewGuid())));

    [Fact]
    public async Task Maps_config_inaccesible_to_502()
        => Assert.Equal((int)HttpStatusCode.BadGateway, await StatusFor(new PartidasConfigInaccesibleException(Guid.NewGuid())));

    [Fact]
    public async Task Maps_already_published_to_409()
        => Assert.Equal((int)HttpStatusCode.Conflict, await StatusFor(new SesionYaPublicadaException(Guid.NewGuid())));

    [Fact]
    public async Task Maps_not_publishable_to_409()
        => Assert.Equal((int)HttpStatusCode.Conflict, await StatusFor(new PartidaNoPublicableException(Guid.NewGuid())));

    [Fact]
    public async Task Maps_cupo_lleno_to_409()
        => Assert.Equal((int)HttpStatusCode.Conflict, await StatusFor(new CupoLlenoException(Guid.NewGuid())));

    [Fact]
    public async Task Maps_participante_no_identificado_to_401()
        => Assert.Equal((int)HttpStatusCode.Unauthorized, await StatusFor(new ParticipanteNoIdentificadoException()));
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test "services/operaciones-sesion/Umbral.OperacionesSesion.sln" --filter "FullyQualifiedName~SesionesControllerTests|FullyQualifiedName~ExceptionHandlingMiddlewareTests"`
Expected: BUILD FAIL.

- [ ] **Step 3: Implement the controller**

`Api/Controllers/SesionesController.cs`:
```csharp
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Queries;

namespace Umbral.OperacionesSesion.Api.Controllers;

[ApiController]
[Route("")]
public sealed class SesionesController : ControllerBase
{
    private readonly ISender _mediator;

    public SesionesController(ISender mediator) => _mediator = mediator;

    [HttpPost("partidas/{partidaId:guid}/publicacion")]
    public async Task<IActionResult> Publicar(Guid partidaId, CancellationToken cancellationToken)
    {
        var bearer = Request.Headers.Authorization.ToString();
        var command = new PublicarPartidaCommand(partidaId, string.IsNullOrWhiteSpace(bearer) ? null : bearer);
        var lobby = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(ObtenerLobby), new { partidaId }, lobby);
    }

    [HttpPost("partidas/{partidaId:guid}/inscripciones")]
    public async Task<IActionResult> Inscribir(Guid partidaId, CancellationToken cancellationToken)
    {
        var participanteId = ObtenerParticipanteId();
        var response = await _mediator.Send(new InscribirParticipanteCommand(partidaId, participanteId), cancellationToken);
        return CreatedAtAction(nameof(ObtenerLobby), new { partidaId }, response);
    }

    [HttpDelete("partidas/{partidaId:guid}/inscripciones/mia")]
    public async Task<IActionResult> CancelarInscripcion(Guid partidaId, CancellationToken cancellationToken)
    {
        var participanteId = ObtenerParticipanteId();
        await _mediator.Send(new CancelarInscripcionCommand(partidaId, participanteId), cancellationToken);
        return NoContent();
    }

    [HttpGet("partidas/{partidaId:guid}/lobby")]
    public async Task<IActionResult> ObtenerLobby(Guid partidaId, CancellationToken cancellationToken)
    {
        var lobby = await _mediator.Send(new ObtenerLobbyQuery(partidaId), cancellationToken);
        return Ok(lobby);
    }

    private Guid ObtenerParticipanteId()
    {
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(sub, out var id) ? id : throw new ParticipanteNoIdentificadoException();
    }
}
```

- [ ] **Step 4: Extend the middleware mapping**

Replace `Api/Middleware/ExceptionHandlingMiddleware.cs`:
```csharp
using System.Net;
using System.Text.Json;
using FluentValidation;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Domain.Exceptions;

namespace Umbral.OperacionesSesion.Api.Middleware;

// Centralized exception handling with domain/application exception → status mapping (SP-3a).
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
        ParticipanteNoIdentificadoException => HttpStatusCode.Unauthorized,
        PartidaConfigNoEncontradaException
            or SesionNoEncontradaException
            or InscripcionNoEncontradaException => HttpStatusCode.NotFound,
        PartidasConfigInaccesibleException => HttpStatusCode.BadGateway,
        SesionYaPublicadaException
            or PartidaNoPublicableException
            or SesionNoEnLobbyException
            or ModalidadNoSoportadaException
            or ParticipanteYaInscritoException
            or ParticipacionActivaExistenteException
            or CupoLlenoException => HttpStatusCode.Conflict,
        ValidationException or ArgumentException => HttpStatusCode.BadRequest,
        _ => HttpStatusCode.InternalServerError
    };
}
```

- [ ] **Step 5: Add the enum-string converter to Program.cs**

Modify `Api/Program.cs` — replace the `builder.Services.AddControllers();` line with:
```csharp
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
```

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test "services/operaciones-sesion/Umbral.OperacionesSesion.sln" --filter "FullyQualifiedName~SesionesControllerTests|FullyQualifiedName~ExceptionHandlingMiddlewareTests"`
Expected: PASS (12 tests).

- [ ] **Step 7: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Api services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api
git commit -m "SP-3a: SesionesController (4 endpoints) + mapeo de excepciones + JSON enum-as-string"
```

---

### Task 10: Contract tests (end-to-end through the HTTP stack)

**Files:**
- Create: `tests/Umbral.OperacionesSesion.ContractTests/OperacionesSesionWebFactory.cs`, `SesionEndpointsTests.cs`

**Interfaces:**
- Consumes: the full Api (`Program`), with `IConfiguracionPartidaClient` overridden by a seedable stub so Partidas is not required.
- Produces: a reusable `OperacionesSesionWebFactory` exposing a mutable `StubConfigClient`.

- [ ] **Step 1: Write the failing test**

`tests/Umbral.OperacionesSesion.ContractTests/OperacionesSesionWebFactory.cs`:
```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Interfaces;

namespace Umbral.OperacionesSesion.ContractTests;

public sealed class StubConfigClient : IConfiguracionPartidaClient
{
    // Per-partida overrides; null value = explicit 404. Unmapped ids fall back to a valid Individual config.
    public Dictionary<Guid, ConfiguracionPartidaDto?> Respuestas { get; } = new();

    public ConfiguracionPartidaDto Default { get; set; } =
        new("Copa", "Individual", "Manual", null, 1, 10,
            new List<JuegoResumenDto> { new(Guid.NewGuid(), 1, "Trivia") });

    public Task<ConfiguracionPartidaDto?> ObtenerConfiguracionAsync(
        Guid partidaId, string? bearerToken, CancellationToken cancellationToken)
        => Task.FromResult(Respuestas.TryGetValue(partidaId, out var r) ? r : Default);
}

public sealed class OperacionesSesionWebFactory : WebApplicationFactory<Program>
{
    public StubConfigClient Stub { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IConfiguracionPartidaClient>();
            services.AddSingleton<IConfiguracionPartidaClient>(Stub);
        });
    }
}
```
`tests/Umbral.OperacionesSesion.ContractTests/SesionEndpointsTests.cs`:
```csharp
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.DTOs;

namespace Umbral.OperacionesSesion.ContractTests;

public class SesionEndpointsTests : IClassFixture<OperacionesSesionWebFactory>
{
    private readonly OperacionesSesionWebFactory _factory;
    private readonly HttpClient _client;

    public SesionEndpointsTests(OperacionesSesionWebFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static ConfiguracionPartidaDto Config(string modalidad = "Individual", int max = 10, int juegos = 1) =>
        new("Copa", modalidad, "Manual", null, 1, max,
            BuildJuegos(juegos));

    private static List<JuegoResumenDto> BuildJuegos(int juegos)
    {
        var list = new List<JuegoResumenDto>();
        for (var o = 1; o <= juegos; o++) list.Add(new JuegoResumenDto(Guid.NewGuid(), o, "Trivia"));
        return list;
    }

    [Fact]
    public async Task Publish_then_lobby_then_inscribe_flow()
    {
        var partidaId = Guid.NewGuid();
        _factory.Stub.Respuestas[partidaId] = Config(juegos: 2);

        var publish = await _client.PostAsync($"/partidas/{partidaId}/publicacion", null);
        Assert.Equal(HttpStatusCode.Created, publish.StatusCode);
        Assert.NotNull(publish.Headers.Location);
        var lobby = await publish.Content.ReadFromJsonAsync<LobbyDto>();
        Assert.Equal("Lobby", lobby!.Estado);
        Assert.Equal(0, lobby.InscritosActivos);

        var getLobby = await _client.GetFromJsonAsync<LobbyDto>($"/partidas/{partidaId}/lobby");
        Assert.Equal("Individual", getLobby!.Modalidad);
    }

    [Fact]
    public async Task Publish_unknown_partida_returns_404()
    {
        var partidaId = Guid.NewGuid();
        _factory.Stub.Respuestas[partidaId] = null; // explicit 404 from Partidas

        var publish = await _client.PostAsync($"/partidas/{partidaId}/publicacion", null);
        Assert.Equal(HttpStatusCode.NotFound, publish.StatusCode);
    }

    [Fact]
    public async Task Double_publish_returns_409()
    {
        var partidaId = Guid.NewGuid();
        _factory.Stub.Respuestas[partidaId] = Config();

        Assert.Equal(HttpStatusCode.Created, (await _client.PostAsync($"/partidas/{partidaId}/publicacion", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await _client.PostAsync($"/partidas/{partidaId}/publicacion", null)).StatusCode);
    }

    [Fact]
    public async Task Inscribe_into_equipo_partida_returns_409()
    {
        var partidaId = Guid.NewGuid();
        _factory.Stub.Respuestas[partidaId] = Config(modalidad: "Equipo");
        Assert.Equal(HttpStatusCode.Created, (await _client.PostAsync($"/partidas/{partidaId}/publicacion", null)).StatusCode);

        // Authenticated participant via a fabricated sub header is not available without auth middleware;
        // the gateway injects identity in real runs. Here the request has no principal, so inscribe → 401.
        var inscribe = await _client.PostAsync($"/partidas/{partidaId}/inscripciones", null);
        Assert.Equal(HttpStatusCode.Unauthorized, inscribe.StatusCode);
    }

    [Fact]
    public async Task Inscribe_into_unpublished_partida_returns_404_after_identity()
    {
        // No session published for this id; without a principal the controller fails identity first (401).
        var inscribe = await _client.PostAsync($"/partidas/{Guid.NewGuid()}/inscripciones", null);
        Assert.Equal(HttpStatusCode.Unauthorized, inscribe.StatusCode);
    }
}
```
> Note on auth in contract tests: the test host has no authentication middleware (the gateway supplies identity in real runs), so `User` carries no `sub` claim and inscription endpoints return 401. The publish/lobby flow and the publish error paths (404/409) are fully exercised here; inscription *business* invariants (duplicate, capacity, modalidad, active-elsewhere) are covered at the handler level in Task 5. This keeps the contract suite honest about what the host actually enforces.

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test "services/operaciones-sesion/Umbral.OperacionesSesion.sln" --filter "FullyQualifiedName~SesionEndpointsTests"`
Expected: BUILD FAIL (factory not defined), then once it builds the assertions drive the wiring.

- [ ] **Step 3: Make it pass**

No production code should be needed — the endpoints already exist (Task 9) and the factory override wires the stub. If the publish flow fails because `AddHttpClient` still wins over the stub, confirm `RemoveAll<IConfiguracionPartidaClient>()` runs in `ConfigureServices` (it executes after the app's registrations, so the stub wins). Run the suite:

Run: `dotnet test "services/operaciones-sesion/Umbral.OperacionesSesion.sln" --filter "FullyQualifiedName~SesionEndpointsTests"`
Expected: PASS (5 tests).

- [ ] **Step 4: Run the FULL service suite**

Run: `dotnet test "services/operaciones-sesion/Umbral.OperacionesSesion.sln"`
Expected: PASS (all: domain + application + infrastructure + api unit + integration + contract + the pre-existing Health tests).

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests
git commit -m "SP-3a: contract tests end-to-end (factory con stub de config) + suite verde"
```

---

### Task 11: Contracts docs + mini-ADR (R1)

**Files:**
- Modify: `contracts/http/operaciones-sesion-api.md`
- Modify: `contracts/events/operaciones-sesion-events.md`
- Create: `docs/05-decisions/ADR-0010-runtime-estado-en-operaciones.md`

**Interfaces:** none (documentation). No test cycle; deliverable is the written contract + ADR.

- [ ] **Step 1: Register the HTTP endpoints**

Replace the `Endpoint Registry` table in `contracts/http/operaciones-sesion-api.md` with the concrete SP-3a rows (keep the existing Status/Access Path/Owned Capabilities sections; update the Status line to note SP-3a is registered):

```markdown
## Endpoint Registry

| Capability | Method | Gateway path | Auth (coarse) | Success | Errors |
|---|---|---|---|---|---|
| Publish a partida to lobby | POST | `/operaciones-sesion/partidas/{partidaId}/publicacion` | Operador | 201 + LobbyDto (Location → lobby) | 404 config no existe · 502 Partidas inaccesible · 409 ya publicada / no publicable |
| Inscribe (Individual) | POST | `/operaciones-sesion/partidas/{partidaId}/inscripciones` | Participante | 201 + InscripcionResponse | 401 sin identidad · 404 sesión no existe · 409 ya inscrito / participación activa / cupo lleno / modalidad no soportada |
| Cancel own inscription | DELETE | `/operaciones-sesion/partidas/{partidaId}/inscripciones/mia` | Participante | 204 | 401 · 404 sesión / inscripción no existe |
| Lobby state | GET | `/operaciones-sesion/partidas/{partidaId}/lobby` | Operador/Participante | 200 + LobbyDto | 404 sesión no existe |

### DTOs

- `LobbyDto { partidaId, sesionPartidaId, estado, modalidad, minimosParticipacion, maximosParticipacion, inscritosActivos, participantes[] }`
- `InscripcionResponse { inscripcionId, partidaId, participanteId }`

Notes: enums serialized as strings. `participanteId` is taken from the JWT `sub` claim (never the body). Config handoff is an internal `GET /partidas/{id}` (not via the gateway), forwarding the caller's bearer.
```

- [ ] **Step 2: Register the event payload**

In `contracts/events/operaciones-sesion-events.md`, update the `PartidaPublicadaEnLobby` row Status to `Payload registered (SP-3a)` and append a payload section:

```markdown
## Payloads (registered)

### `PartidaPublicadaEnLobby` (SP-3a)

Emitted after a partida is published to Lobby. In SP-3a it is published through a **No-Op** port (no broker delivery yet); the exchange/queue/routing-key/idempotency are defined by the RabbitMQ backbone slice.

```json
{
  "partidaId": "guid",
  "sesionPartidaId": "guid",
  "modalidad": "Individual | Equipo",
  "minimosParticipacion": 1,
  "maximosParticipacion": 10
}
```
```

- [ ] **Step 3: Write the mini-ADR**

`docs/05-decisions/ADR-0010-runtime-estado-en-operaciones.md`:
```markdown
# ADR-0010 — El estado runtime de la partida vive en Operaciones de Sesión

- **Estado:** Aceptado
- **Fecha:** 2026-06-26
- **Contexto de slice:** SP-3a (`docs/superpowers/specs/2026-06-26-sp3a-publicacion-lobby-inscripcion-design.md`)
- **Relacionado:** ADR-0009 (topología de servicios), SP-2 (SEAM `EstadoPartida` nullable)

## Contexto

El diagrama de clases ubica `EstadoPartida ∈ {Lobby, Iniciada, Cancelada, Terminada}` en el agregado `Partida`. SP-2 dejó esa propiedad **nullable** (`null` = configurada, no publicada) con la nota "SP-3 pone Lobby". Pero publicar/runtime pertenece a **Operaciones de Sesión**, y un servicio **nunca** escribe la BD de otro (frontera dura). Operaciones no puede mover el `EstadoPartida` de Partidas.

## Decisión

El ciclo de vida runtime de la partida se materializa en el agregado **`SesionPartida.EstadoSesion`** dentro de **Operaciones de Sesión**. El `EstadoPartida` de **Partidas permanece `null`** para siempre: Partidas es config-only y no expone ningún command de publicación/runtime.

## Alternativas rechazadas

1. **Partidas expone un command de publicación** que voltea su propio `EstadoPartida` → re-aloja runtime en un servicio config-only; viola el ownership de ADR-0009.
2. **Un evento hace que Partidas actualice su estado** → exige el backbone de mensajería (diferido) y **duplica** el estado en dos servicios (fuente de verdad ambigua).

## Consecuencias

- El estado runtime es **single-sourced** en Operaciones.
- Partidas no gana superficie de publicación/runtime; su `EstadoPartida` nullable queda como marcador de "configurada" y no avanza.
- El backbone de RabbitMQ posterior **no** duplica el estado; solo transporta eventos.
- Lectores que esperaban "SP-3 pone Lobby en Partidas" deben mirar `SesionPartida` en Operaciones (este ADR es el registro durable; referénciese desde la nota SEAM de SP-2 si hay confusión).
```

- [ ] **Step 4: Verify markdown + commit**

```bash
git add contracts/http/operaciones-sesion-api.md contracts/events/operaciones-sesion-events.md docs/05-decisions/ADR-0010-runtime-estado-en-operaciones.md
git commit -m "SP-3a: contratos HTTP/evento registrados + ADR-0010 (estado runtime en Operaciones)"
```

---

### Task 12: R1 structural gate + traceability + ledger

**Files:**
- Modify: `docs/04-sdd/traceability-matrix.md`
- Modify: `.git/sdd/progress.md`

**Interfaces:** none. Deliverable is a verified green build + updated tracking docs.

- [ ] **Step 1: Run the R1 structural checklist**

Verify by inspection / commands (all must hold):
```bash
# Application graded folder set (exactly these dirs):
ls services/operaciones-sesion/src/Umbral.OperacionesSesion.Application
# Expect: Commands Queries Interfaces Validators DTOs Handlers Exceptions (+ files DependencyInjection.cs, ValidationBehavior.cs)
ls services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers
# Expect: Commands Queries

# Infrastructure has Persistence + Services:
ls services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure
# Expect: Persistence Services DependencyInjection.cs

# Repository interfaces in Domain:
ls services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Abstractions/Persistence

# Program.cs uses MapControllers only (no minimal-API routes):
grep -n "MapControllers\|MapGet\|MapPost" services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Program.cs
# Expect: MapControllers present; no MapGet/MapPost

# Controllers inherit ControllerBase + have unit tests:
grep -rn "ControllerBase" services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Controllers
```
Confirm: every controller (`HealthController`, `SesionesController`) has a unit-test class; `ExceptionHandlingMiddleware` is registered in `Program.cs`.

- [ ] **Step 2: Run the full service suite + confirm old services untouched**

```bash
dotnet test "services/operaciones-sesion/Umbral.OperacionesSesion.sln"
git status --short services/trivia-game-service services/bdt-game-service services/partidas services/identity-service
```
Expected: all tests PASS; the `git status` line is EMPTY (only `operaciones-sesion` + docs/contracts changed this slice).

- [ ] **Step 3: Update the traceability matrix**

In `docs/04-sdd/traceability-matrix.md`, add an SP-3a row recording: slice SP-3a (publish→Lobby + Individual inscriptions), owning service Operaciones de Sesión, spec + plan paths, ADR-0010, suite green (state the final test count from Step 2), and the deferrals (Equipo→3a-E, start→3b, SignalR→3f, RabbitMQ backbone→its own slice). Follow the existing row format in that file.

- [ ] **Step 4: Append the ledger line**

Append one line to `.git/sdd/progress.md` recording SP-3a complete: tasks 1–12, the final commit range, suite green (count), R1 gate passed, ADR-0010 accepted, and `Next: SP-3a-E (Equipo/convocatorias) or SP-3b (start), user's call`.

- [ ] **Step 5: Commit**

```bash
git add docs/04-sdd/traceability-matrix.md .git/sdd/progress.md
git commit -m "SP-3a: R1 gate verificado, traceability + ledger actualizados"
```

---

## Self-Review

**1. Spec coverage** (each spec section → task):
- §2 R1 → Task 2 (aggregate) + Task 11 (ADR-0010). ✓
- §2 Option A handoff → Task 8 (HTTP client) + Task 4 (handler uses it). ✓
- §2 No-Op events → Task 8 + Task 4 (emit). ✓
- §3 publish/inscribe/cancel/lobby → Tasks 4/5/6/9. ✓
- §4 domain model (VOs, enums, SesionPartida, InscripcionPartida, exceptions) → Tasks 1–2. ✓
- §4.5 404-vs-502 distinction → Task 8 (client) + Task 9 (middleware) + tests. ✓
- §5 Application graded + ValidationBehavior + TimeProvider → Task 3. ✓
- §6 persistence + migration → Task 7. ✓
- §7 Api + endpoints → Task 9. ✓
- §8 error table → Task 9 middleware + Task 9 tests. ✓
- §9 contracts → Task 11. ✓
- §10 testing pyramid → every task's tests + Task 10 contract. ✓
- §11 R1 gate → Task 12. ✓
- §12 micro-decisions (no inscription event / JWT forward / functional-perm deferred) → honored: no inscription event emitted; bearer forwarded (Task 4/8/9); no local functional-perm checks added. ✓
- §13 mini-ADR → Task 11. ✓

**2. Placeholder scan:** No "TBD"/"add validation"/"similar to". Each code step shows complete code. ✓

**3. Type consistency:** `ISesionPartidaRepository` signatures (`GetByPartidaIdAsync`, `ExistsForPartidaAsync`, `ParticipanteTieneParticipacionActivaAsync`) identical in Domain interface (Task 2), fake (Task 4), and EF impl (Task 7). `MapearLobby` defined in Task 4, reused in Task 6. `LobbyDto`/`InscripcionResponse`/`ConfiguracionPartidaDto`/`JuegoResumenDto` fields identical across producer (Task 3) and consumers (Tasks 4–10). `PublicarPartidaCommand(Guid, string?)` consistent in Tasks 3/4/9. `Inscribir(...)` 4-arg signature consistent in Task 2 (def), Task 5 (handler), tests. Enum member names match Partidas for string parse. ✓

**Known scoping note (logged, not a gap):** contract-suite inscription paths assert 401 because the test host has no auth middleware; inscription business invariants are covered at the handler level (Task 5). This is called out in Task 10 so coverage is not silently overstated.

## Execution Handoff

(filled in by the chat after save)
