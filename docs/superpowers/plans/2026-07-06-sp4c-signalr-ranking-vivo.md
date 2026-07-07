# SP-4c — Ranking en vivo por SignalR + hardening — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Difundir por SignalR (vía gateway) el ranking nativo recalculado tras cada evento de scoring proyectado y el ranking consolidado al finalizar la partida, y saldar la deuda de hardening `[Authorize]` del servicio Puntuaciones.

**Architecture:** Enfoque A del spec (`docs/superpowers/specs/2026-07-06-sp4c-signalr-ranking-vivo-design.md`): un `RankingHub` pasivo (solo membresía de grupos), un port `IRankingRealtimePublisher` (Application) implementado sobre `IHubContext` (Api/Realtime), y un `RankingBroadcastDispatcher` best-effort invocado por el pipeline del worker **después** de cada proyección exitosa. Los payloads reusan los response DTOs HTTP de SP-4a/4b tal cual.

**Tech Stack:** .NET 8, ASP.NET Core SignalR (framework), MediatR, xUnit (fakes a mano, sin Moq), `Microsoft.AspNetCore.SignalR.Client` solo en IntegrationTests.

## Global Constraints

- Rama: `feature/sp-4c-signalr-ranking` (ya creada desde `feature/sp-4b-consolidado`; la serie SP-4 no se integra a develop).
- Estructura graduada CLAUDE.md: ports en `Application/Interfaces/`, sin lógica de negocio en hubs ni controllers, controllers heredan `ControllerBase` y despachan por `ISender`, todo controller con unit tests.
- **Sin cambios** en: gateway (`gateway/src/Umbral.Gateway/appsettings.json`), esquema de BD (sin migraciones), cola/bindings RabbitMQ (`RabbitMqConsumerOptions` intacto), shapes de los DTOs HTTP existentes.
- Nombres de mensaje SignalR exactos (contrato): `RankingTriviaActualizado`, `RankingBDTActualizado`, `RankingConsolidadoCalculado`. Grupo: `puntuaciones-partida-{partidaId}`. Ruta del hub: `puntuaciones/hubs/ranking`.
- Un fallo de difusión **nunca** afecta la proyección ni el ack del worker (ADR-0012 best-effort).
- Comandos de test: `dotnet test "services/puntuaciones/Umbral.Puntuaciones.sln"` (suite completa; base actual **96/96**: 74 unit + 15 integration + 7 contract). Los tests opt-in de RabbitMQ solo corren con Docker disponible.
- Vocabulario en español del dominio (partida, juego, marcador, ranking); comentarios de código en español, estilo del servicio.

---

### Task 1: `RankingRealtimeMessages` + `RankingHub` + mapeo en `Program.cs`

**Files:**
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Api/Realtime/RankingRealtimeMessages.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Api/Realtime/RankingHub.cs`
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Api/Program.cs`
- Test: `services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Api/Realtime/RankingHubTests.cs`
- Test (fakes): `services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Api/Realtime/SignalRFakes.cs`

**Interfaces:**
- Consumes: `IProyeccionesRepository.GetPartidaAsync(Guid, CancellationToken)` (Domain, existente); `PartidaProyectada.DesdePublicacion(partidaId, sesionPartidaId, Modalidad)` para sembrar fakes; `FakeProyeccionesRepository` existente en `UnitTests/Application/Fakes/`.
- Produces: `RankingRealtimeMessages.GrupoPartida(Guid)` → `"puntuaciones-partida-{id}"`; constantes `RankingTriviaActualizado`, `RankingBDTActualizado`, `RankingConsolidadoCalculado`; `RankingHub` con métodos `SuscribirAPartida(Guid)` / `DesuscribirDePartida(Guid)`; hub mapeado en `puntuaciones/hubs/ranking`; SignalR registrado con enums-como-string (paridad con el JSON HTTP). Los fakes `FakeGroupManager` y `FakeHubCallerContext` los reusan las Tasks 2 y 6.

- [ ] **Step 1: Escribir los tests que fallan**

`services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Api/Realtime/SignalRFakes.cs`:

```csharp
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;

namespace Umbral.Puntuaciones.UnitTests.Api.Realtime;

public sealed class FakeGroupManager : IGroupManager
{
    public List<(string ConnectionId, string Group)> Added { get; } = new();
    public List<(string ConnectionId, string Group)> Removed { get; } = new();

    public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
    {
        Added.Add((connectionId, groupName));
        return Task.CompletedTask;
    }

    public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
    {
        Removed.Add((connectionId, groupName));
        return Task.CompletedTask;
    }
}

public sealed class FakeHubCallerContext : HubCallerContext
{
    public FakeHubCallerContext(string connectionId) => ConnectionId = connectionId;

    public override string ConnectionId { get; }
    public override string? UserIdentifier => null;
    public override System.Security.Claims.ClaimsPrincipal? User => null;
    public override IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();
    public override IFeatureCollection Features { get; } = new FeatureCollection();
    public override CancellationToken ConnectionAborted => CancellationToken.None;
    public override void Abort() { }
}
```

`services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Api/Realtime/RankingHubTests.cs`:

```csharp
using Microsoft.AspNetCore.SignalR;
using Umbral.Puntuaciones.Api.Realtime;
using Umbral.Puntuaciones.Domain.Entities;
using Umbral.Puntuaciones.Domain.Enums;
using Umbral.Puntuaciones.UnitTests.Application.Fakes;

namespace Umbral.Puntuaciones.UnitTests.Api.Realtime;

public class RankingHubTests
{
    private static (RankingHub Hub, FakeGroupManager Groups, FakeProyeccionesRepository Repo) Construir(string connId = "c1")
    {
        var repo = new FakeProyeccionesRepository();
        var groups = new FakeGroupManager();
        var hub = new RankingHub(repo)
        {
            Context = new FakeHubCallerContext(connId),
            Groups = groups
        };
        return (hub, groups, repo);
    }

    [Fact]
    public async Task Suscribir_con_partida_proyectada_une_al_grupo()
    {
        var (hub, groups, repo) = Construir();
        var partidaId = Guid.NewGuid();
        repo.AddPartida(PartidaProyectada.DesdePublicacion(partidaId, Guid.NewGuid(), Modalidad.Individual));

        await hub.SuscribirAPartida(partidaId);

        Assert.Contains(("c1", RankingRealtimeMessages.GrupoPartida(partidaId)), groups.Added);
    }

    [Fact]
    public async Task Suscribir_a_partida_desconocida_lanza_HubException_y_no_une()
    {
        var (hub, groups, _) = Construir();

        await Assert.ThrowsAsync<HubException>(() => hub.SuscribirAPartida(Guid.NewGuid()));

        Assert.Empty(groups.Added);
    }

    [Fact]
    public async Task Desuscribir_remueve_del_grupo()
    {
        var (hub, groups, _) = Construir();
        var partidaId = Guid.NewGuid();

        await hub.DesuscribirDePartida(partidaId);

        Assert.Contains(("c1", RankingRealtimeMessages.GrupoPartida(partidaId)), groups.Removed);
    }
}
```

