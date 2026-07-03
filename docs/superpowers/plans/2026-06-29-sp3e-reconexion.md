# SP-3e — Reconexión / recuperación de estado transitorio (Individual) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Exponer `GET /operaciones-sesion/mi-sesion`, un agregador read-only que devuelve el snapshot de estado vigente del participante (RF-14/RB-33) en una sola llamada.

**Architecture:** CQRS Query de solo lectura en Operaciones de Sesión. Un nuevo método de repositorio halla la `SesionPartida` de la participación activa **vigente** del participante (inscripción `Activa` en partida `Lobby`/`Iniciada`) con eager-load del grafo; el handler la proyecta a `MiSesionDto` reusando los sub-DTOs participant-safe ya existentes (`PreguntaActualDto`, `EtapaActualDto`). Sin mutación, sin eventos.

**Tech Stack:** .NET 8, MediatR, EF Core 8 (Npgsql + InMemory en tests), xUnit, WebApplicationFactory.

**Spec:** `docs/superpowers/specs/2026-06-29-sp3e-reconexion-design.md`

## Global Constraints

- **Servicio:** `services/operaciones-sesion/`. Base de la rama: `feature/code-migration-SP-3` (HEAD `d583e4e` al planear).
- **Read-only:** la slice no muta estado, **no emite eventos**, no toca el publisher. Es un `IRequest<MiSesionDto?>`.
- **Individual-only:** participación resuelta vía `InscripcionPartida` individual. Equipo/convocatoria → slice-E (no implementar).
- **No-leak:** `MiSesionDto` solo reusa `PreguntaActualDto`/`EtapaActualDto`; jamás `codigoQREsperado` ni el flag de opción correcta.
- **Value objects:** los IDs usan `.Valor` (no `.Value`): `sesion.Id.Valor`, `inscripcion.Id.Valor`.
- **Enums serializados como string** (configuración JSON ya existente del servicio): proyectar con `.ToString()`.
- **No Moq:** los unit tests reusan `FakeSender` (controller) y `FakeSesionPartidaRepository` (handler).
- **Git hygiene (memoria `subagent-git-cleanup-hazard`):** prohibido `git checkout`/`restore`/`clean`/`stash`/`reset`; nunca `git add -A`/`.`/`docs/`. Solo `git add` de archivos nombrados.
- **Git carve-out (decisión del usuario, vigente):** `docs/04-sdd/traceability-matrix.md`, `docs/superpowers/specs/2026-06-27-sp3c-runtime-trivia-design.md` y `docs/04-sdd/auditorias/` quedan modificados/sin commitear (squash del usuario). La fila SP-3e en la matriz se **escribe pero NO se commitea** (Task 5).
- **Suite de referencia (verde al iniciar):** UnitTests 172, IntegrationTests 8, ContractTests 28.

---

### Task 1: Repo `GetByParticipanteActivoAsync` (interface + EF + fake)

**Files:**
- Modify: `src/Umbral.OperacionesSesion.Domain/Abstractions/Persistence/ISesionPartidaRepository.cs`
- Modify: `src/Umbral.OperacionesSesion.Infrastructure/Persistence/SesionPartidaRepository.cs`
- Modify: `tests/Umbral.OperacionesSesion.UnitTests/Application/Fakes/FakeSesionPartidaRepository.cs`
- Test: `tests/Umbral.OperacionesSesion.IntegrationTests/SesionPartidaRepositoryReconexionTests.cs`

**Interfaces:**
- Produces: `Task<SesionPartida?> ISesionPartidaRepository.GetByParticipanteActivoAsync(Guid participanteId, CancellationToken)` — devuelve la sesión con inscripción `Activa` del participante en estado `Lobby`/`Iniciada`, con grafo cargado; `null` si no existe. Consumido por Task 2 (handler) y el fake por sus unit tests.

> Ampliar la interface obliga a actualizar sus DOS implementaciones (`SesionPartidaRepository` EF + `FakeSesionPartidaRepository`) en esta misma tarea para mantener la compilación verde. El resto de archivos que referencian `ISesionPartidaRepository` son consumidores (handlers) y no cambian.

- [ ] **Step 1: Write the failing test**

