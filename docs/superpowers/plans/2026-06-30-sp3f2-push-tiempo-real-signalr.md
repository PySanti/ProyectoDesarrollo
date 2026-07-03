# SP-3f-2 — Push tiempo real (SignalR) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Añadir un canal de push en vivo (SignalR) en Operaciones de Sesión que difunde las transiciones de estado runtime de cada partida a los clientes suscritos, sin depender del broker RabbitMQ.

**Architecture:** Un `SignalRSesionEventsPublisher` implementa el puerto existente `ISesionEventsPublisher` (13 métodos) y emite a un `SesionHub` por grupo `partida:{id}`. Un `CompositeSesionEventsPublisher` hace fan-out resiliente a `[NoOp, SignalR]` detrás del puerto, así los handlers no cambian. El gateway YARP ya hace passthrough de WebSockets; se añade lectura del JWT por query `access_token` en servicio y gateway.

**Tech Stack:** .NET 8, ASP.NET Core SignalR (framework compartido `Microsoft.AspNetCore.App`), MediatR (sin tocar), xUnit, fakes a mano (sin Moq).

## Global Constraints

- **Idioma/dominio:** vocabulario de dominio en español; payloads participant-safe.
- **Sin Moq:** todos los dobles de prueba son fakes a mano.
- **Clean Architecture:** Domain no depende de Infra; Hub y SignalR-publisher en **Api** (concern ASP.NET); Composite en **Infrastructure** (depende solo del puerto); puerto en **Application**.
- **Reloj:** usar `DateTime` que ya traen los eventos; no introducir `DateTime.UtcNow` nuevo en producción.
- **Carve-out git (vigente):** dejar SIEMPRE sin commitear `docs/04-sdd/traceability-matrix.md`, `docs/superpowers/specs/2026-06-27-sp3c-runtime-trivia-design.md`, `docs/04-sdd/auditorias/`. Nunca `git add -A` / `git add .` / `git add docs/`. Stagear SOLO los archivos exactos nombrados en cada paso de commit. Prohibido `git checkout/restore/clean/stash/reset` amplios.
- **Mensaje de commit:** cada commit termina con exactamente una línea de atribución:
  `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>` (sin línea Claude-Session).
- **Suite base (HEAD del slice):** UnitTests + IntegrationTests + ContractTests deben quedar verdes al final de cada tarea.

## Refinamiento del plan vs spec (decisión de planeación)

El spec listó `enunciado`/`opciones`/`descripcion` en los payloads de `PreguntaActivada`/`EtapaActivada`. Al revisar los eventos de dominio (`PreguntaTriviaActivadaEvent`, `EtapaBDTActivadaEvent`) se confirmó que **NO cargan ese contenido** — solo ids, `Orden`, `TiempoLimiteSegundos`, `FechaActivacion`. Resolución (dentro de la "decisión abierta" del spec sobre extensión de eventos):

- **Payloads de push delgados (señal):** llevan ids + `Orden` + `FechaLimiteUtc`. El cliente trae el contenido (texto/opciones/área) por los endpoints de pull existentes (`GET /pregunta-actual`, `GET /etapa-actual`) o ya lo tiene de `GET /mi-sesion`.
- **`FechaLimiteUtc` se deriva** de `FechaActivacion.AddSeconds(TiempoLimiteSegundos)` — **no se extienden los eventos** (se elimina esa sub-tarea del spec).
- Beneficio: cero churn de eventos, cero superficie de leak (el push nunca transporta texto de preguntas/opciones/QR).

## File Structure

**Producción (crear):**
- `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SesionRealtimeMessages.cs` — constantes de nombres de mensaje + helper `GrupoPartida`.
- `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SesionRealtimePayloads.cs` — los 10 records de payload.
- `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SesionHub.cs` — el hub (`SuscribirAPartida`/`DesuscribirDePartida`).
- `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SignalRSesionEventsPublisher.cs` — impl del puerto vía `IHubContext<SesionHub>`.
- `services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Services/CompositeSesionEventsPublisher.cs` — fan-out resiliente.

**Producción (modificar):**
- `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Program.cs` — `AddSignalR`, registro composite, JWT `access_token` query, `MapHub`.
- `gateway/src/Umbral.Gateway/Security/KeycloakJwtExtensions.cs` — `OnMessageReceived` lee `access_token` query en ruta del hub.
- `contracts/http/operaciones-sesion-api.md` — sección "Realtime / SignalR".

**Tests (crear/modificar):**
- `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/Realtime/SesionHubTests.cs`
- `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/Realtime/SignalRSesionEventsPublisherTests.cs`
- `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Infrastructure/CompositeSesionEventsPublisherTests.cs`
- `services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/RealtimeWiringTests.cs`
- `services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/RealtimeContractTests.cs`
- `gateway/tests/Umbral.Gateway.IntegrationTests/GatewayEndpointsTests.cs` (modificar: +1 fact)

**Carve-out (modificar, NO commitear):**
- `docs/04-sdd/traceability-matrix.md` — fila SP-3f-2.

---

### Task 1: SesionHub + constantes/payloads/grupo

**Files:**
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SesionRealtimeMessages.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SesionRealtimePayloads.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SesionHub.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/Realtime/SesionHubTests.cs`

**Interfaces:**
- Consumes: `ISesionPartidaRepository.GetByParticipanteActivoAsync(Guid, CancellationToken) -> Task<SesionPartida?>`; `SesionPartida.PartidaId (Guid)`. Fake existente: `Umbral.OperacionesSesion.UnitTests.Application.Fakes.FakeSesionPartidaRepository` con `Add(SesionPartida)`.
- Produces: `SesionRealtimeMessages` (10 consts string + `GrupoPartida(Guid)->string`); payload records; `SesionHub.SuscribirAPartida(Guid)`, `SesionHub.DesuscribirDePartida(Guid)`.

- [ ] **Step 1: Escribir los archivos de constantes y payloads (no requieren test propio; los ejercita el hub y el publisher).**

`SesionRealtimeMessages.cs`:
```csharp
using System;

namespace Umbral.OperacionesSesion.Api.Realtime;

public static class SesionRealtimeMessages
{
    public const string PartidaEnLobby = nameof(PartidaEnLobby);
    public const string PartidaIniciada = nameof(PartidaIniciada);
    public const string JuegoActivado = nameof(JuegoActivado);
    public const string PartidaCancelada = nameof(PartidaCancelada);
    public const string PartidaFinalizada = nameof(PartidaFinalizada);
    public const string PreguntaActivada = nameof(PreguntaActivada);
    public const string PreguntaCerrada = nameof(PreguntaCerrada);
    public const string EtapaActivada = nameof(EtapaActivada);
    public const string EtapaCerrada = nameof(EtapaCerrada);
    public const string EtapaGanada = nameof(EtapaGanada);

    public static string GrupoPartida(Guid partidaId) => $"partida:{partidaId}";
}
```

`SesionRealtimePayloads.cs`:
```csharp
using System;

namespace Umbral.OperacionesSesion.Api.Realtime;