- [ ] **Step 2: Verificar que fallan (no compilan: `RankingHub` no existe)**

Run: `dotnet test "services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Umbral.Puntuaciones.UnitTests.csproj" --filter "FullyQualifiedName~RankingHubTests"`
Expected: FAIL de compilación — `RankingHub`/`RankingRealtimeMessages` no definidos.

- [ ] **Step 3: Implementación mínima**

`services/puntuaciones/src/Umbral.Puntuaciones.Api/Realtime/RankingRealtimeMessages.cs`:

```csharp
namespace Umbral.Puntuaciones.Api.Realtime;

public static class RankingRealtimeMessages
{
    public const string RankingTriviaActualizado = nameof(RankingTriviaActualizado);
    public const string RankingBDTActualizado = nameof(RankingBDTActualizado);
    public const string RankingConsolidadoCalculado = nameof(RankingConsolidadoCalculado);

    public static string GrupoPartida(Guid partidaId) => $"puntuaciones-partida-{partidaId}";
}
```

`services/puntuaciones/src/Umbral.Puntuaciones.Api/Realtime/RankingHub.cs`:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Umbral.Puntuaciones.Domain.Abstractions.Persistence;

namespace Umbral.Puntuaciones.Api.Realtime;

// Hub de ranking en vivo (SP-4c). Solo membresía de grupos: el repositorio se usa únicamente para
// validar que la partida exista en las proyecciones (paridad con ADR-0011); sin lógica de negocio.
// Lectura para cualquier rol autenticado, misma postura que los endpoints HTTP.
[Authorize]
public sealed class RankingHub : Hub
{
    private readonly IProyeccionesRepository _repo;

    public RankingHub(IProyeccionesRepository repo) => _repo = repo;