```csharp
// tests/Umbral.OperacionesSesion.IntegrationTests/SesionPartidaRepositoryReconexionTests.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Infrastructure.Persistence;
using Xunit;

namespace Umbral.OperacionesSesion.IntegrationTests;

public class SesionPartidaRepositoryReconexionTests
{
    private static readonly DateTime T0 = new(2026, 6, 29, 10, 0, 0, DateTimeKind.Utc);

    private static DbContextOptions<OperacionesSesionDbContext> InMemoryOptions(string name) =>
        new DbContextOptionsBuilder<OperacionesSesionDbContext>().UseInMemoryDatabase(name).Options;

    // Sesión BDT Individual con N etapas; opcionalmente inscrita + iniciada.
    private static SesionPartida BuildSesion(Guid partidaId, bool inscribir, Guid participanteId, bool iniciar)
    {
        var etapas = new List<EtapaSnapshot> { new(Guid.NewGuid(), 1, "QR-1", 50, 3600) };
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.BusquedaDelTesoro, "Plaza", etapas);
        var snapshot = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10,
            new List<JuegoResumen> { juego });
        var sesion = SesionPartida.Publicar(partidaId, snapshot);
        if (inscribir) sesion.Inscribir(participanteId, false, 0, T0);
        if (iniciar) sesion.Iniciar(T0);
        return sesion;
    }

    [Fact]
    public async Task Devuelve_sesion_viva_con_inscripcion_activa_y_grafo_cargado()
    {
        var options = InMemoryOptions("recon-hit-" + Guid.NewGuid());
        var partidaId = Guid.NewGuid();
        var participante = Guid.NewGuid();
        await using (var ctx = new OperacionesSesionDbContext(options))
        {
            ctx.Sesiones.Add(BuildSesion(partidaId, inscribir: true, participante, iniciar: true));
            await ctx.SaveChangesAsync();
        }
        await using (var ctx = new OperacionesSesionDbContext(options))
        {
            var repo = new SesionPartidaRepository(ctx);
            var sesion = await repo.GetByParticipanteActivoAsync(participante, CancellationToken.None);
            Assert.NotNull(sesion);
            Assert.Equal(partidaId, sesion!.PartidaId);
            Assert.Single(sesion.Juegos);                       // grafo cargado vía Include
            Assert.Single(sesion.Juegos[0].Etapas);
        }
    }

    [Fact]
    public async Task Null_cuando_participante_no_tiene_inscripcion()
    {
        var options = InMemoryOptions("recon-miss-" + Guid.NewGuid());
        await using (var ctx = new OperacionesSesionDbContext(options))
        {
            ctx.Sesiones.Add(BuildSesion(Guid.NewGuid(), inscribir: false, Guid.NewGuid(), iniciar: false));
            await ctx.SaveChangesAsync();
        }
        await using (var ctx = new OperacionesSesionDbContext(options))
        {
            var repo = new SesionPartidaRepository(ctx);
            Assert.Null(await repo.GetByParticipanteActivoAsync(Guid.NewGuid(), CancellationToken.None));
        }
    }

    [Fact]
    public async Task Null_cuando_inscripcion_cancelada()
    {
        var options = InMemoryOptions("recon-cancel-" + Guid.NewGuid());
        var partidaId = Guid.NewGuid();
        var participante = Guid.NewGuid();
        var sesion = BuildSesion(partidaId, inscribir: true, participante, iniciar: false);
        sesion.CancelarInscripcion(participante);               // queda en Lobby, inscripción Cancelada
        await using (var ctx = new OperacionesSesionDbContext(options))
        {
            ctx.Sesiones.Add(sesion);
            await ctx.SaveChangesAsync();
        }
        await using (var ctx = new OperacionesSesionDbContext(options))
        {
            var repo = new SesionPartidaRepository(ctx);
            Assert.Null(await repo.GetByParticipanteActivoAsync(participante, CancellationToken.None));
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests" --filter "FullyQualifiedName~Reconexion"`
Expected: FAIL de compilación (`GetByParticipanteActivoAsync` no existe en la interface).

- [ ] **Step 3: Write minimal implementation**

En `ISesionPartidaRepository.cs`, añadir a la interface:
```csharp
    Task<SesionPartida?> GetByParticipanteActivoAsync(Guid participanteId, CancellationToken cancellationToken);
```

En `SesionPartidaRepository.cs` (EF), añadir el método (reusa el Include consolidado de `GetByPartidaIdAsync`):
```csharp
    public Task<SesionPartida?> GetByParticipanteActivoAsync(Guid participanteId, CancellationToken cancellationToken)
        => _dbContext.Sesiones
            .Include(s => s.Juegos).ThenInclude(j => j.Preguntas).ThenInclude(p => p.Opciones)
            .Include(s => s.Juegos).ThenInclude(j => j.Preguntas).ThenInclude(p => p.Respuestas)
            .Include(s => s.Juegos).ThenInclude(j => j.Etapas).ThenInclude(e => e.Tesoros)
            .Include(s => s.Inscripciones)
            .Where(s => s.Estado == EstadoSesion.Lobby || s.Estado == EstadoSesion.Iniciada)
            .OrderBy(s => s.PartidaId)
            .FirstOrDefaultAsync(
                s => s.Inscripciones.Any(i => i.ParticipanteId == participanteId && i.Estado == EstadoInscripcion.Activa),
                cancellationToken);
```