public sealed record PartidaEnLobbyPayload(Guid PartidaId);
public sealed record PartidaIniciadaPayload(Guid PartidaId);
public sealed record JuegoActivadoPayload(Guid PartidaId, Guid JuegoId, int Orden, string TipoJuego);
public sealed record PartidaCanceladaPayload(Guid PartidaId, string Motivo);
public sealed record PartidaFinalizadaPayload(Guid PartidaId);
public sealed record PreguntaActivadaPayload(Guid PartidaId, Guid JuegoId, Guid PreguntaId, int Orden, DateTime FechaLimiteUtc);
public sealed record PreguntaCerradaPayload(Guid PartidaId, Guid JuegoId, Guid PreguntaId);
public sealed record EtapaActivadaPayload(Guid PartidaId, Guid JuegoId, Guid EtapaId, int Orden, DateTime FechaLimiteUtc);
public sealed record EtapaCerradaPayload(Guid PartidaId, Guid JuegoId, Guid EtapaId);
public sealed record EtapaGanadaPayload(Guid PartidaId, Guid JuegoId, Guid EtapaId);
```

- [ ] **Step 2: Escribir el test que falla (hub).**

`SesionHubTests.cs`:
```csharp
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Umbral.OperacionesSesion.Api.Realtime;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Api.Realtime;

public class SesionHubTests
{
    private static ClaimsPrincipal Usuario(string? sub, string? rol)
    {
        var claims = new List<Claim>();
        if (sub is not null) claims.Add(new Claim("sub", sub));
        if (rol is not null) claims.Add(new Claim("roles", rol));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test", "name", "roles"));
    }

    private static SesionPartida SesionDe(Guid partidaId, Guid participanteId)
    {
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, Array.Empty<PreguntaSnapshot>());
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        var s = SesionPartida.Publicar(partidaId, snap);
        s.Inscribir(participanteId, false, 0, new DateTime(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc));
        return s;
    }

    private static SesionHub Construir(ISesionPartidaRepositorioFake repo, ClaimsPrincipal user,
        FakeGroupManager groups, string connId = "c1")
    {
        var hub = new SesionHub(repo.Repo)
        {
            Context = new FakeHubCallerContext(user, connId),
            Groups = groups
        };
        return hub;
    }

    [Fact]
    public async Task Operador_se_une_al_grupo_sin_consultar_repo()
    {
        var partidaId = Guid.NewGuid();
        var repo = new ISesionPartidaRepositorioFake(); // repo vacío
        var groups = new FakeGroupManager();
        var hub = Construir(repo, Usuario(sub: null, rol: "Operador"), groups);

        await hub.SuscribirAPartida(partidaId);

        Assert.Contains(("c1", SesionRealtimeMessages.GrupoPartida(partidaId)), groups.Added);
    }

    [Fact]
    public async Task Inscrito_se_une_al_grupo()
    {
        var partidaId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();
        var repo = new ISesionPartidaRepositorioFake();
        repo.Inner.Add(SesionDe(partidaId, participanteId));
        var groups = new FakeGroupManager();
        var hub = Construir(repo, Usuario(sub: participanteId.ToString(), rol: "Participante"), groups);

        await hub.SuscribirAPartida(partidaId);

        Assert.Contains(("c1", SesionRealtimeMessages.GrupoPartida(partidaId)), groups.Added);
    }

    [Fact]
    public async Task No_inscrito_lanza_HubException_y_no_une()
    {
        var partidaId = Guid.NewGuid();
        var repo = new ISesionPartidaRepositorioFake(); // repo vacío => GetByParticipanteActivoAsync null
        var groups = new FakeGroupManager();
        var hub = Construir(repo, Usuario(sub: Guid.NewGuid().ToString(), rol: "Participante"), groups);

        await Assert.ThrowsAsync<HubException>(() => hub.SuscribirAPartida(partidaId));
        Assert.Empty(groups.Added);
    }

    [Fact]
    public async Task Sin_sub_lanza_HubException()
    {
        var repo = new ISesionPartidaRepositorioFake();
        var groups = new FakeGroupManager();
        var hub = Construir(repo, Usuario(sub: null, rol: "Participante"), groups);

        await Assert.ThrowsAsync<HubException>(() => hub.SuscribirAPartida(Guid.NewGuid()));
    }

    [Fact]
    public async Task Desuscribir_quita_del_grupo()
    {
        var partidaId = Guid.NewGuid();
        var repo = new ISesionPartidaRepositorioFake();
        var groups = new FakeGroupManager();
        var hub = Construir(repo, Usuario(sub: Guid.NewGuid().ToString(), rol: "Operador"), groups);

        await hub.DesuscribirDePartida(partidaId);

        Assert.Contains(("c1", SesionRealtimeMessages.GrupoPartida(partidaId)), groups.Removed);
    }

    // ---- Fakes locales ----

    private sealed class ISesionPartidaRepositorioFake
    {
        public FakeSesionPartidaRepository Inner { get; } = new();
        public Domain.Abstractions.Persistence.ISesionPartidaRepository Repo => Inner;
    }

    private sealed class FakeGroupManager : IGroupManager
    {
        public List<(string Conn, string Group)> Added { get; } = new();
        public List<(string Conn, string Group)> Removed { get; } = new();
        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        { Added.Add((connectionId, groupName)); return Task.CompletedTask; }
        public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        { Removed.Add((connectionId, groupName)); return Task.CompletedTask; }
    }

    private sealed class FakeHubCallerContext : HubCallerContext
    {
        private readonly ClaimsPrincipal _user;
        private readonly string _connId;
        public FakeHubCallerContext(ClaimsPrincipal user, string connId) { _user = user; _connId = connId; }
        public override string ConnectionId => _connId;
        public override string? UserIdentifier => null;
        public override ClaimsPrincipal? User => _user;
        public override IDictionary<object, object?> Items => throw new NotImplementedException();
        public override Microsoft.AspNetCore.Http.Features.IFeatureCollection Features => throw new NotImplementedException();
        public override CancellationToken ConnectionAborted => CancellationToken.None;
        public override void Abort() => throw new NotImplementedException();
    }
}
```

> Nota de tipos: verificar contra `FakeSesionPartidaRepository` real que `GetByParticipanteActivoAsync` devuelve la sesión cuando hay inscripción activa. Si la firma del constructor de `SesionPartida`/helpers difiere, ajustar `SesionDe` al patrón usado en `BarrerTimeoutsCommandHandlerTests` (mismo proyecto). No inventar APIs.

- [ ] **Step 3: Correr el test para verlo fallar.**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj" --filter "FullyQualifiedName~SesionHubTests"`
Expected: FAIL de compilación ("SesionHub no existe").

- [ ] **Step 4: Implementar `SesionHub`.**