    public async Task SuscribirAPartida(Guid partidaId)
    {
        var partida = await _repo.GetPartidaAsync(partidaId, Context.ConnectionAborted);
        if (partida is null)
        {
            throw new HubException("Partida no proyectada.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, RankingRealtimeMessages.GrupoPartida(partidaId), Context.ConnectionAborted);
    }

    public Task DesuscribirDePartida(Guid partidaId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, RankingRealtimeMessages.GrupoPartida(partidaId), Context.ConnectionAborted);
}
```

En `Program.cs`, tras el bloque `builder.Services.AddControllers()...` añadir (los enums viajan como string, paridad con el JSON de los controllers):

```csharp
builder.Services.AddSignalR().AddJsonProtocol(options =>
    options.PayloadSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
```

Y tras `app.MapControllers();`:

```csharp
app.MapHub<Umbral.Puntuaciones.Api.Realtime.RankingHub>("puntuaciones/hubs/ranking");
```

- [ ] **Step 4: Verificar que pasan**

Run: `dotnet test "services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Umbral.Puntuaciones.UnitTests.csproj" --filter "FullyQualifiedName~RankingHubTests"`
Expected: PASS (3/3).

- [ ] **Step 5: Regresión rápida del proyecto de unit tests + commit**

Run: `dotnet test "services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Umbral.Puntuaciones.UnitTests.csproj"`
Expected: PASS (77 = 74 + 3).

```bash
git add services/puntuaciones
git commit -m "feat(puntuaciones): RankingHub con suscripcion por partida proyectada (SP-4c)"
```

---

### Task 2: Port `IRankingRealtimePublisher` + `SignalRRankingRealtimePublisher`

**Files:**
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Application/Interfaces/IRankingRealtimePublisher.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Api/Realtime/SignalRRankingRealtimePublisher.cs`
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Api/Program.cs` (registro DI)
- Test: `services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Api/Realtime/SignalRRankingRealtimePublisherTests.cs`

**Interfaces:**
- Consumes: `RankingJuegoResponse(Guid JuegoId, TipoJuego TipoJuego, DateTime GeneradoEn, IReadOnlyList<EntradaRankingDto> Entradas)` y `RankingConsolidadoResponse(Guid PartidaId, DateTime GeneradoEn, IReadOnlyList<EntradaRankingConsolidadoDto> Entradas)` (DTOs existentes); `RankingRealtimeMessages` y `FakeGroupManager` (Task 1).
- Produces: interfaz consumida por Task 3:

```csharp
public interface IRankingRealtimePublisher
{
    Task PublicarRankingTriviaActualizadoAsync(Guid partidaId, RankingJuegoResponse ranking, CancellationToken cancellationToken);
    Task PublicarRankingBdtActualizadoAsync(Guid partidaId, RankingJuegoResponse ranking, CancellationToken cancellationToken);
    Task PublicarRankingConsolidadoCalculadoAsync(RankingConsolidadoResponse ranking, CancellationToken cancellationToken);
}
```

(`partidaId` viaja como parámetro en los dos primeros porque `RankingJuegoResponse` no lo lleva; el consolidado lo trae en el DTO.)

- [ ] **Step 1: Escribir los tests que fallan**

`services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Api/Realtime/SignalRRankingRealtimePublisherTests.cs`:

```csharp
using Microsoft.AspNetCore.SignalR;
using Umbral.Puntuaciones.Api.Realtime;
using Umbral.Puntuaciones.Application.DTOs;
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.UnitTests.Api.Realtime;

public sealed class FakeClientProxy : IClientProxy
{
    public List<(string Method, object?[] Args)> Sent { get; } = new();

    public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
    {
        Sent.Add((method, args));
        return Task.CompletedTask;
    }
}

public sealed class FakeHubClients : IHubClients
{
    public FakeClientProxy Proxy { get; } = new();
    public string? GrupoSolicitado { get; private set; }

    public IClientProxy Group(string groupName)
    {
        GrupoSolicitado = groupName;
        return Proxy;
    }

    public IClientProxy All => Proxy;
    public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => Proxy;
    public IClientProxy Client(string connectionId) => Proxy;
    public IClientProxy Clients(IReadOnlyList<string> connectionIds) => Proxy;
    public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => Proxy;
    public IClientProxy Groups(IReadOnlyList<string> groupNames) => Proxy;
    public IClientProxy User(string userId) => Proxy;
    public IClientProxy Users(IReadOnlyList<string> userIds) => Proxy;
}

public sealed class FakeHubContext : IHubContext<RankingHub>
{
    public FakeHubClients FakeClients { get; } = new();
    public IHubClients Clients => FakeClients;
    public IGroupManager Groups { get; } = new FakeGroupManager();
}

public class SignalRRankingRealtimePublisherTests
{
    private static RankingJuegoResponse Ranking(Guid juegoId) =>
        new(juegoId, TipoJuego.Trivia, DateTime.UtcNow,
            new[] { new EntradaRankingDto(1, Guid.NewGuid(), TipoCompetidor.Participante, 10, 1500, 1) });

    [Fact]
    public async Task Trivia_envia_al_grupo_de_la_partida_con_mensaje_y_payload()
    {
        var hub = new FakeHubContext();
        var publisher = new SignalRRankingRealtimePublisher(hub);
        var partidaId = Guid.NewGuid();
        var ranking = Ranking(Guid.NewGuid());

        await publisher.PublicarRankingTriviaActualizadoAsync(partidaId, ranking, CancellationToken.None);

        Assert.Equal(RankingRealtimeMessages.GrupoPartida(partidaId), hub.FakeClients.GrupoSolicitado);
        var (method, args) = Assert.Single(hub.FakeClients.Proxy.Sent);
        Assert.Equal(RankingRealtimeMessages.RankingTriviaActualizado, method);
        Assert.Same(ranking, Assert.Single(args));
    }

    [Fact]
    public async Task Bdt_envia_el_mensaje_RankingBDTActualizado()
    {
        var hub = new FakeHubContext();
        var publisher = new SignalRRankingRealtimePublisher(hub);
        var partidaId = Guid.NewGuid();

        await publisher.PublicarRankingBdtActualizadoAsync(partidaId, Ranking(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(RankingRealtimeMessages.GrupoPartida(partidaId), hub.FakeClients.GrupoSolicitado);
        Assert.Equal(RankingRealtimeMessages.RankingBDTActualizado, hub.FakeClients.Proxy.Sent.Single().Method);
    }

    [Fact]
    public async Task Consolidado_usa_el_partidaId_del_payload()
    {
        var hub = new FakeHubContext();
        var publisher = new SignalRRankingRealtimePublisher(hub);
        var partidaId = Guid.NewGuid();
        var consolidado = new RankingConsolidadoResponse(partidaId, DateTime.UtcNow,
            Array.Empty<EntradaRankingConsolidadoDto>());

        await publisher.PublicarRankingConsolidadoCalculadoAsync(consolidado, CancellationToken.None);

        Assert.Equal(RankingRealtimeMessages.GrupoPartida(partidaId), hub.FakeClients.GrupoSolicitado);
        Assert.Equal(RankingRealtimeMessages.RankingConsolidadoCalculado, hub.FakeClients.Proxy.Sent.Single().Method);
    }
}
```

- [ ] **Step 2: Verificar que fallan (no compilan)**

Run: `dotnet test "services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Umbral.Puntuaciones.UnitTests.csproj" --filter "FullyQualifiedName~SignalRRankingRealtimePublisherTests"`
Expected: FAIL de compilación — `IRankingRealtimePublisher`/`SignalRRankingRealtimePublisher` no definidos.

- [ ] **Step 3: Implementación mínima**

`services/puntuaciones/src/Umbral.Puntuaciones.Application/Interfaces/IRankingRealtimePublisher.cs`:

```csharp
using Umbral.Puntuaciones.Application.DTOs;

namespace Umbral.Puntuaciones.Application.Interfaces;

// Port de difusión de rankings en vivo (SP-4c). La implementación vive en Api (SignalR).
public interface IRankingRealtimePublisher
{
    Task PublicarRankingTriviaActualizadoAsync(Guid partidaId, RankingJuegoResponse ranking, CancellationToken cancellationToken);
    Task PublicarRankingBdtActualizadoAsync(Guid partidaId, RankingJuegoResponse ranking, CancellationToken cancellationToken);
    Task PublicarRankingConsolidadoCalculadoAsync(RankingConsolidadoResponse ranking, CancellationToken cancellationToken);
}
```

`services/puntuaciones/src/Umbral.Puntuaciones.Api/Realtime/SignalRRankingRealtimePublisher.cs`:

```csharp
using Microsoft.AspNetCore.SignalR;
using Umbral.Puntuaciones.Application.DTOs;
using Umbral.Puntuaciones.Application.Interfaces;

namespace Umbral.Puntuaciones.Api.Realtime;

public sealed class SignalRRankingRealtimePublisher : IRankingRealtimePublisher
{
    private readonly IHubContext<RankingHub> _hub;

    public SignalRRankingRealtimePublisher(IHubContext<RankingHub> hub) => _hub = hub;

    private Task Difundir(Guid partidaId, string mensaje, object payload, CancellationToken ct) =>
        _hub.Clients.Group(RankingRealtimeMessages.GrupoPartida(partidaId)).SendAsync(mensaje, payload, ct);

    public Task PublicarRankingTriviaActualizadoAsync(Guid partidaId, RankingJuegoResponse ranking, CancellationToken cancellationToken) =>
        Difundir(partidaId, RankingRealtimeMessages.RankingTriviaActualizado, ranking, cancellationToken);

    public Task PublicarRankingBdtActualizadoAsync(Guid partidaId, RankingJuegoResponse ranking, CancellationToken cancellationToken) =>
        Difundir(partidaId, RankingRealtimeMessages.RankingBDTActualizado, ranking, cancellationToken);

    public Task PublicarRankingConsolidadoCalculadoAsync(RankingConsolidadoResponse ranking, CancellationToken cancellationToken) =>
        Difundir(ranking.PartidaId, RankingRealtimeMessages.RankingConsolidadoCalculado, ranking, cancellationToken);
}
```

En `Program.cs`, junto al `AddSignalR()` de Task 1:

```csharp
builder.Services.AddSingleton<Umbral.Puntuaciones.Application.Interfaces.IRankingRealtimePublisher,
    Umbral.Puntuaciones.Api.Realtime.SignalRRankingRealtimePublisher>();
```

- [ ] **Step 4: Verificar que pasan**

Run: `dotnet test "services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Umbral.Puntuaciones.UnitTests.csproj" --filter "FullyQualifiedName~SignalRRankingRealtimePublisherTests"`
Expected: PASS (3/3).

- [ ] **Step 5: Commit**

```bash
git add services/puntuaciones
git commit -m "feat(puntuaciones): port y publisher SignalR de rankings en vivo (SP-4c)"
```

---

### Task 3: `RankingBroadcastDispatcher` (best-effort, traga excepciones)

**Files:**
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Api/Workers/RankingBroadcastDispatcher.cs`
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Api/Program.cs` (registro DI)
- Test: `services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Workers/RankingBroadcastDispatcherTests.cs`

**Interfaces:**
- Consumes: `IRankingRealtimePublisher` (Task 2); queries existentes `ObtenerRankingJuegoQuery(Guid PartidaId, Guid JuegoId) : IRequest<RankingJuegoResponse>` y `ObtenerRankingConsolidadoQuery(Guid PartidaId) : IRequest<RankingConsolidadoResponse>`; comandos existentes `ProyectarPuntajeTriviaCommand`, `ProyectarEtapaBdtGanadaCommand`, `ProyectarPartidaFinalizadaCommand` (todos llevan `PartidaId`; los dos primeros llevan `JuegoId`); `FakeSender` existente (`UnitTests/Api/FakeSender.cs`, namespace `Umbral.Puntuaciones.UnitTests.Api`).
- Produces: `RankingBroadcastDispatcher.DifundirAsync(object comandoProyectado, CancellationToken ct)` — público, scoped, **nunca lanza** (Task 4 y Task 6 dependen de esa garantía).

- [ ] **Step 1: Escribir los tests que fallan**

`services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Workers/RankingBroadcastDispatcherTests.cs`:

```csharp
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Umbral.Puntuaciones.Api.Workers;
using Umbral.Puntuaciones.Application.Commands;
using Umbral.Puntuaciones.Application.DTOs;
using Umbral.Puntuaciones.Application.Interfaces;
using Umbral.Puntuaciones.Application.Queries;
using Umbral.Puntuaciones.Domain.Enums;
using Umbral.Puntuaciones.UnitTests.Api;

namespace Umbral.Puntuaciones.UnitTests.Workers;

public sealed class FakeRankingRealtimePublisher : IRankingRealtimePublisher
{
    public List<(string Mensaje, Guid PartidaId, object Payload)> Publicados { get; } = new();
    public bool LanzarError { get; set; }

    private Task Registrar(string mensaje, Guid partidaId, object payload)
    {
        if (LanzarError)
        {
            throw new InvalidOperationException("hub caído");
        }
        Publicados.Add((mensaje, partidaId, payload));
        return Task.CompletedTask;
    }

    public Task PublicarRankingTriviaActualizadoAsync(Guid partidaId, RankingJuegoResponse ranking, CancellationToken cancellationToken) =>
        Registrar("Trivia", partidaId, ranking);

    public Task PublicarRankingBdtActualizadoAsync(Guid partidaId, RankingJuegoResponse ranking, CancellationToken cancellationToken) =>
        Registrar("BDT", partidaId, ranking);

    public Task PublicarRankingConsolidadoCalculadoAsync(RankingConsolidadoResponse ranking, CancellationToken cancellationToken) =>
        Registrar("Consolidado", ranking.PartidaId, ranking);
}

// ISender que siempre lanza: cubre la rama "la query falla" (p.ej. PartidaNoTerminadaException
// si PartidaFinalizada se proyectó sobre una partida que quedó Cancelada).
public sealed class SenderQueFalla : ISender
{
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("query falló");

    public Task<object?> Send(object request, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("query falló");

    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest =>
        throw new InvalidOperationException("query falló");

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}

public class RankingBroadcastDispatcherTests
{
    private static readonly DateTime Ahora = DateTime.UtcNow;

    private static RankingJuegoResponse RankingJuego() =>
        new(Guid.NewGuid(), TipoJuego.Trivia, Ahora,
            new[] { new EntradaRankingDto(1, Guid.NewGuid(), TipoCompetidor.Participante, 10, 1500, 1) });

    private static RankingBroadcastDispatcher Construir(ISender sender, IRankingRealtimePublisher publisher) =>
        new(sender, publisher, NullLogger<RankingBroadcastDispatcher>.Instance);

    [Fact]
    public async Task Puntaje_trivia_resuelve_ranking_del_juego_y_publica_Trivia()
    {
        var ranking = RankingJuego();
        var sender = new FakeSender(ranking);
        var publisher = new FakeRankingRealtimePublisher();
        var comando = new ProyectarPuntajeTriviaCommand(Guid.NewGuid(), Ahora, Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 10, 1500, null);

        await Construir(sender, publisher).DifundirAsync(comando, CancellationToken.None);

        var query = Assert.IsType<ObtenerRankingJuegoQuery>(sender.LastRequest);
        Assert.Equal(comando.PartidaId, query.PartidaId);
        Assert.Equal(comando.JuegoId, query.JuegoId);
        var publicado = Assert.Single(publisher.Publicados);
        Assert.Equal(("Trivia", comando.PartidaId), (publicado.Mensaje, publicado.PartidaId));
        Assert.Same(ranking, publicado.Payload);
    }

    [Fact]
    public async Task Etapa_bdt_ganada_publica_BDT()
    {
        var sender = new FakeSender(RankingJuego());
        var publisher = new FakeRankingRealtimePublisher();
        var comando = new ProyectarEtapaBdtGanadaCommand(Guid.NewGuid(), Ahora, Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 25, 4000, null);

        await Construir(sender, publisher).DifundirAsync(comando, CancellationToken.None);

        Assert.Equal("BDT", Assert.Single(publisher.Publicados).Mensaje);
    }

    [Fact]
    public async Task Partida_finalizada_resuelve_consolidado_y_publica()
    {
        var partidaId = Guid.NewGuid();
        var consolidado = new RankingConsolidadoResponse(partidaId, Ahora, Array.Empty<EntradaRankingConsolidadoDto>());
        var sender = new FakeSender(consolidado);
        var publisher = new FakeRankingRealtimePublisher();
        var comando = new ProyectarPartidaFinalizadaCommand(Guid.NewGuid(), Ahora, partidaId, Guid.NewGuid(), Ahora);

        await Construir(sender, publisher).DifundirAsync(comando, CancellationToken.None);

        var query = Assert.IsType<ObtenerRankingConsolidadoQuery>(sender.LastRequest);
        Assert.Equal(partidaId, query.PartidaId);
        Assert.Equal(("Consolidado", partidaId), (Assert.Single(publisher.Publicados).Mensaje, Assert.Single(publisher.Publicados).PartidaId));
    }

    [Fact]
    public async Task Comandos_sin_ranking_no_publican_ni_consultan()
    {
        var sender = new FakeSender(null);
        var publisher = new FakeRankingRealtimePublisher();
        var comando = new ProyectarPartidaIniciadaCommand(Guid.NewGuid(), Ahora, Guid.NewGuid(), Guid.NewGuid(), Ahora);

        await Construir(sender, publisher).DifundirAsync(comando, CancellationToken.None);

        Assert.Null(sender.LastRequest);
        Assert.Empty(publisher.Publicados);
    }

    [Fact]
    public async Task Fallo_de_la_query_no_propaga()
    {
        var publisher = new FakeRankingRealtimePublisher();
        var comando = new ProyectarPartidaFinalizadaCommand(Guid.NewGuid(), Ahora, Guid.NewGuid(), Guid.NewGuid(), Ahora);

        await Construir(new SenderQueFalla(), publisher).DifundirAsync(comando, CancellationToken.None);

        Assert.Empty(publisher.Publicados);
    }

    [Fact]
    public async Task Fallo_del_publisher_no_propaga()
    {
        var sender = new FakeSender(RankingJuego());
        var publisher = new FakeRankingRealtimePublisher { LanzarError = true };
        var comando = new ProyectarPuntajeTriviaCommand(Guid.NewGuid(), Ahora, Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 10, 1500, null);

        await Construir(sender, publisher).DifundirAsync(comando, CancellationToken.None);

        Assert.Empty(publisher.Publicados);
    }
}
```

- [ ] **Step 2: Verificar que fallan (no compilan)**

Run: `dotnet test "services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Umbral.Puntuaciones.UnitTests.csproj" --filter "FullyQualifiedName~RankingBroadcastDispatcherTests"`
Expected: FAIL de compilación — `RankingBroadcastDispatcher` no definido.

- [ ] **Step 3: Implementación mínima**

`services/puntuaciones/src/Umbral.Puntuaciones.Api/Workers/RankingBroadcastDispatcher.cs`:

```csharp
using MediatR;
using Umbral.Puntuaciones.Application.Commands;
using Umbral.Puntuaciones.Application.Interfaces;
using Umbral.Puntuaciones.Application.Queries;

namespace Umbral.Puntuaciones.Api.Workers;

// Difusión best-effort tras una proyección exitosa (SP-4c): resuelve el ranking recalculado con la
// misma query que sirve el GET y lo publica por SignalR. Cualquier fallo se degrada a warning:
// la proyección y el ack del worker nunca dependen del push (ADR-0012).
public sealed class RankingBroadcastDispatcher
{
    private readonly ISender _sender;
    private readonly IRankingRealtimePublisher _publisher;
    private readonly ILogger<RankingBroadcastDispatcher> _logger;

    public RankingBroadcastDispatcher(ISender sender, IRankingRealtimePublisher publisher,
        ILogger<RankingBroadcastDispatcher> logger)
    {
        _sender = sender;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task DifundirAsync(object comandoProyectado, CancellationToken ct)
    {
        try
        {
            switch (comandoProyectado)
            {
                case ProyectarPuntajeTriviaCommand c:
                    await _publisher.PublicarRankingTriviaActualizadoAsync(c.PartidaId,
                        await _sender.Send(new ObtenerRankingJuegoQuery(c.PartidaId, c.JuegoId), ct), ct);
                    break;
                case ProyectarEtapaBdtGanadaCommand c:
                    await _publisher.PublicarRankingBdtActualizadoAsync(c.PartidaId,
                        await _sender.Send(new ObtenerRankingJuegoQuery(c.PartidaId, c.JuegoId), ct), ct);
                    break;
                case ProyectarPartidaFinalizadaCommand c:
                    await _publisher.PublicarRankingConsolidadoCalculadoAsync(
                        await _sender.Send(new ObtenerRankingConsolidadoQuery(c.PartidaId), ct), ct);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Fallo difundiendo ranking tras proyectar {Comando}; la proyección y el ack no se ven afectados.",
                comandoProyectado.GetType().Name);
        }
    }
}
```

En `Program.cs`, junto al registro del hosted service:

```csharp
builder.Services.AddScoped<Umbral.Puntuaciones.Api.Workers.RankingBroadcastDispatcher>();
```

- [ ] **Step 4: Verificar que pasan**

Run: `dotnet test "services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Umbral.Puntuaciones.UnitTests.csproj" --filter "FullyQualifiedName~RankingBroadcastDispatcherTests"`
Expected: PASS (6/6).

- [ ] **Step 5: Commit**

```bash
git add services/puntuaciones
git commit -m "feat(puntuaciones): dispatcher best-effort de difusion de rankings (SP-4c)"
```

---

### Task 4: Pipeline de proyección + difusión en el worker

**Files:**
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Api/Workers/ProyeccionPipeline.cs`
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Api/Workers/OperacionesSesionEventsConsumer.cs`
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Api/Program.cs` (registro DI)

**Interfaces:**
- Consumes: `RankingBroadcastDispatcher.DifundirAsync(object, CancellationToken)` (Task 3, nunca lanza).
- Produces: `ProyeccionPipeline.EjecutarAsync(object comando, CancellationToken ct)` — singleton; crea un scope, despacha el comando por `ISender` (las excepciones de la proyección **sí** propagan, el worker conserva su reintento por `DbUpdateException`) y, solo si la proyección tuvo éxito, difunde. Task 6 lo usa como punto de entrada E2E (mismo código que producción).

Este pipeline extrae el `DespacharAsync` privado del worker: así el camino real proyección→difusión es invocable desde los tests de integración sin RabbitMQ. Sin unit tests propios: la garantía "difusión solo tras éxito / fallo no bloquea" ya está fijada por los tests del dispatcher (Task 3) y el E2E (Task 6); el reintento del worker conserva su comportamiento SP-4b.

- [ ] **Step 1: Implementar el pipeline**

`services/puntuaciones/src/Umbral.Puntuaciones.Api/Workers/ProyeccionPipeline.cs`:

```csharp
using MediatR;

namespace Umbral.Puntuaciones.Api.Workers;

// Camino único proyección→difusión (SP-4c): scope propio por comando; la difusión solo ocurre si la
// proyección tuvo éxito y nunca lanza (el dispatcher degrada todo fallo a warning). Extraído del
// worker para que integración pueda ejercitar el mismo código sin broker.
public sealed class ProyeccionPipeline
{
    private readonly IServiceScopeFactory _scopeFactory;

    public ProyeccionPipeline(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public async Task EjecutarAsync(object comando, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        await sender.Send(comando, ct);
        var difusor = scope.ServiceProvider.GetRequiredService<RankingBroadcastDispatcher>();
        await difusor.DifundirAsync(comando, ct);
    }
}
```

- [ ] **Step 2: Reemplazar `DespacharAsync` en el worker**

En `OperacionesSesionEventsConsumer.cs`:

1. Constructor: sustituir el parámetro `IServiceScopeFactory scopeFactory` por `ProyeccionPipeline pipeline`, campo `_scopeFactory` por `private readonly ProyeccionPipeline _pipeline;`.
2. Eliminar el método privado `DespacharAsync` y reemplazar sus dos llamadas (intento y reintento dentro de `ProcesarMensajeAsync`) por `await _pipeline.EjecutarAsync(command, ct);`. Los `catch (DbUpdateException)` / `catch (Exception)` y el `BasicAck` del `finally` quedan **exactamente igual** (el dispatcher no lanza, así que ninguna excepción nueva llega a esos catch).

En `Program.cs`, antes de `AddHostedService`:

```csharp
builder.Services.AddSingleton<Umbral.Puntuaciones.Api.Workers.ProyeccionPipeline>();
```

- [ ] **Step 3: Compilar y regresión completa**

Run: `dotnet test "services/puntuaciones/Umbral.Puntuaciones.sln"`
Expected: PASS — 108 (86 unit + 15 integration + 7 contract), 0 failed. (86 = 74 + 3 hub + 3 publisher + 6 dispatcher.)

- [ ] **Step 4: Commit**

```bash
git add services/puntuaciones
git commit -m "feat(puntuaciones): pipeline proyeccion+difusion en el worker (SP-4c)"
```

---

### Task 5: Hardening `[Authorize]` + `access_token` del hub + infraestructura de auth en tests

**Files:**
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Api/Controllers/RankingsController.cs`
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Api/Controllers/EquiposController.cs`
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Api/Program.cs` (JwtBearerEvents)
- Create: `services/puntuaciones/tests/Umbral.Puntuaciones.ContractTests/TestAuthHandler.cs`
- Create: `services/puntuaciones/tests/Umbral.Puntuaciones.ContractTests/PuntuacionesWebFactory.cs`
- Create: `services/puntuaciones/tests/Umbral.Puntuaciones.IntegrationTests/TestAuthHandler.cs`
- Create: `services/puntuaciones/tests/Umbral.Puntuaciones.IntegrationTests/PuntuacionesWebFactory.cs`
- Modify: `services/puntuaciones/tests/Umbral.Puntuaciones.ContractTests/RankingContractTests.cs`, `ConsolidadoContractTests.cs`
- Modify: `services/puntuaciones/tests/Umbral.Puntuaciones.IntegrationTests/ProyeccionYRankingE2ETests.cs`, `ConsolidadoYRendimientoE2ETests.cs`, `RabbitMqProyeccionRoundTripTests.cs`
- Test: `services/puntuaciones/tests/Umbral.Puntuaciones.ContractTests/AutorizacionContractTests.cs`
- Test: modificar `services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Api/RankingsControllerTests.cs` y `EquiposControllerTests.cs` (test de atributo)

**Interfaces:**
- Consumes: patrón `TestAuthHandler` de `services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/TestAuthHandler.cs` (header `X-Test-Sub`).
- Produces: `PuntuacionesWebFactory : WebApplicationFactory<Program>` con método `CreateClientAutenticado()` en **ambos** proyectos de test (namespaces `Umbral.Puntuaciones.ContractTests` / `Umbral.Puntuaciones.IntegrationTests`); Task 6 usa la de IntegrationTests para el hub. `HealthContractTests`/`HealthEndpointTests` **no se tocan** (health sigue anónimo con la factory base).

- [ ] **Step 1: Tests de atributo (fallan primero)**

Añadir al final de `RankingsControllerTests.cs`:

```csharp
    [Fact]
    public void Exige_autenticacion_a_nivel_de_clase()
    {
        var atributos = typeof(RankingsController)
            .GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), inherit: true);
        Assert.NotEmpty(atributos);
    }
```

Y el equivalente en `EquiposControllerTests.cs` con `typeof(EquiposController)`.

Run: `dotnet test "services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Umbral.Puntuaciones.UnitTests.csproj" --filter "FullyQualifiedName~Exige_autenticacion"`
Expected: FAIL (2 tests, `Assert.NotEmpty` sobre colección vacía).

- [ ] **Step 2: Aplicar `[Authorize]` y el `access_token` del hub**

En ambos controllers, añadir `using Microsoft.AspNetCore.Authorization;` y el atributo sobre la clase (debajo de `[Route("puntuaciones")]` / la ruta existente):

```csharp
[Authorize]
```

En `Program.cs`, dentro de `.AddJwtBearer(options => { ... })`, después de `options.TokenValidationParameters = ...;` añadir:

```csharp
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    // SignalR no puede mandar el header Authorization por WebSocket: el token viaja
                    // en el query string solo para la ruta del hub (patrón de Operaciones de Sesión).
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/puntuaciones/hubs/ranking"))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                }
            };
```

Run (atributo): `dotnet test "services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Umbral.Puntuaciones.UnitTests.csproj" --filter "FullyQualifiedName~Exige_autenticacion"`
Expected: PASS (2/2). Los proyectos de contract/integration ahora fallarán en HTTP (401/500) — se arregla en los pasos siguientes.

- [ ] **Step 3: `TestAuthHandler` + `PuntuacionesWebFactory` en ContractTests**

`services/puntuaciones/tests/Umbral.Puntuaciones.ContractTests/TestAuthHandler.cs`:

```csharp
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Umbral.Puntuaciones.ContractTests;

public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-Sub", out var subValue) ||
            string.IsNullOrWhiteSpace(subValue.ToString()))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing X-Test-Sub header"));
        }

        var sub = subValue.ToString();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, sub),
            new Claim("sub", sub)
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
```

`services/puntuaciones/tests/Umbral.Puntuaciones.ContractTests/PuntuacionesWebFactory.cs`:

```csharp
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Umbral.Puntuaciones.ContractTests;

// Host de test con scheme de autenticación de prueba: [Authorize] exige un scheme por defecto y el
// bloque Keycloak de Program.cs no se configura en tests.
public sealed class PuntuacionesWebFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    public HttpClient CreateClientAutenticado()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Sub", Guid.NewGuid().ToString());
        return client;
    }
}
```

Crear los **mismos dos archivos** en `services/puntuaciones/tests/Umbral.Puntuaciones.IntegrationTests/` cambiando únicamente el namespace a `Umbral.Puntuaciones.IntegrationTests`.

- [ ] **Step 4: Migrar los tests HTTP existentes a la factory autenticada**

En `RankingContractTests.cs`, `ConsolidadoContractTests.cs`, `ProyeccionYRankingE2ETests.cs`, `ConsolidadoYRendimientoE2ETests.cs`:
- `IClassFixture<WebApplicationFactory<Program>>` → `IClassFixture<PuntuacionesWebFactory>`; campo y parámetro del constructor a `PuntuacionesWebFactory`.
- Cada `_factory.CreateClient()` → `_factory.CreateClientAutenticado()`.
- Quitar el `using Microsoft.AspNetCore.Mvc.Testing;` si queda sin uso.

En `RabbitMqProyeccionRoundTripTests.cs` (opt-in): sustituir la construcción de la factory por `new PuntuacionesWebFactory()` (o el fixture equivalente que use) y el `factory.CreateClient()` de la línea ~42 por `factory.CreateClientAutenticado()`; los `GetAsync` de ranking pasan a ir autenticados. No cambiar nada más del test.

`HealthContractTests` y `HealthEndpointTests` quedan como están (siguen con `WebApplicationFactory<Program>` y sin header: `/health` es anónimo).

Run: `dotnet test "services/puntuaciones/Umbral.Puntuaciones.sln"`
Expected: PASS — 110 (88 unit + 15 integration + 7 contract).

- [ ] **Step 5: Contract tests de 401 (fallan → pasan)**

`services/puntuaciones/tests/Umbral.Puntuaciones.ContractTests/AutorizacionContractTests.cs`:

```csharp
using System.Net;