En `FakeSesionPartidaRepository.cs`, añadir (añadir `using System.Linq;` y `using Umbral.OperacionesSesion.Domain.Enums;` si faltan):
```csharp
    public Task<SesionPartida?> GetByParticipanteActivoAsync(Guid participanteId, CancellationToken cancellationToken)
        => Task.FromResult(_store.Values.FirstOrDefault(s =>
            (s.Estado == EstadoSesion.Lobby || s.Estado == EstadoSesion.Iniciada)
            && s.Inscripciones.Any(i => i.ParticipanteId == participanteId && i.EsActiva)));
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests"`
Expected: PASS (incluye los 3 nuevos; IntegrationTests 11).

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Domain/Abstractions/Persistence/ISesionPartidaRepository.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Persistence/SesionPartidaRepository.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/Fakes/FakeSesionPartidaRepository.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests/SesionPartidaRepositoryReconexionTests.cs
git commit -m "SP-3e T1: GetByParticipanteActivoAsync (repo por participante vivo + grafo)"
```

---

### Task 2: `MiSesionDto` + `ObtenerMiSesionQuery` + handler

**Files:**
- Create: `src/Umbral.OperacionesSesion.Application/DTOs/MiSesionDto.cs`
- Create: `src/Umbral.OperacionesSesion.Application/Queries/ObtenerMiSesionQuery.cs`
- Create: `src/Umbral.OperacionesSesion.Application/Handlers/Queries/ObtenerMiSesionQueryHandler.cs`
- Test: `tests/Umbral.OperacionesSesion.UnitTests/Application/ObtenerMiSesionQueryHandlerTests.cs`

**Interfaces:**
- Consumes: `ISesionPartidaRepository.GetByParticipanteActivoAsync` (Task 1); `PreguntaActualDto`, `OpcionPublicaDto`, `EtapaActualDto` (existentes).
- Produces: `MiSesionDto`, `InscripcionResumenDto`, `JuegoActivoResumenDto`; `ObtenerMiSesionQuery(Guid ParticipanteId) : IRequest<MiSesionDto?>`. Consumidos por Task 3 (controller) y Task 4 (e2e).

- [ ] **Step 1: Write the failing test**

```csharp
// tests/Umbral.OperacionesSesion.UnitTests/Application/ObtenerMiSesionQueryHandlerTests.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Handlers.Queries;
using Umbral.OperacionesSesion.Application.Queries;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class ObtenerMiSesionQueryHandlerTests
{
    private static readonly DateTime T0 = new(2026, 6, 29, 10, 0, 0, DateTimeKind.Utc);

    private static SesionPartida Trivia(Guid partidaId, Guid participante, Guid opcionOk, Guid opcionMala, bool iniciar)
    {
        var pregunta = new PreguntaSnapshot(Guid.NewGuid(), 1, "2+2?", 100, 3600,
            new[] { new OpcionSnapshot(opcionOk, "4", true), new OpcionSnapshot(opcionMala, "5", false) });
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[] { pregunta });
        var snap = new ConfiguracionSnapshot("Q", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10,
            new List<JuegoResumen> { juego });
        var s = SesionPartida.Publicar(partidaId, snap);
        s.Inscribir(participante, false, 0, T0);
        if (iniciar) s.Iniciar(T0);   // activa juego 1 + pregunta 1
        return s;
    }

    private static SesionPartida Bdt(Guid partidaId, Guid participante)
    {
        var etapa = new EtapaSnapshot(Guid.NewGuid(), 1, "QR-1", 50, 3600);
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.BusquedaDelTesoro, "Plaza", new[] { etapa });
        var snap = new ConfiguracionSnapshot("B", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10,
            new List<JuegoResumen> { juego });
        var s = SesionPartida.Publicar(partidaId, snap);
        s.Inscribir(participante, false, 0, T0);
        s.Iniciar(T0);                // activa juego 1 + etapa 1
        return s;
    }

    [Fact]
    public async Task Sin_participacion_devuelve_null()
    {
        var handler = new ObtenerMiSesionQueryHandler(new FakeSesionPartidaRepository());
        var dto = await handler.Handle(new ObtenerMiSesionQuery(Guid.NewGuid()), CancellationToken.None);
        Assert.Null(dto);
    }

    [Fact]
    public async Task Lobby_devuelve_estado_sin_juego_activo()
    {
        var repo = new FakeSesionPartidaRepository();
        var partidaId = Guid.NewGuid(); var pid = Guid.NewGuid();
        repo.Add(Trivia(partidaId, pid, Guid.NewGuid(), Guid.NewGuid(), iniciar: false)); // queda en Lobby
        var dto = await new ObtenerMiSesionQueryHandler(repo).Handle(new ObtenerMiSesionQuery(pid), CancellationToken.None);
        Assert.NotNull(dto);
        Assert.Equal("Lobby", dto!.EstadoPartida);
        Assert.Null(dto.JuegoActivo);
        Assert.Null(dto.PreguntaActual);
        Assert.Null(dto.YaRespondioPreguntaActual);
        Assert.Equal(partidaId, dto.PartidaId);
    }

    [Fact]
    public async Task Trivia_activa_sin_responder_yaRespondio_false()
    {
        var repo = new FakeSesionPartidaRepository();
        var partidaId = Guid.NewGuid(); var pid = Guid.NewGuid();
        repo.Add(Trivia(partidaId, pid, Guid.NewGuid(), Guid.NewGuid(), iniciar: true));
        var dto = await new ObtenerMiSesionQueryHandler(repo).Handle(new ObtenerMiSesionQuery(pid), CancellationToken.None);
        Assert.Equal("Iniciada", dto!.EstadoPartida);
        Assert.Equal("Trivia", dto.JuegoActivo!.TipoJuego);
        Assert.NotNull(dto.PreguntaActual);
        Assert.Equal("2+2?", dto.PreguntaActual!.Texto);
        Assert.False(dto.YaRespondioPreguntaActual);
        Assert.Null(dto.EtapaActual);
    }

    [Fact]
    public async Task Trivia_activa_respondida_incorrecto_yaRespondio_true()
    {
        var repo = new FakeSesionPartidaRepository();
        var partidaId = Guid.NewGuid(); var pid = Guid.NewGuid();
        var opcionOk = Guid.NewGuid(); var opcionMala = Guid.NewGuid();
        var sesion = Trivia(partidaId, pid, opcionOk, opcionMala, iniciar: true);
        sesion.ResponderPregunta(pid, opcionMala, T0);   // incorrecto → la pregunta sigue activa
        repo.Add(sesion);
        var dto = await new ObtenerMiSesionQueryHandler(repo).Handle(new ObtenerMiSesionQuery(pid), CancellationToken.None);
        Assert.NotNull(dto!.PreguntaActual);             // sigue activa
        Assert.True(dto.YaRespondioPreguntaActual);
    }

    [Fact]
    public async Task Bdt_activo_expone_etapa_y_yaRespondio_null()
    {
        var repo = new FakeSesionPartidaRepository();
        var partidaId = Guid.NewGuid(); var pid = Guid.NewGuid();
        repo.Add(Bdt(partidaId, pid));
        var dto = await new ObtenerMiSesionQueryHandler(repo).Handle(new ObtenerMiSesionQuery(pid), CancellationToken.None);
        Assert.Equal("BusquedaDelTesoro", dto!.JuegoActivo!.TipoJuego);
        Assert.NotNull(dto.EtapaActual);
        Assert.Equal("Plaza", dto.EtapaActual!.AreaBusqueda);
        Assert.Null(dto.PreguntaActual);
        Assert.Null(dto.YaRespondioPreguntaActual);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests" --filter "FullyQualifiedName~ObtenerMiSesion"`
Expected: FAIL de compilación (DTO/Query/Handler no existen).

- [ ] **Step 3: Write minimal implementation**

```csharp
// src/Umbral.OperacionesSesion.Application/DTOs/MiSesionDto.cs
namespace Umbral.OperacionesSesion.Application.DTOs;

public sealed record MiSesionDto(
    Guid PartidaId,
    Guid SesionPartidaId,
    string EstadoPartida,
    string Modalidad,
    InscripcionResumenDto Inscripcion,
    JuegoActivoResumenDto? JuegoActivo,
    PreguntaActualDto? PreguntaActual,
    EtapaActualDto? EtapaActual,
    bool? YaRespondioPreguntaActual);

public sealed record InscripcionResumenDto(Guid InscripcionId, string Estado);

public sealed record JuegoActivoResumenDto(Guid JuegoId, int Orden, string TipoJuego, string EstadoJuego);
```

```csharp
// src/Umbral.OperacionesSesion.Application/Queries/ObtenerMiSesionQuery.cs
using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;

namespace Umbral.OperacionesSesion.Application.Queries;

public sealed record ObtenerMiSesionQuery(Guid ParticipanteId) : IRequest<MiSesionDto?>;
```

```csharp
// src/Umbral.OperacionesSesion.Application/Handlers/Queries/ObtenerMiSesionQueryHandler.cs
using MediatR;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Queries;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;
using Umbral.OperacionesSesion.Domain.Enums;

namespace Umbral.OperacionesSesion.Application.Handlers.Queries;

public sealed class ObtenerMiSesionQueryHandler : IRequestHandler<ObtenerMiSesionQuery, MiSesionDto?>
{
    private readonly ISesionPartidaRepository _sesiones;

    public ObtenerMiSesionQueryHandler(ISesionPartidaRepository sesiones) => _sesiones = sesiones;

    public async Task<MiSesionDto?> Handle(ObtenerMiSesionQuery request, CancellationToken cancellationToken)
    {
        var sesion = await _sesiones.GetByParticipanteActivoAsync(request.ParticipanteId, cancellationToken);
        if (sesion is null) return null;

        var inscripcion = sesion.Inscripciones.First(i => i.ParticipanteId == request.ParticipanteId && i.EsActiva);
        var inscDto = new InscripcionResumenDto(inscripcion.Id.Valor, inscripcion.Estado.ToString());

        JuegoActivoResumenDto? juegoDto = null;
        PreguntaActualDto? preguntaDto = null;
        EtapaActualDto? etapaDto = null;
        bool? yaRespondio = null;

        if (sesion.Estado == EstadoSesion.Iniciada)
        {
            var juego = sesion.Juegos.FirstOrDefault(j => j.Estado == EstadoJuego.Activo);
            if (juego is not null)
            {
                juegoDto = new JuegoActivoResumenDto(
                    juego.JuegoId, juego.Orden, juego.TipoJuego.ToString(), juego.Estado.ToString());

                if (juego.TipoJuego == TipoJuego.Trivia && juego.PreguntaActiva is { } preg)
                {
                    preguntaDto = new PreguntaActualDto(
                        sesion.PartidaId, juego.JuegoId, preg.PreguntaId, preg.Orden, preg.Texto,
                        preg.TiempoLimiteSegundos, preg.FechaActivacion!.Value,
                        preg.Opciones.Select(o => new OpcionPublicaDto(o.OpcionId, o.Texto)).ToList());
                    yaRespondio = preg.Respuestas.Any(r => r.ParticipanteId == request.ParticipanteId);
                }
                else if (juego.TipoJuego == TipoJuego.BusquedaDelTesoro && juego.EtapaActiva is { } et)
                {
                    etapaDto = new EtapaActualDto(
                        sesion.PartidaId, juego.JuegoId, et.EtapaId, et.Orden, juego.AreaBusqueda,
                        et.TiempoLimiteSegundos, et.FechaActivacion!.Value);
                }
            }
        }

        return new MiSesionDto(
            sesion.PartidaId, sesion.Id.Valor, sesion.Estado.ToString(), sesion.Modalidad.ToString(),
            inscDto, juegoDto, preguntaDto, etapaDto, yaRespondio);
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests"`
Expected: PASS (UnitTests 177; +5 nuevos).

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/DTOs/MiSesionDto.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Queries/ObtenerMiSesionQuery.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Handlers/Queries/ObtenerMiSesionQueryHandler.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/ObtenerMiSesionQueryHandlerTests.cs
git commit -m "SP-3e T2: MiSesionDto + ObtenerMiSesion query/handler (proyección estado vigente)"
```

---

### Task 3: Controller `GET /mi-sesion` (200/204)

**Files:**
- Modify: `src/Umbral.OperacionesSesion.Api/Controllers/SesionesController.cs`
- Test: `tests/Umbral.OperacionesSesion.UnitTests/Api/SesionesControllerReconexionTests.cs`

**Interfaces:**
- Consumes: `ObtenerMiSesionQuery`, `MiSesionDto` (Task 2); `ObtenerParticipanteId()` (existente, claim `sub`).
- Produces: endpoint REST `GET mi-sesion` → `200 + MiSesionDto` / `204`.

> Reusa el `FakeSender` del proyecto y el helper que monta el controller con un `ClaimsPrincipal` (`sub`) — el mismo patrón de `SesionesControllerTriviaTests`/`SesionesControllerBdtTests` (SP-3c/3d). Revisar esos archivos para el nombre exacto del helper (`WithUser`/`ControllerConSub`) y del `FakeSender`.

- [ ] **Step 1: Write the failing test**

```csharp
// tests/Umbral.OperacionesSesion.UnitTests/Api/SesionesControllerReconexionTests.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Queries;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Api;

public class SesionesControllerReconexionTests
{
    [Fact]
    public async Task Mi_sesion_con_participacion_devuelve_200_y_dto()
    {
        var partidaId = Guid.NewGuid();
        var sub = Guid.NewGuid();
        var dto = new MiSesionDto(partidaId, Guid.NewGuid(), "Iniciada", "Individual",
            new InscripcionResumenDto(Guid.NewGuid(), "Activa"), null, null, null, null);
        var sender = new FakeSender(dto);
        var controller = ControllerConSub(sender, sub);

        var result = await controller.ObtenerMiSesion(default);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<MiSesionDto>(ok.Value);
        var query = Assert.IsType<ObtenerMiSesionQuery>(sender.LastRequest);
        Assert.Equal(sub, query.ParticipanteId);     // participanteId tomado del claim sub
    }

    [Fact]
    public async Task Mi_sesion_sin_participacion_devuelve_204()
    {
        var sender = new FakeSender(null);           // handler devuelve null
        var controller = ControllerConSub(sender, Guid.NewGuid());
        var result = await controller.ObtenerMiSesion(default);
        Assert.IsType<NoContentResult>(result);
    }

    // ControllerConSub / FakeSender: reusar exactamente los helpers de SesionesControllerTriviaTests (3c).
}
```

> `FakeSender(null)` debe poder devolver `null` para una respuesta `MiSesionDto?`. Si el `FakeSender` existente exige un valor no nulo, ajustar su campo a `object?` (cambio mínimo, retrocompatible) — verificarlo al leer el helper.

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests" --filter "FullyQualifiedName~Reconexion&FullyQualifiedName~Api"`
Expected: FAIL (endpoint `ObtenerMiSesion` no existe).

- [ ] **Step 3: Write minimal implementation**

En `SesionesController` añadir (tras `ObtenerEtapaActual`, antes de `ObtenerParticipanteId`):
```csharp
    [HttpGet("mi-sesion")]
    public async Task<IActionResult> ObtenerMiSesion(CancellationToken cancellationToken)
    {
        var participanteId = ObtenerParticipanteId();
        var dto = await _mediator.Send(new ObtenerMiSesionQuery(participanteId), cancellationToken);
        return dto is null ? NoContent() : Ok(dto);
    }
```
> Si falta el `using` de `Umbral.OperacionesSesion.Application.Queries`, ya está presente (lo usan los GET existentes). No añadir middleware: `204` no es excepción.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests"`
Expected: PASS (UnitTests 179; +2 nuevos).

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Controllers/SesionesController.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/SesionesControllerReconexionTests.cs
git commit -m "SP-3e T3: endpoint GET mi-sesion (200 dto / 204 sin participación)"
```

---

### Task 4: Contract e2e (WebApplicationFactory) + no-leak

**Files:**
- Test: `tests/Umbral.OperacionesSesion.ContractTests/ReconexionEndpointsTests.cs`

**Interfaces:**
- Consumes: pipeline HTTP real vía `OperacionesSesionWebFactory` (`Stub`, `CreateClientAs`); `MiSesionDto`, `ConfiguracionPartidaDto` y los config DTOs (existentes).

> Gate de aceptación e2e, TEST-ONLY (producción intacta). Reusa el harness de `BdtRuntimeEndpointsTests.cs` (flujo publicar→inscribir→iniciar, `_factory.Stub.Respuestas[partidaId]`, `CreateClientAs`) y, para construir una config **Trivia con preguntas**, el patrón de `TriviaRuntimeEndpointsTests.cs` (helper `BuildTriviaConfig` o equivalente — leer ese archivo para la forma exacta de `JuegoResumenDto`/`TriviaConfigDto`/`PreguntaConfigDto`). Cada test usa `partidaId` y `jugador` frescos.

- [ ] **Step 1: Write the failing test**

```csharp
// tests/Umbral.OperacionesSesion.ContractTests/ReconexionEndpointsTests.cs
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.DTOs;
using Xunit;

namespace Umbral.OperacionesSesion.ContractTests;

public class ReconexionEndpointsTests : IClassFixture<OperacionesSesionWebFactory>
{
    private readonly OperacionesSesionWebFactory _factory;
    private readonly HttpClient _operador;

    public ReconexionEndpointsTests(OperacionesSesionWebFactory factory)
    {
        _factory = factory;
        _operador = factory.CreateClient();
    }

    [Fact]
    public async Task Sin_participacion_devuelve_204()
    {
        var cliente = _factory.CreateClientAs(Guid.NewGuid());
        var resp = await cliente.GetAsync("/mi-sesion");
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
    }

    [Fact]
    public async Task Reconexion_en_lobby_devuelve_estado_lobby()
    {
        var partidaId = Guid.NewGuid();
        var jugador = Guid.NewGuid();
        var jugadorClient = _factory.CreateClientAs(jugador);

        Assert.Equal(HttpStatusCode.Created,
            (await _operador.PostAsync($"/partidas/{partidaId}/publicacion", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Created,
            (await jugadorClient.PostAsync($"/partidas/{partidaId}/inscripciones", null)).StatusCode);
        // NO iniciar → sigue en Lobby

        var dto = await jugadorClient.GetFromJsonAsync<MiSesionDto>("/mi-sesion");
        Assert.Equal(partidaId, dto!.PartidaId);
        Assert.Equal("Lobby", dto.EstadoPartida);
        Assert.Null(dto.JuegoActivo);
    }

    [Fact]
    public async Task Reconexion_recupera_etapa_bdt_activa_sin_filtrar_qr()
    {
        var partidaId = Guid.NewGuid();
        var jugador = Guid.NewGuid();
        // Config BDT: mismo patrón que BdtRuntimeEndpointsTests.BuildBdtConfig
        _factory.Stub.Respuestas[partidaId] = BdtConfig("QR-SECRETO", 50);
        var jugadorClient = _factory.CreateClientAs(jugador);

        await _operador.PostAsync($"/partidas/{partidaId}/publicacion", null);
        await jugadorClient.PostAsync($"/partidas/{partidaId}/inscripciones", null);
        await _operador.PostAsync($"/partidas/{partidaId}/inicio", null);

        var resp = await jugadorClient.GetAsync("/mi-sesion");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var dto = await resp.Content.ReadFromJsonAsync<MiSesionDto>();
        Assert.Equal("Iniciada", dto!.EstadoPartida);
        Assert.Equal("BusquedaDelTesoro", dto.JuegoActivo!.TipoJuego);
        Assert.Equal(1, dto.EtapaActual!.Orden);

        // NO-LEAK: el cuerpo crudo no contiene el QR esperado
        var raw = await resp.Content.ReadAsStringAsync();
        Assert.DoesNotContain("QR-SECRETO", raw);
        Assert.DoesNotContain("codigoQR", raw, StringComparison.OrdinalIgnoreCase);
    }

    // Helper local: config BDT Individual de 1 etapa (forma de ConfiguracionPartidaDto idéntica
    // a BdtRuntimeEndpointsTests.BuildBdtConfig — replicar esa estructura exacta).
    private static ConfiguracionPartidaDto BdtConfig(string qr, int puntaje)
    {
        var juego = new JuegoResumenDto(Guid.NewGuid(), 1, "BusquedaDelTesoro",
            Trivia: null,
            Bdt: new BdtConfigDto("Plaza central",
                new System.Collections.Generic.List<EtapaConfigDto>
                {
                    new(Guid.NewGuid(), 1, qr, puntaje, 3600)
                }));
        return new ConfiguracionPartidaDto("Copa", "Individual", "Manual", null, 1, 10,
            new System.Collections.Generic.List<JuegoResumenDto> { juego });
    }
}
```

> Añadir, si el harness lo soporta y el patrón de `TriviaRuntimeEndpointsTests` está disponible, un cuarto test que inicie una partida **Trivia** y verifique que `/mi-sesion` recupera `preguntaActual` con `yaRespondioPreguntaActual=false`, y que el cuerpo crudo no contiene `esCorrecta:true` ni el texto de la opción correcta. Si construir la config Trivia con preguntas resulta más costoso que el resto de la tarea, dejar registrado el gap en el commit (no inventar una forma de DTO que no exista).

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests" --filter "FullyQualifiedName~Reconexion"`
Expected: FAIL (404 en `/mi-sesion` hasta que Task 3 esté integrada — al ejecutar tras Task 3, falla solo si hay regresión; si Task 3 ya está, este paso confirma comportamiento e2e).

- [ ] **Step 3: Write minimal implementation**

Sin código de producción (las Tasks 1–3 ya lo aportan). Si algún test falla, corregir el test (harness/forma de DTO), no la producción.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests"`
Expected: PASS (ContractTests 31; +3, o +4 si se añade el de Trivia).

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/ReconexionEndpointsTests.cs
git commit -m "SP-3e T4: contract tests e2e de reconexión (lobby/bdt + no-leak + 204)"
```

---

### Task 5: Contratos HTTP + traceability

**Files:**
- Modify: `contracts/http/operaciones-sesion-api.md`
- Modify (escribir, NO commitear): `docs/04-sdd/traceability-matrix.md`

**Interfaces:** documentación; sin código.

> CARVE-OUT GIT (vigente, = SP-3c T16 / SP-3d T17): escribir la fila SP-3e en `traceability-matrix.md` pero **NO** stagearla/commitearla. El commit lleva SOLO `contracts/http/operaciones-sesion-api.md`.

- [ ] **Step 1: HTTP contract**

En `contracts/http/operaciones-sesion-api.md`, añadir al Endpoint Registry (verbatim):
```
| Mi sesión (reconexión) | GET | `/operaciones-sesion/mi-sesion` | Participante | 200 + MiSesionDto · 204 sin participación activa | 401 sin identidad |
```
A la lista de DTOs:
```
- `MiSesionDto { partidaId, sesionPartidaId, estadoPartida, modalidad, inscripcion{ inscripcionId, estado }, juegoActivo?{ juegoId, orden, tipoJuego, estadoJuego }, preguntaActual?, etapaActual?, yaRespondioPreguntaActual? }` (participant-safe; reusa PreguntaActualDto/EtapaActualDto; nunca `codigoQREsperado` ni la opción correcta)
```
A las Notes (al final del párrafo existente):
```
`GET /mi-sesion` direcciona por participante (JWT `sub`, sin `partidaId`): devuelve la única participación activa vigente (partida en Lobby/Iniciada) o `204` si no hay. `estadoPartida` en el cuerpo solo toma Lobby/Iniciada. `yaRespondioPreguntaActual` es true/false solo con pregunta Trivia activa, null en BDT/lobby. Read-only; no emite eventos.
```

- [ ] **Step 2: Traceability (escribir, NO commitear)**

Añadir fila a `docs/04-sdd/traceability-matrix.md`:
```
| Reconexión / estado vigente (SP-3e) | Agregador GET /mi-sesion (por participante, JWT sub): snapshot único de participación + estado partida + juego activo + sub-estado vigente (pregunta/etapa activa, DTOs participant-safe reusados) + yaRespondioPreguntaActual; read-only CQRS, sin eventos; cubre Lobby+Iniciada, terminal/sin-participación → 204; Individual-only | Operaciones de Sesión | — (Puntuaciones SP-4; push SignalR SP-3f) | docs/superpowers/specs/2026-06-29-sp3e-reconexion-design.md · docs/superpowers/plans/2026-06-29-sp3e-reconexion.md | contracts/http/operaciones-sesion-api.md | Implemented — suite verde. **Fuente:** RF-14/RB-33. **Diferido:** Equipo/convocatoria→slice-E, push tiempo real→SP-3f, scoring→SP-4. |
```

- [ ] **Step 3: Run the FULL suite**

Run: `dotnet test "services/operaciones-sesion"`
Expected: PASS (docs-only; confirma que nada se rompió). UnitTests 179 · IntegrationTests 11 · ContractTests 31.

- [ ] **Step 4: Commit (solo el contrato HTTP)**

```bash
git add contracts/http/operaciones-sesion-api.md
git commit -m "SP-3e T5: contrato HTTP de mi-sesion (fila traceability SP-3e escrita, sin commitear)"
```
> traceability-matrix.md queda modificado+unstaged, uniéndose al squash pendiente del usuario.

> === TODAS las tareas SP-3e completas. Next: review final whole-branch (opus) sobre el rango SP-3e, luego finishing-a-development-branch (decide el usuario). ===

---

## Self-Review (autor del plan)

**1. Cobertura del spec (§ por §):**
- §3 contrato (GET /mi-sesion, 200/204) → T3 (endpoint), T5 (doc).
- §4 DTO (MiSesionDto + sub-DTOs, yaRespondioPreguntaActual) → T2.
- §5 repo `GetByParticipanteActivoAsync` (vivo + grafo) → T1; proyección progreso propio → T2 (handler, sin predicados nuevos por hallazgo del dominio).
- §6 Application (query + handler, null→null) → T2.
- §7 matriz de estados (6 filas) → T2 (unit: sin participación/lobby/trivia-sin-responder/trivia-respondida/bdt) + T4 (e2e: 204/lobby/bdt). Fila "juego activo sin sub-estado" cubierta por la lógica `is { }` (rama null) — sin test dedicado (estado transitorio raro); aceptable.
- §8 Api (endpoint thin, sin middleware) → T3.
- §9 no-leak → T4 (aserción de cuerpo crudo).
- §10 testing (repo/handler/controller/e2e) → T1/T2/T3/T4.
- §11 fronteras → respetado por construcción (Individual-only; sin eventos).

**2. Placeholder scan:** sin TBD/TODO. Todo step de código lleva código real. Dos reusos explícitos de harness de test existente (helper de controller en T3; config Trivia/BDT en T4) están señalados con el archivo exacto a leer — son reuso de infraestructura, no lógica pendiente. El cuarto test de Trivia en T4 es opcional con instrucción de registrar el gap si la forma del DTO encarece — no es un placeholder de producción.

**3. Consistencia de tipos:** `GetByParticipanteActivoAsync(Guid, CancellationToken) → SesionPartida?` idéntico en interface/EF/fake/handler (T1→T2). `MiSesionDto` (9 campos) idéntico en T2/T3/T4. `ObtenerMiSesionQuery(Guid ParticipanteId)` idéntico T2/T3. IDs vía `.Valor`. Enums vía `.ToString()`. Sub-DTOs reusados (`PreguntaActualDto` 8-args con `IReadOnlyList<OpcionPublicaDto>`, `EtapaActualDto` 7-args con `AreaBusqueda`) coinciden con las definiciones existentes verificadas.

**Nota de orden T3→T4:** el e2e de T4 asume el endpoint de T3 integrado; si se ejecuta en orden, T4 corre verde de una. El "fail" de su Step 2 solo aplica si se escribe T4 antes que T3.