`SesionHub.cs`:
```csharp
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;

namespace Umbral.OperacionesSesion.Api.Realtime;

[Authorize]
public sealed class SesionHub : Hub
{
    private readonly ISesionPartidaRepository _repo;

    public SesionHub(ISesionPartidaRepository repo) => _repo = repo;

    public async Task SuscribirAPartida(Guid partidaId)
    {
        var user = Context.User;
        var esOperador = user?.IsInRole("Operador") ?? false;
        if (!esOperador)
        {
            var sub = user?.FindFirst("sub")?.Value ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (sub is null || !Guid.TryParse(sub, out var participanteId))
            {
                throw new HubException("Participante no identificado.");
            }

            var sesion = await _repo.GetByParticipanteActivoAsync(participanteId, Context.ConnectionAborted);
            if (sesion is null || sesion.PartidaId != partidaId)
            {
                throw new HubException("No inscrito en la partida.");
            }
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, SesionRealtimeMessages.GrupoPartida(partidaId), Context.ConnectionAborted);
    }

    public Task DesuscribirDePartida(Guid partidaId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, SesionRealtimeMessages.GrupoPartida(partidaId), Context.ConnectionAborted);
}
```

- [ ] **Step 5: Correr el test y verlo pasar.**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj" --filter "FullyQualifiedName~SesionHubTests"`
Expected: PASS (5 tests).

- [ ] **Step 6: Commit.**

```bash
git add \
  services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SesionRealtimeMessages.cs \
  services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SesionRealtimePayloads.cs \
  services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SesionHub.cs \
  services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/Realtime/SesionHubTests.cs
git commit -m "SP-3f-2 T1: SesionHub (suscripción por partida, auth operador/inscrito) + constantes/payloads

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 2: SignalRSesionEventsPublisher

**Files:**
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SignalRSesionEventsPublisher.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/Realtime/SignalRSesionEventsPublisherTests.cs`

**Interfaces:**
- Consumes: `ISesionEventsPublisher` (13 métodos, en `Umbral.OperacionesSesion.Application.Interfaces`); records de evento (`PreguntaTriviaActivadaEvent(... int Orden, int TiempoLimiteSegundos, DateTime FechaActivacion)`, `EtapaBDTActivadaEvent(... int Orden, int TiempoLimiteSegundos, DateTime FechaActivacion)`, `JuegoActivadoEvent(Guid PartidaId, Guid SesionPartidaId, Guid JuegoId, int Orden, string TipoJuego)`, `PartidaCanceladaEvent(... string Motivo ...)`, etc.); `SesionRealtimeMessages`, payloads (T1); `IHubContext<SesionHub>`.
- Produces: `SignalRSesionEventsPublisher : ISesionEventsPublisher`.

- [ ] **Step 1: Escribir el test que falla.**

`SignalRSesionEventsPublisherTests.cs`:
```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Umbral.OperacionesSesion.Api.Realtime;
using Umbral.OperacionesSesion.Application.Interfaces;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Api.Realtime;

public class SignalRSesionEventsPublisherTests
{
    private static readonly DateTime T0 = new(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc);

    private static (SignalRSesionEventsPublisher pub, FakeHubClients clients) Build()
    {
        var clients = new FakeHubClients();
        var ctx = new FakeHubContext(clients);
        return (new SignalRSesionEventsPublisher(ctx), clients);
    }

    [Fact]
    public async Task JuegoActivado_difunde_al_grupo_con_payload()
    {
        var (pub, clients) = Build();
        var partidaId = Guid.NewGuid();
        var juegoId = Guid.NewGuid();

        await pub.PublicarJuegoActivadoAsync(
            new JuegoActivadoEvent(partidaId, Guid.NewGuid(), juegoId, 2, "Trivia"), CancellationToken.None);

        Assert.Equal(SesionRealtimeMessages.GrupoPartida(partidaId), clients.LastGroup);
        Assert.Equal(SesionRealtimeMessages.JuegoActivado, clients.Proxy.Method);
        var payload = Assert.IsType<JuegoActivadoPayload>(clients.Proxy.Args![0]);
        Assert.Equal(partidaId, payload.PartidaId);
        Assert.Equal(juegoId, payload.JuegoId);
        Assert.Equal(2, payload.Orden);
        Assert.Equal("Trivia", payload.TipoJuego);
    }

    [Fact]
    public async Task PreguntaActivada_deriva_fechaLimite_de_activacion_mas_tiempo()
    {
        var (pub, clients) = Build();
        var partidaId = Guid.NewGuid();

        await pub.PublicarPreguntaTriviaActivadaAsync(
            new PreguntaTriviaActivadaEvent(partidaId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, 30, T0),
            CancellationToken.None);

        Assert.Equal(SesionRealtimeMessages.PreguntaActivada, clients.Proxy.Method);
        var payload = Assert.IsType<PreguntaActivadaPayload>(clients.Proxy.Args![0]);
        Assert.Equal(T0.AddSeconds(30), payload.FechaLimiteUtc);
        Assert.Equal(1, payload.Orden);
    }

    [Fact]
    public async Task EtapaActivada_deriva_fechaLimite()
    {
        var (pub, clients) = Build();
        await pub.PublicarEtapaBDTActivadaAsync(
            new EtapaBDTActivadaEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, 45, T0),
            CancellationToken.None);

        Assert.Equal(SesionRealtimeMessages.EtapaActivada, clients.Proxy.Method);
        var payload = Assert.IsType<EtapaActivadaPayload>(clients.Proxy.Args![0]);
        Assert.Equal(T0.AddSeconds(45), payload.FechaLimiteUtc);
    }

    [Fact]
    public async Task EtapaGanada_difunde_sin_puntaje()
    {
        var (pub, clients) = Build();
        await pub.PublicarEtapaBDTGanadaAsync(
            new EtapaBDTGanadaEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100, 1234),
            CancellationToken.None);

        Assert.Equal(SesionRealtimeMessages.EtapaGanada, clients.Proxy.Method);
        var payload = Assert.IsType<EtapaGanadaPayload>(clients.Proxy.Args![0]);
        // payload no expone Puntaje: la sola existencia del tipo lo garantiza en compilación
        Assert.NotEqual(Guid.Empty, payload.EtapaId);
    }

    [Fact]
    public async Task PartidaFinalizada_difunde()
    {
        var (pub, clients) = Build();
        var partidaId = Guid.NewGuid();
        await pub.PublicarPartidaFinalizadaAsync(
            new PartidaFinalizadaEvent(partidaId, Guid.NewGuid(), T0), CancellationToken.None);

        Assert.Equal(SesionRealtimeMessages.PartidaFinalizada, clients.Proxy.Method);
        Assert.IsType<PartidaFinalizadaPayload>(clients.Proxy.Args![0]);
    }

    [Fact]
    public async Task Eventos_scoring_adjacentes_no_difunden()
    {
        var (pub, clients) = Build();

        await pub.PublicarRespuestaTriviaValidadaAsync(
            new RespuestaTriviaValidadaEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), true, T0),
            CancellationToken.None);
        await pub.PublicarPuntajeTriviaIncrementadoAsync(
            new PuntajeTriviaIncrementadoEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 10, 500),
            CancellationToken.None);
        await pub.PublicarTesoroQRValidadoAsync(
            new TesoroQRValidadoEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Correcto", T0),
            CancellationToken.None);

        Assert.Null(clients.LastGroup);     // nunca se pidió grupo
        Assert.Null(clients.Proxy.Method);  // nunca se envió
    }

    // ---- Fakes locales ----

    private sealed class FakeHubContext : IHubContext<SesionHub>
    {
        public FakeHubContext(IHubClients clients) => Clients = clients;
        public IHubClients Clients { get; }
        public IGroupManager Groups => throw new NotImplementedException();
    }

    private sealed class FakeHubClients : IHubClients
    {
        public string? LastGroup { get; private set; }
        public FakeClientProxy Proxy { get; } = new();
        public IClientProxy Group(string groupName) { LastGroup = groupName; return Proxy; }

        public IClientProxy All => throw new NotImplementedException();
        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => throw new NotImplementedException();
        public IClientProxy Client(string connectionId) => throw new NotImplementedException();
        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => throw new NotImplementedException();
        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => throw new NotImplementedException();
        public IClientProxy Groups(IReadOnlyList<string> groupNames) => throw new NotImplementedException();
        public IClientProxy User(string userId) => throw new NotImplementedException();
        public IClientProxy Users(IReadOnlyList<string> userIds) => throw new NotImplementedException();
    }

    private sealed class FakeClientProxy : IClientProxy
    {
        public string? Method { get; private set; }
        public object?[]? Args { get; private set; }
        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
        { Method = method; Args = args; return Task.CompletedTask; }
    }
}
```