namespace Umbral.Puntuaciones.ContractTests;

public class AutorizacionContractTests : IClassFixture<PuntuacionesWebFactory>
{
    private readonly PuntuacionesWebFactory _factory;

    public AutorizacionContractTests(PuntuacionesWebFactory factory) => _factory = factory;

    [Theory]
    [InlineData("/puntuaciones/partidas/11111111-1111-1111-1111-111111111111/juegos/22222222-2222-2222-2222-222222222222/ranking")]
    [InlineData("/puntuaciones/partidas/11111111-1111-1111-1111-111111111111/juegos/22222222-2222-2222-2222-222222222222/marcadores/33333333-3333-3333-3333-333333333333")]
    [InlineData("/puntuaciones/partidas/11111111-1111-1111-1111-111111111111/ranking-consolidado")]
    [InlineData("/puntuaciones/equipos/11111111-1111-1111-1111-111111111111/rendimiento")]
    public async Task Endpoints_de_lectura_sin_token_devuelven_401(string ruta)
    {
        var client = _factory.CreateClient(); // sin X-Test-Sub

        var response = await client.GetAsync(ruta);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Negotiate_del_hub_sin_token_devuelve_401_y_con_token_200()
    {
        var anonimo = _factory.CreateClient();
        var autenticado = _factory.CreateClientAutenticado();

        var sinToken = await anonimo.PostAsync("/puntuaciones/hubs/ranking/negotiate?negotiateVersion=1", null);
        var conToken = await autenticado.PostAsync("/puntuaciones/hubs/ranking/negotiate?negotiateVersion=1", null);

        Assert.Equal(HttpStatusCode.Unauthorized, sinToken.StatusCode);
        Assert.Equal(HttpStatusCode.OK, conToken.StatusCode);
    }

    [Fact]
    public async Task Health_sigue_siendo_anonimo()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
```

Run: `dotnet test "services/puntuaciones/tests/Umbral.Puntuaciones.ContractTests/Umbral.Puntuaciones.ContractTests.csproj"`
Expected: PASS — 13 (7 + 6 nuevos).

- [ ] **Step 6: Suite completa + commit**

Run: `dotnet test "services/puntuaciones/Umbral.Puntuaciones.sln"`
Expected: PASS — 116 (88 unit + 15 integration + 13 contract).

```bash
git add services/puntuaciones
git commit -m "feat(puntuaciones): [Authorize] en endpoints y hub con auth de prueba en tests (SP-4c)"
```

---

### Task 6: E2E de tiempo real (cliente SignalR contra el TestServer)

**Files:**
- Modify: `services/puntuaciones/tests/Umbral.Puntuaciones.IntegrationTests/Umbral.Puntuaciones.IntegrationTests.csproj` (paquete cliente)
- Test: `services/puntuaciones/tests/Umbral.Puntuaciones.IntegrationTests/RankingRealtimeE2ETests.cs`

**Interfaces:**
- Consumes: `ProyeccionPipeline.EjecutarAsync(object, CancellationToken)` (Task 4 — mismo camino que el worker en producción); `PuntuacionesWebFactory.CreateClientAutenticado()` (Task 5, namespace IntegrationTests); mensajes `RankingTriviaActualizado` / `RankingConsolidadoCalculado` (Task 1).
- Produces: cobertura E2E hub+publisher+dispatcher+queries+serialización; nada consumido por tareas posteriores.

- [ ] **Step 1: Añadir el paquete cliente**

Run: `dotnet add "services/puntuaciones/tests/Umbral.Puntuaciones.IntegrationTests/Umbral.Puntuaciones.IntegrationTests.csproj" package Microsoft.AspNetCore.SignalR.Client --version 8.0.11`
Expected: paquete añadido (cualquier 8.0.x vale si 8.0.11 no está en caché).

- [ ] **Step 2: Escribir los tests que fallan**

`services/puntuaciones/tests/Umbral.Puntuaciones.IntegrationTests/RankingRealtimeE2ETests.cs`:

```csharp
using System.Text.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Umbral.Puntuaciones.Api.Workers;
using Umbral.Puntuaciones.Application.Commands;
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.IntegrationTests;

public class RankingRealtimeE2ETests : IClassFixture<PuntuacionesWebFactory>
{
    private static readonly DateTime Ahora = DateTime.UtcNow;
    private static readonly TimeSpan Espera = TimeSpan.FromSeconds(10);
    private readonly PuntuacionesWebFactory _factory;

    public RankingRealtimeE2ETests(PuntuacionesWebFactory factory) => _factory = factory;

    // Mismo camino que el worker en producción: proyección + difusión best-effort.
    private async Task ProyectarPorPipeline(object comando)
    {
        var pipeline = _factory.Services.GetRequiredService<ProyeccionPipeline>();
        await pipeline.EjecutarAsync(comando, CancellationToken.None);
    }

    private HubConnection Conectar()
    {
        _ = _factory.Server; // fuerza el arranque del TestServer
        return new HubConnectionBuilder()
            .WithUrl(new Uri(_factory.Server.BaseAddress, "puntuaciones/hubs/ranking"), options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                options.Headers.Add("X-Test-Sub", Guid.NewGuid().ToString());
                options.Transports = HttpTransportType.LongPolling; // el TestServer no soporta WebSocket
            })
            .Build();
    }

    [Fact]
    public async Task Puntaje_trivia_proyectado_difunde_el_ranking_recalculado_al_grupo()
    {
        var partidaId = Guid.NewGuid();
        var sesionId = Guid.NewGuid();
        var juegoId = Guid.NewGuid();
        var competidor = Guid.NewGuid();
        await ProyectarPorPipeline(new ProyectarPartidaPublicadaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, Modalidad.Individual));
        await ProyectarPorPipeline(new ProyectarJuegoActivadoCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juegoId, 1, TipoJuego.Trivia));