- [ ] **Step 2: Correr el test para verlo fallar.**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj" --filter "FullyQualifiedName~SignalRSesionEventsPublisherTests"`
Expected: FAIL de compilación ("SignalRSesionEventsPublisher no existe").

- [ ] **Step 3: Implementar `SignalRSesionEventsPublisher`.**

`SignalRSesionEventsPublisher.cs`:
```csharp
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Umbral.OperacionesSesion.Application.Interfaces;

namespace Umbral.OperacionesSesion.Api.Realtime;

public sealed class SignalRSesionEventsPublisher : ISesionEventsPublisher
{
    private readonly IHubContext<SesionHub> _hub;

    public SignalRSesionEventsPublisher(IHubContext<SesionHub> hub) => _hub = hub;

    private Task Difundir(System.Guid partidaId, string mensaje, object payload, CancellationToken ct) =>
        _hub.Clients.Group(SesionRealtimeMessages.GrupoPartida(partidaId)).SendAsync(mensaje, payload, ct);

    public Task PublicarPartidaPublicadaEnLobbyAsync(PartidaPublicadaEnLobbyEvent e, CancellationToken ct) =>
        Difundir(e.PartidaId, SesionRealtimeMessages.PartidaEnLobby, new PartidaEnLobbyPayload(e.PartidaId), ct);

    public Task PublicarPartidaIniciadaAsync(PartidaIniciadaEvent e, CancellationToken ct) =>
        Difundir(e.PartidaId, SesionRealtimeMessages.PartidaIniciada, new PartidaIniciadaPayload(e.PartidaId), ct);

    public Task PublicarJuegoActivadoAsync(JuegoActivadoEvent e, CancellationToken ct) =>
        Difundir(e.PartidaId, SesionRealtimeMessages.JuegoActivado, new JuegoActivadoPayload(e.PartidaId, e.JuegoId, e.Orden, e.TipoJuego), ct);

    public Task PublicarPartidaCanceladaAsync(PartidaCanceladaEvent e, CancellationToken ct) =>
        Difundir(e.PartidaId, SesionRealtimeMessages.PartidaCancelada, new PartidaCanceladaPayload(e.PartidaId, e.Motivo), ct);

    public Task PublicarPartidaFinalizadaAsync(PartidaFinalizadaEvent e, CancellationToken ct) =>
        Difundir(e.PartidaId, SesionRealtimeMessages.PartidaFinalizada, new PartidaFinalizadaPayload(e.PartidaId), ct);

    public Task PublicarPreguntaTriviaActivadaAsync(PreguntaTriviaActivadaEvent e, CancellationToken ct) =>
        Difundir(e.PartidaId, SesionRealtimeMessages.PreguntaActivada,
            new PreguntaActivadaPayload(e.PartidaId, e.JuegoId, e.PreguntaId, e.Orden, e.FechaActivacion.AddSeconds(e.TiempoLimiteSegundos)), ct);

    public Task PublicarPreguntaTriviaCerradaAsync(PreguntaTriviaCerradaEvent e, CancellationToken ct) =>
        Difundir(e.PartidaId, SesionRealtimeMessages.PreguntaCerrada, new PreguntaCerradaPayload(e.PartidaId, e.JuegoId, e.PreguntaId), ct);

    public Task PublicarEtapaBDTActivadaAsync(EtapaBDTActivadaEvent e, CancellationToken ct) =>
        Difundir(e.PartidaId, SesionRealtimeMessages.EtapaActivada,
            new EtapaActivadaPayload(e.PartidaId, e.JuegoId, e.EtapaId, e.Orden, e.FechaActivacion.AddSeconds(e.TiempoLimiteSegundos)), ct);

    public Task PublicarEtapaBDTCerradaAsync(EtapaBDTCerradaEvent e, CancellationToken ct) =>
        Difundir(e.PartidaId, SesionRealtimeMessages.EtapaCerrada, new EtapaCerradaPayload(e.PartidaId, e.JuegoId, e.EtapaId), ct);

    public Task PublicarEtapaBDTGanadaAsync(EtapaBDTGanadaEvent e, CancellationToken ct) =>
        Difundir(e.PartidaId, SesionRealtimeMessages.EtapaGanada, new EtapaGanadaPayload(e.PartidaId, e.JuegoId, e.EtapaId), ct);

    // No difunden (per-participante / scoring-adjacentes → SP-4). Documentado en el diseño SP-3f-2.
    public Task PublicarRespuestaTriviaValidadaAsync(RespuestaTriviaValidadaEvent e, CancellationToken ct) => Task.CompletedTask;
    public Task PublicarPuntajeTriviaIncrementadoAsync(PuntajeTriviaIncrementadoEvent e, CancellationToken ct) => Task.CompletedTask;
    public Task PublicarTesoroQRValidadoAsync(TesoroQRValidadoEvent e, CancellationToken ct) => Task.CompletedTask;
}
```

- [ ] **Step 4: Correr el test y verlo pasar.**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj" --filter "FullyQualifiedName~SignalRSesionEventsPublisherTests"`
Expected: PASS (6 tests).

- [ ] **Step 5: Commit.**

```bash
git add \
  services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SignalRSesionEventsPublisher.cs \
  services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/Realtime/SignalRSesionEventsPublisherTests.cs
git commit -m "SP-3f-2 T2: SignalRSesionEventsPublisher (10 difunden / 3 no-op, deadline derivado)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 3: CompositeSesionEventsPublisher (fan-out resiliente)

**Files:**
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Services/CompositeSesionEventsPublisher.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Infrastructure/CompositeSesionEventsPublisherTests.cs`

**Interfaces:**
- Consumes: `ISesionEventsPublisher`; `ILogger<CompositeSesionEventsPublisher>`.
- Produces: `CompositeSesionEventsPublisher(IEnumerable<ISesionEventsPublisher>, ILogger<CompositeSesionEventsPublisher>) : ISesionEventsPublisher`.

- [ ] **Step 1: Escribir el test que falla.**

`CompositeSesionEventsPublisherTests.cs`:
```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Umbral.OperacionesSesion.Application.Interfaces;
using Umbral.OperacionesSesion.Infrastructure.Services;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Infrastructure;

public class CompositeSesionEventsPublisherTests
{
    private static readonly DateTime T0 = new(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc);
    private static PartidaFinalizadaEvent Evt() => new(Guid.NewGuid(), Guid.NewGuid(), T0);

    [Fact]
    public async Task Fan_out_invoca_a_todos()
    {
        var a = new RecordingPublisher();
        var b = new RecordingPublisher();
        var sut = new CompositeSesionEventsPublisher(new ISesionEventsPublisher[] { a, b }, NullLogger<CompositeSesionEventsPublisher>.Instance);

        await sut.PublicarPartidaFinalizadaAsync(Evt(), CancellationToken.None);

        Assert.Equal(1, a.Finalizadas);
        Assert.Equal(1, b.Finalizadas);
    }

    [Fact]
    public async Task Publicador_que_lanza_no_detiene_a_los_demas_ni_propaga()
    {
        var malo = new ThrowingPublisher();
        var bueno = new RecordingPublisher();
        var sut = new CompositeSesionEventsPublisher(new ISesionEventsPublisher[] { malo, bueno }, NullLogger<CompositeSesionEventsPublisher>.Instance);

        await sut.PublicarPartidaFinalizadaAsync(Evt(), CancellationToken.None); // no debe lanzar

        Assert.Equal(1, bueno.Finalizadas);
    }

    [Fact]
    public async Task OperationCanceledException_se_propaga()
    {
        var cancela = new CancelingPublisher();
        var bueno = new RecordingPublisher();
        var sut = new CompositeSesionEventsPublisher(new ISesionEventsPublisher[] { cancela, bueno }, NullLogger<CompositeSesionEventsPublisher>.Instance);

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => sut.PublicarPartidaFinalizadaAsync(Evt(), CancellationToken.None));
    }

    private class RecordingPublisher : NoOpBase
    {
        public int Finalizadas;
        public override Task PublicarPartidaFinalizadaAsync(PartidaFinalizadaEvent e, CancellationToken ct)
        { Finalizadas++; return Task.CompletedTask; }
    }

    private sealed class ThrowingPublisher : NoOpBase
    {
        public override Task PublicarPartidaFinalizadaAsync(PartidaFinalizadaEvent e, CancellationToken ct)
            => throw new InvalidOperationException("boom");
    }

    private sealed class CancelingPublisher : NoOpBase
    {
        public override Task PublicarPartidaFinalizadaAsync(PartidaFinalizadaEvent e, CancellationToken ct)
            => throw new OperationCanceledException();
    }

    // Base que implementa los 13 métodos como no-op; los tests sólo overridean PartidaFinalizada.
    private abstract class NoOpBase : ISesionEventsPublisher
    {
        public virtual Task PublicarPartidaPublicadaEnLobbyAsync(PartidaPublicadaEnLobbyEvent e, CancellationToken ct) => Task.CompletedTask;
        public virtual Task PublicarPartidaIniciadaAsync(PartidaIniciadaEvent e, CancellationToken ct) => Task.CompletedTask;
        public virtual Task PublicarJuegoActivadoAsync(JuegoActivadoEvent e, CancellationToken ct) => Task.CompletedTask;
        public virtual Task PublicarPartidaCanceladaAsync(PartidaCanceladaEvent e, CancellationToken ct) => Task.CompletedTask;
        public virtual Task PublicarPartidaFinalizadaAsync(PartidaFinalizadaEvent e, CancellationToken ct) => Task.CompletedTask;
        public virtual Task PublicarRespuestaTriviaValidadaAsync(RespuestaTriviaValidadaEvent e, CancellationToken ct) => Task.CompletedTask;
        public virtual Task PublicarPuntajeTriviaIncrementadoAsync(PuntajeTriviaIncrementadoEvent e, CancellationToken ct) => Task.CompletedTask;
        public virtual Task PublicarPreguntaTriviaActivadaAsync(PreguntaTriviaActivadaEvent e, CancellationToken ct) => Task.CompletedTask;
        public virtual Task PublicarPreguntaTriviaCerradaAsync(PreguntaTriviaCerradaEvent e, CancellationToken ct) => Task.CompletedTask;
        public virtual Task PublicarTesoroQRValidadoAsync(TesoroQRValidadoEvent e, CancellationToken ct) => Task.CompletedTask;
        public virtual Task PublicarEtapaBDTGanadaAsync(EtapaBDTGanadaEvent e, CancellationToken ct) => Task.CompletedTask;
        public virtual Task PublicarEtapaBDTCerradaAsync(EtapaBDTCerradaEvent e, CancellationToken ct) => Task.CompletedTask;
        public virtual Task PublicarEtapaBDTActivadaAsync(EtapaBDTActivadaEvent e, CancellationToken ct) => Task.CompletedTask;
    }
}
```

- [ ] **Step 2: Correr el test para verlo fallar.**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj" --filter "FullyQualifiedName~CompositeSesionEventsPublisherTests"`
Expected: FAIL de compilación ("CompositeSesionEventsPublisher no existe").

- [ ] **Step 3: Implementar `CompositeSesionEventsPublisher`.**

`CompositeSesionEventsPublisher.cs`:
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Umbral.OperacionesSesion.Application.Interfaces;

namespace Umbral.OperacionesSesion.Infrastructure.Services;

public sealed class CompositeSesionEventsPublisher : ISesionEventsPublisher
{
    private readonly IReadOnlyList<ISesionEventsPublisher> _publishers;
    private readonly ILogger<CompositeSesionEventsPublisher> _logger;

    public CompositeSesionEventsPublisher(
        IEnumerable<ISesionEventsPublisher> publishers,
        ILogger<CompositeSesionEventsPublisher> logger)
    {
        _publishers = publishers.ToList();
        _logger = logger;
    }

    private async Task FanOut(Func<ISesionEventsPublisher, Task> call)
    {
        foreach (var p in _publishers)
        {
            try
            {
                await call(p);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Publicador {Publicador} falló al emitir evento de sesión", p.GetType().Name);
            }
        }
    }