        await using var conexion = Conectar();
        var recibido = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        conexion.On<JsonElement>("RankingTriviaActualizado", payload => recibido.TrySetResult(payload));
        await conexion.StartAsync();
        await conexion.InvokeAsync("SuscribirAPartida", partidaId);

        await ProyectarPorPipeline(new ProyectarPuntajeTriviaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juegoId,
            Guid.NewGuid(), competidor, 10, 1500, null));

        var payload = await recibido.Task.WaitAsync(Espera);
        Assert.Equal(juegoId, payload.GetProperty("juegoId").GetGuid());
        Assert.Equal("Trivia", payload.GetProperty("tipoJuego").GetString());
        var entrada = payload.GetProperty("entradas")[0];
        Assert.Equal(competidor, entrada.GetProperty("competidorId").GetGuid());
        Assert.Equal(10, entrada.GetProperty("puntos").GetInt32());
    }

    [Fact]
    public async Task Partida_finalizada_difunde_el_consolidado()
    {
        var partidaId = Guid.NewGuid();
        var sesionId = Guid.NewGuid();
        var juegoId = Guid.NewGuid();
        var competidor = Guid.NewGuid();
        await ProyectarPorPipeline(new ProyectarPartidaPublicadaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, Modalidad.Individual));
        await ProyectarPorPipeline(new ProyectarJuegoActivadoCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juegoId, 1, TipoJuego.BusquedaDelTesoro));
        await ProyectarPorPipeline(new ProyectarEtapaBdtGanadaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juegoId,
            Guid.NewGuid(), competidor, 25, 4000, null));

        await using var conexion = Conectar();
        var recibido = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        conexion.On<JsonElement>("RankingConsolidadoCalculado", payload => recibido.TrySetResult(payload));
        await conexion.StartAsync();
        await conexion.InvokeAsync("SuscribirAPartida", partidaId);

        await ProyectarPorPipeline(new ProyectarPartidaFinalizadaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, Ahora));

        var payload = await recibido.Task.WaitAsync(Espera);
        Assert.Equal(partidaId, payload.GetProperty("partidaId").GetGuid());
        var entrada = payload.GetProperty("entradas")[0];
        Assert.Equal(competidor, entrada.GetProperty("competidorId").GetGuid());
        Assert.Equal(1, entrada.GetProperty("juegosGanados").GetInt32());
        Assert.Equal(25, entrada.GetProperty("puntosTotales").GetInt32());
    }

    [Fact]
    public async Task Suscribirse_a_partida_no_proyectada_lanza_HubException()
    {
        await using var conexion = Conectar();
        await conexion.StartAsync();

        await Assert.ThrowsAsync<Microsoft.AspNetCore.SignalR.HubException>(
            () => conexion.InvokeAsync("SuscribirAPartida", Guid.NewGuid()));
    }
}
```

- [ ] **Step 3: Verificar que pasan (la implementación ya existe desde Tasks 1-5)**

Run: `dotnet test "services/puntuaciones/tests/Umbral.Puntuaciones.IntegrationTests/Umbral.Puntuaciones.IntegrationTests.csproj"`
Expected: PASS — 18 (15 + 3 nuevos). Si `Puntaje_trivia...` expira en los 10 s, revisar que `MapHub` (Task 1) y el registro del publisher (Task 2) estén en `Program.cs` — es el modo de fallo típico.

- [ ] **Step 4: Suite completa + commit**

Run: `dotnet test "services/puntuaciones/Umbral.Puntuaciones.sln"`
Expected: PASS — 119 (88 unit + 18 integration + 13 contract).

```bash
git add services/puntuaciones
git commit -m "test(puntuaciones): e2e de ranking en vivo por SignalR (SP-4c)"
```

---

### Task 7: Contratos, service-context, traceability y verificación final

**Files:**
- Modify: `contracts/http/puntuaciones-api.md`
- Modify: `contracts/events/operaciones-sesion-events.md` (solo las 2 notas "deferred to Puntuaciones (SP-4)")
- Modify: `services/puntuaciones/service-context.md`
- Modify: `docs/04-sdd/traceability-matrix.md`

**Interfaces:**
- Consumes: nombres/rutas/payloads exactos de Tasks 1-6; conteos reales del `dotnet test` final.
- Produces: documentación canónica del push de ranking; deuda de hardening retirada.

- [ ] **Step 1: `contracts/http/puntuaciones-api.md`**

1. Reemplazar el párrafo `## Status` para registrar SP-4c: los 4 endpoints HTTP + hub SignalR de ranking en vivo; pendiente **solo** auditoría/historial (SP-4d).
2. Añadir una sección nueva antes de `## Autorización`:

```markdown
## SignalR — ranking en vivo (SP-4c)

Hub: `puntuaciones/hubs/ranking` (vía gateway, ruta `/puntuaciones/*`; el token JWT viaja en el
query string `access_token` durante la negociación WebSocket — el gateway no reescribe paths).

Métodos del cliente:

| Método | Parámetros | Comportamiento |
|---|---|---|
| `SuscribirAPartida` | `partidaId: guid` | Une la conexión al grupo de la partida. `HubException("Partida no proyectada.")` si la partida no existe en las proyecciones (el cliente reintenta al recibir `PartidaEnLobby` de Operaciones de Sesión). |
| `DesuscribirDePartida` | `partidaId: guid` | Remueve la conexión del grupo. |

Mensajes servidor→cliente (payloads = shapes HTTP ya documentados en este contrato; enums como
string, camelCase):

| Mensaje | Disparador (evento proyectado) | Payload |
|---|---|---|
| `RankingTriviaActualizado` | `PuntajeTriviaIncrementado` | El shape de `GET .../juegos/{juegoId}/ranking` |
| `RankingBDTActualizado` | `EtapaBDTGanada` | El shape de `GET .../juegos/{juegoId}/ranking` |
| `RankingConsolidadoCalculado` | `PartidaFinalizada` | El shape de `GET .../ranking-consolidado` |

La difusión es best-effort (ADR-0012): un push perdido no se reintenta; los GET HTTP son la fuente
recuperable. Scoring tardío tras `PartidaFinalizada` re-difunde el ranking nativo del juego, no el
consolidado (la relectura HTTP lo incorpora).
```

3. Reescribir `## Autorización`: la autenticación se exige en el gateway **y** en el servicio (defensa en profundidad): `[Authorize]` en los endpoints de lectura y en el hub; `/health` anónimo; lectura para cualquier rol autenticado, sin permiso funcional específico. Eliminar la frase "hardening del servicio → SP-4c".

- [ ] **Step 2: `contracts/events/operaciones-sesion-events.md`**

Actualizar las dos notas que dicen `RankingTriviaActualizado` / `RankingBDTActualizado` "is deferred to Puntuaciones (SP-4)" para que digan que desde SP-4c se difunden como mensajes SignalR por Puntuaciones (referenciar la sección SignalR de `contracts/http/puntuaciones-api.md`). No tocar cola, bindings ni payloads.

- [ ] **Step 3: `services/puntuaciones/service-context.md`**

1. Extender el párrafo de Status con SP-4c: hub `puntuaciones/hubs/ranking`, los 3 mensajes, difusión best-effort orquestada por el worker tras cada proyección exitosa (`ProyeccionPipeline` → `RankingBroadcastDispatcher`), `[Authorize]` en endpoints y hub.
2. Pending queda: **solo** audit/history projection (SP-4d).
3. Deuda: retirar "`[Authorize]`/hardening del servicio → SP-4c" (saldada); acotar "ramas warn+ack del worker sin unit tests" a las ramas SP-4a preexistentes (la rama de difusión quedó cubierta por los tests del dispatcher y el E2E); mantener `ArgumentException`→400 sin log y retención/índice de `eventos_procesados` → SP-4d.

- [ ] **Step 4: `docs/04-sdd/traceability-matrix.md`**

Añadir la fila SP-4c inmediatamente después de la fila SP-4b, mismo formato de una línea. Fuentes: RF-13, RF-22, RF-37, RF-38, RNF-03, RNF-17, RNF-21, HU-26, HU-42; spec `docs/superpowers/specs/2026-07-06-sp4c-signalr-ranking-vivo-design.md`; plan `docs/superpowers/plans/2026-07-06-sp4c-signalr-ranking-vivo.md`; conteos reales del Step 5.

- [ ] **Step 5: Verificación final**

Run: `dotnet test "services/puntuaciones/Umbral.Puntuaciones.sln"`
Expected: PASS — 119/119 (88 unit + 18 integration + 13 contract), 0 failed, 0 skipped. Usar los conteos **reales** en la fila de traceability. Round-trip RabbitMQ opt-in: correr solo si Docker está disponible; si no, anotarlo en la fila (mismo criterio que SP-4b).

- [ ] **Step 6: Commit**

```bash
git add contracts/http/puntuaciones-api.md contracts/events/operaciones-sesion-events.md services/puntuaciones/service-context.md docs/04-sdd/traceability-matrix.md
git commit -m "docs(puntuaciones): contratos SignalR SP-4c, service-context y traceability"
```