    public Task PublicarPartidaPublicadaEnLobbyAsync(PartidaPublicadaEnLobbyEvent e, CancellationToken ct) => FanOut(p => p.PublicarPartidaPublicadaEnLobbyAsync(e, ct));
    public Task PublicarPartidaIniciadaAsync(PartidaIniciadaEvent e, CancellationToken ct) => FanOut(p => p.PublicarPartidaIniciadaAsync(e, ct));
    public Task PublicarJuegoActivadoAsync(JuegoActivadoEvent e, CancellationToken ct) => FanOut(p => p.PublicarJuegoActivadoAsync(e, ct));
    public Task PublicarPartidaCanceladaAsync(PartidaCanceladaEvent e, CancellationToken ct) => FanOut(p => p.PublicarPartidaCanceladaAsync(e, ct));
    public Task PublicarPartidaFinalizadaAsync(PartidaFinalizadaEvent e, CancellationToken ct) => FanOut(p => p.PublicarPartidaFinalizadaAsync(e, ct));
    public Task PublicarRespuestaTriviaValidadaAsync(RespuestaTriviaValidadaEvent e, CancellationToken ct) => FanOut(p => p.PublicarRespuestaTriviaValidadaAsync(e, ct));
    public Task PublicarPuntajeTriviaIncrementadoAsync(PuntajeTriviaIncrementadoEvent e, CancellationToken ct) => FanOut(p => p.PublicarPuntajeTriviaIncrementadoAsync(e, ct));
    public Task PublicarPreguntaTriviaActivadaAsync(PreguntaTriviaActivadaEvent e, CancellationToken ct) => FanOut(p => p.PublicarPreguntaTriviaActivadaAsync(e, ct));
    public Task PublicarPreguntaTriviaCerradaAsync(PreguntaTriviaCerradaEvent e, CancellationToken ct) => FanOut(p => p.PublicarPreguntaTriviaCerradaAsync(e, ct));
    public Task PublicarTesoroQRValidadoAsync(TesoroQRValidadoEvent e, CancellationToken ct) => FanOut(p => p.PublicarTesoroQRValidadoAsync(e, ct));
    public Task PublicarEtapaBDTGanadaAsync(EtapaBDTGanadaEvent e, CancellationToken ct) => FanOut(p => p.PublicarEtapaBDTGanadaAsync(e, ct));
    public Task PublicarEtapaBDTCerradaAsync(EtapaBDTCerradaEvent e, CancellationToken ct) => FanOut(p => p.PublicarEtapaBDTCerradaAsync(e, ct));
    public Task PublicarEtapaBDTActivadaAsync(EtapaBDTActivadaEvent e, CancellationToken ct) => FanOut(p => p.PublicarEtapaBDTActivadaAsync(e, ct));
}
```

- [ ] **Step 4: Correr el test y verlo pasar.**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj" --filter "FullyQualifiedName~CompositeSesionEventsPublisherTests"`
Expected: PASS (3 tests).

- [ ] **Step 5: Commit.**

```bash
git add \
  services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Services/CompositeSesionEventsPublisher.cs \
  services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Infrastructure/CompositeSesionEventsPublisherTests.cs
git commit -m "SP-3f-2 T3: CompositeSesionEventsPublisher (fan-out resiliente, re-throw OCE)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 4: Wiring del servicio (SignalR + composite + JWT WS + MapHub)

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Program.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/RealtimeWiringTests.cs`

**Interfaces:**
- Consumes: `SignalRSesionEventsPublisher` (T2), `CompositeSesionEventsPublisher` (T3), `NoOpSesionEventsPublisher` (existente), `SesionHub` (T1), `ISesionEventsPublisher`.
- Produces: host con `ISesionEventsPublisher` resuelto como `CompositeSesionEventsPublisher`; hub mapeado en `hubs/sesion`.

- [ ] **Step 1: Escribir el test de wiring que falla.**

`RealtimeWiringTests.cs`:
```csharp
using Microsoft.Extensions.DependencyInjection;
using Umbral.OperacionesSesion.Application.Interfaces;
using Umbral.OperacionesSesion.Infrastructure.Services;
using Xunit;

namespace Umbral.OperacionesSesion.ContractTests;

public class RealtimeWiringTests : IClassFixture<OperacionesSesionWebFactory>
{
    private readonly OperacionesSesionWebFactory _factory;
    public RealtimeWiringTests(OperacionesSesionWebFactory factory) => _factory = factory;

    [Fact]
    public void ISesionEventsPublisher_se_resuelve_como_composite()
    {
        using var scope = _factory.Services.CreateScope();
        var pub = scope.ServiceProvider.GetRequiredService<ISesionEventsPublisher>();
        Assert.IsType<CompositeSesionEventsPublisher>(pub);
    }
}
```

> `OperacionesSesionWebFactory` ya existe en el proyecto ContractTests (usada por los e2e de SP-3e). Construye el host con el `Program.cs` real, así prueba el wiring de DI de producción.

- [ ] **Step 2: Correr el test para verlo fallar.**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/Umbral.OperacionesSesion.ContractTests.csproj" --filter "FullyQualifiedName~RealtimeWiringTests"`
Expected: FAIL — resuelve `NoOpSesionEventsPublisher`, no `CompositeSesionEventsPublisher`.

- [ ] **Step 3: Modificar `Program.cs` — DI del composite + SignalR.**

Añadir el using al inicio (tras los usings existentes):
```csharp
using Umbral.OperacionesSesion.Application.Interfaces;
```

Tras la línea `builder.Services.AddHostedService<...MantenimientoSesionesWorker>();` (antes de `AddControllers`), insertar:
```csharp
builder.Services.AddSignalR();
builder.Services.AddScoped<Umbral.OperacionesSesion.Infrastructure.Services.NoOpSesionEventsPublisher>();
builder.Services.AddScoped<Umbral.OperacionesSesion.Api.Realtime.SignalRSesionEventsPublisher>();
builder.Services.AddScoped<ISesionEventsPublisher>(sp =>
    new Umbral.OperacionesSesion.Infrastructure.Services.CompositeSesionEventsPublisher(
        new ISesionEventsPublisher[]
        {
            sp.GetRequiredService<Umbral.OperacionesSesion.Infrastructure.Services.NoOpSesionEventsPublisher>(),
            sp.GetRequiredService<Umbral.OperacionesSesion.Api.Realtime.SignalRSesionEventsPublisher>(),
        },
        sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Umbral.OperacionesSesion.Infrastructure.Services.CompositeSesionEventsPublisher>>()));
```

> Esta registración del interface (scoped, factory) se hace DESPUÉS de `AddOperacionesSesionInfrastructure`, así sobreescribe el `AddScoped<ISesionEventsPublisher, NoOpSesionEventsPublisher>` de Infrastructure (last-wins). Los handlers siguen inyectando `ISesionEventsPublisher` → obtienen el composite.

- [ ] **Step 4: Modificar `Program.cs` — JWT `access_token` por query en la rama configurada.**

Dentro del bloque `.AddJwtBearer(options => { ... })` (la rama `if (...keycloak configurado...)`), tras asignar `options.TokenValidationParameters = ...`, añadir:
```csharp
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/sesion"))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                }
            };
```

> `JwtBearerEvents` ya está disponible vía el using existente `Microsoft.AspNetCore.Authentication.JwtBearer`. Ruta `/hubs/sesion` = el hub hosteado service-local.

- [ ] **Step 5: Modificar `Program.cs` — `MapHub`.**

Tras `app.MapControllers();` añadir:
```csharp
app.MapHub<Umbral.OperacionesSesion.Api.Realtime.SesionHub>("hubs/sesion");
```

- [ ] **Step 6: Correr el test de wiring + suite completa.**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/Umbral.OperacionesSesion.ContractTests.csproj" --filter "FullyQualifiedName~RealtimeWiringTests"`
Expected: PASS.

Run (regresión completa): `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj"` y `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/Umbral.OperacionesSesion.ContractTests.csproj"` y `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests/Umbral.OperacionesSesion.IntegrationTests.csproj"`
Expected: TODO verde (el composite con SignalR corre en el host de tests; difundir a grupos vacíos es no-op, sin flake — mismo principio que el worker no-neutralizado).

- [ ] **Step 7: Commit.**

```bash
git add \
  services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Program.cs \
  services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/RealtimeWiringTests.cs
git commit -m "SP-3f-2 T4: wiring SignalR (composite NoOp+SignalR, JWT access_token query, MapHub hubs/sesion)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 5: Gateway — JWT `access_token` por query + ruta del hub protegida

**Files:**
- Modify: `gateway/src/Umbral.Gateway/Security/KeycloakJwtExtensions.cs`
- Test: `gateway/tests/Umbral.Gateway.IntegrationTests/GatewayEndpointsTests.cs` (+1 fact)

**Interfaces:**
- Consumes: `JwtBearerEvents` (ya en uso en el archivo).
- Produces: lectura de `access_token` query para la ruta `/operaciones-sesion/hubs`; ruta del hub heredando la policy autenticada.

- [ ] **Step 1: Escribir el test que falla.**

Añadir a `GatewayEndpointsTests.cs` (mismo estilo que los facts existentes; usa el `WebApplicationFactory<Program>`/`HttpClient` ya presentes en la clase):
```csharp
    [Fact]
    public async Task Hub_de_operaciones_requiere_autenticacion()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/operaciones-sesion/hubs/sesion");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }
```
> Ajustar `_factory` / construcción del client al patrón exacto ya usado por los otros facts de este archivo (p. ej. si usan `CreateClient()` directo o un fixture). No introducir un fixture nuevo.

- [ ] **Step 2: Correr el test.**

Run: `dotnet test "gateway/tests/Umbral.Gateway.IntegrationTests/Umbral.Gateway.IntegrationTests.csproj" --filter "FullyQualifiedName~Hub_de_operaciones_requiere_autenticacion"`
Expected: Probablemente PASS ya (la ruta catch-all `/operaciones-sesion/{**}` con policy `Default` 401ea anónimos). Si PASA, confirma que la ruta del hub queda protegida sin cambios de routing — el valor del test es de regresión. Si FALLA, revisar que la ruta exista; no es esperable que falle.

- [ ] **Step 3: Añadir lectura de `access_token` query (handshake WS) en la rama configurada.**

En `KeycloakJwtExtensions.cs`, dentro del `options.Events = new JwtBearerEvents { ... }` existente, añadir `OnMessageReceived` junto a `OnTokenValidated`:
```csharp
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = ctx =>
                    {
                        var accessToken = ctx.Request.Query["access_token"];
                        var path = ctx.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/operaciones-sesion/hubs"))
                        {
                            ctx.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = ctx =>
                    {
                        if (ctx.Principal?.Identity is ClaimsIdentity identity)
                        {
                            KeycloakRoleClaims.AddRolesFromKeycloakClaims(identity);
                        }
                        return Task.CompletedTask;
                    }
                };
```
> Ruta gateway-facing `/operaciones-sesion/hubs` (el gateway reenvía el path completo). La rama sin realm (línea 22, `AddJwtBearer()`) no necesita cambio: en tests offline el fallback policy 401ea anónimos.

- [ ] **Step 4: Correr el test + suite del gateway.**

Run: `dotnet test "gateway/tests/Umbral.Gateway.IntegrationTests/Umbral.Gateway.IntegrationTests.csproj"`
Expected: TODO verde.

- [ ] **Step 5: Commit.**

```bash
git add \
  gateway/src/Umbral.Gateway/Security/KeycloakJwtExtensions.cs \
  gateway/tests/Umbral.Gateway.IntegrationTests/GatewayEndpointsTests.cs
git commit -m "SP-3f-2 T5: gateway lee access_token query para el handshake WS del hub de operaciones

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 6: Contrato (Realtime/SignalR) + test de contrato + traceability

**Files:**
- Modify: `contracts/http/operaciones-sesion-api.md`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/RealtimeContractTests.cs`
- Modify (CARVE-OUT, NO commitear): `docs/04-sdd/traceability-matrix.md`

**Interfaces:**
- Consumes: `SesionRealtimeMessages` (constantes, T1).
- Produces: documentación del hub + test que asevera doc↔constantes.

- [ ] **Step 1: Escribir la sección de contrato.**

Añadir al final de `contracts/http/operaciones-sesion-api.md` una sección nueva:
```markdown

## Realtime / SignalR (SP-3f-2)

Hub: `GET /operaciones-sesion/hubs/sesion` (WebSocket vía gateway YARP; passthrough automático). Auth: JWT obligatorio; en el handshake WebSocket el token viaja por query `access_token` (lo leen gateway y servicio). Grupo por partida.

Cliente → servidor:
- `SuscribirAPartida(partidaId)` — el llamante debe ser `Operador` o estar inscrito en la partida; une la conexión al grupo `partida:{id}`. Rechaza con error de hub en caso contrario.
- `DesuscribirDePartida(partidaId)` — saca la conexión del grupo.

Servidor → cliente (payloads delgados; el contenido se trae por pull `GET /pregunta-actual` / `GET /etapa-actual` / `GET /mi-sesion`):

| Mensaje | Payload |
|---|---|
| `PartidaEnLobby` | `{ partidaId }` |
| `PartidaIniciada` | `{ partidaId }` |
| `JuegoActivado` | `{ partidaId, juegoId, orden, tipoJuego }` |
| `PartidaCancelada` | `{ partidaId, motivo }` |
| `PartidaFinalizada` | `{ partidaId }` |
| `PreguntaActivada` | `{ partidaId, juegoId, preguntaId, orden, fechaLimiteUtc }` |
| `PreguntaCerrada` | `{ partidaId, juegoId, preguntaId }` |
| `EtapaActivada` | `{ partidaId, juegoId, etapaId, orden, fechaLimiteUtc }` |
| `EtapaCerrada` | `{ partidaId, juegoId, etapaId }` |
| `EtapaGanada` | `{ partidaId, juegoId, etapaId }` |

Notas: `fechaLimiteUtc` = activación + tiempo límite (cuenta regresiva local en el cliente). Los payloads nunca llevan puntos acumulados ni ranking (eso es Puntuaciones/SP-4) ni texto de preguntas/opciones/QR (anti-leak). Eventos per-participante/scoring-adjacentes (`RespuestaTriviaValidada`, `PuntajeTriviaIncrementado`, `TesoroQRValidado`) NO se difunden en este slice; su efecto sale por las transiciones de estado. El push se dispara in-process desde Operaciones (no requiere el broker RabbitMQ).
```

- [ ] **Step 2: Escribir el test de contrato que falla.**

`RealtimeContractTests.cs`:
```csharp
using System.IO;
using Umbral.OperacionesSesion.Api.Realtime;
using Xunit;

namespace Umbral.OperacionesSesion.ContractTests;

public class RealtimeContractTests
{
    private static string LeerContrato()
    {
        // Subir desde el bin de test hasta la raíz del repo y abrir el contrato.
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "contracts", "http", "operaciones-sesion-api.md")))
        {
            dir = dir.Parent;
        }
        Assert.NotNull(dir);
        return File.ReadAllText(Path.Combine(dir!.FullName, "contracts", "http", "operaciones-sesion-api.md"));
    }

    [Theory]
    [InlineData(nameof(SesionRealtimeMessages.PartidaEnLobby))]
    [InlineData(nameof(SesionRealtimeMessages.PartidaIniciada))]
    [InlineData(nameof(SesionRealtimeMessages.JuegoActivado))]
    [InlineData(nameof(SesionRealtimeMessages.PartidaCancelada))]
    [InlineData(nameof(SesionRealtimeMessages.PartidaFinalizada))]
    [InlineData(nameof(SesionRealtimeMessages.PreguntaActivada))]
    [InlineData(nameof(SesionRealtimeMessages.PreguntaCerrada))]
    [InlineData(nameof(SesionRealtimeMessages.EtapaActivada))]
    [InlineData(nameof(SesionRealtimeMessages.EtapaCerrada))]
    [InlineData(nameof(SesionRealtimeMessages.EtapaGanada))]
    public void Cada_mensaje_del_codigo_esta_documentado(string mensaje)
    {
        var contrato = LeerContrato();
        Assert.Contains(mensaje, contrato);
    }

    [Fact]
    public void El_hub_esta_documentado()
    {
        var contrato = LeerContrato();
        Assert.Contains("/operaciones-sesion/hubs/sesion", contrato);
        Assert.Contains("access_token", contrato);
    }
}
```

- [ ] **Step 3: Correr el test.**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/Umbral.OperacionesSesion.ContractTests.csproj" --filter "FullyQualifiedName~RealtimeContractTests"`
Expected: PASS (la sección de contrato ya se escribió en Step 1; 11 casos).

> Si el descubrimiento de la raíz del repo falla en el runner, ajustar `LeerContrato` al patrón que ya usen otros tests de contrato para localizar archivos del repo. No hardcodear rutas absolutas.

- [ ] **Step 4: Escribir la fila de traceability (NO commitear).**

Añadir a `docs/04-sdd/traceability-matrix.md`, como nueva fila tras la fila SP-3f-1 (7 columnas, mismo formato que las vecinas):
```markdown
| Push tiempo real SignalR (SP-3f-2) | Hub SignalR (SesionHub) en Operaciones de Sesión que difunde por grupo `partida:{id}` las transiciones runtime (lobby/iniciada/juego/pregunta/etapa activada+cerrada+ganada/cancelada/finalizada) colgándose del puerto ISesionEventsPublisher vía composite NoOp+SignalR (cero churn de handlers); deadline-timestamp para timers; auth WS por access_token query (servicio+gateway); payloads delgados anti-leak | Operaciones de Sesión | Gateway (passthrough WS + JWT query) | docs/superpowers/specs/2026-06-30-sp3f2-push-tiempo-real-signalr-design.md · docs/superpowers/plans/2026-06-30-sp3f2-push-tiempo-real-signalr.md | contracts/http/operaciones-sesion-api.md | Implemented — suite verde. **Diferido:** ranking en vivo→Puntuaciones SP-4, cableado clientes web/móvil→follow-up, broker RabbitMQ real→slice propio (se sumará como otra impl del composite), targeting por participante, pistas+geolocalización→sub-slices SP-3f, Equipo→slice-E. **Gap documentado:** camino WS-a-través-de-YARP no integration-testeado (verificado por unit+contract); desajuste de prefijo gateway↔operaciones es deuda pre-existente que afecta a todos los endpoints por igual. |
```

- [ ] **Step 5: Correr la suite completa de Operaciones.**

Run: `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj"` · `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/Umbral.OperacionesSesion.ContractTests.csproj"` · `dotnet test "services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests/Umbral.OperacionesSesion.IntegrationTests.csproj"`
Expected: TODO verde.

- [ ] **Step 6: Commit (SOLO el contrato + el test; la traceability queda sin commitear).**

```bash
git add \
  contracts/http/operaciones-sesion-api.md \
  services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/RealtimeContractTests.cs
git commit -m "SP-3f-2 T6: contrato Realtime/SignalR del hub de operaciones + test doc↔constantes

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

- [ ] **Step 7: Verificar el carve-out post-commit.**

Run: `git status --short`
Expected: siguen sin commitear SOLO `docs/04-sdd/traceability-matrix.md` (M), `docs/superpowers/specs/2026-06-27-sp3c-runtime-trivia-design.md` (M), `docs/04-sdd/auditorias/` (??). Nada más staged.

---

## Self-Review

**1. Spec coverage:**
- Hub + suscripción por grupo `partida:{id}` → T1. ✓
- Broadcaster sobre el puerto (10 difunden / 3 no-op) → T2. ✓
- Composite fan-out NoOp+SignalR → T3. ✓
- Deadline-timestamp (timers) → T2 (derivación) + contrato T6. ✓
- Auth WS `access_token` query (servicio + gateway) → T4 + T5. ✓
- Contrato Realtime/SignalR + test → T6. ✓
- Carve-out traceability → T6 Step 4/7. ✓
- Fuera de alcance (ranking/clientes/broker/targeting/pistas/geoloc/Equipo) → documentado en traceability T6. ✓
- Decisión: extensión de eventos NO necesaria (deadline derivado; payloads delgados) → "Refinamiento del plan vs spec". ✓

**2. Placeholder scan:** sin TBD/TODO; todo paso con código real. Las notas tipo "ajustar al patrón existente" apuntan a verificación de firmas reales (anti-invención), no son huecos de implementación. ✓

**3. Type consistency:** `SesionRealtimeMessages.GrupoPartida` usado idéntico en T1/T2; nombres de mensaje vía `nameof` (constante == literal) consumidos en T6; payloads de T1 consumidos en T2; `ISesionEventsPublisher` (13 métodos exactos del puerto leído) implementado completo en T2 y T3 y registrado en T4; `GetByParticipanteActivoAsync(Guid, CancellationToken)` y `SesionPartida.PartidaId` consistentes con el código real. ✓

## Execution Handoff

Plan completo y guardado en `docs/superpowers/plans/2026-06-30-sp3f2-push-tiempo-real-signalr.md`. Dos opciones de ejecución:

1. **Subagent-Driven (recomendada)** — un subagente fresco por tarea + revisión de dos etapas (spec-conformance + calidad) entre tareas, commit y línea de ledger por tarea.
2. **Inline (executing-plans)** — ejecución por lotes con checkpoints en esta sesión.

¿Cuál?
