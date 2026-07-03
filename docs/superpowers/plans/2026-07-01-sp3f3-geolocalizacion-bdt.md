# SP-3f-3 — Geolocalización BDT (relay al operador) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Añadir un canal SignalR por el que el participante emite su ubicación (~cada 2s) y el servidor la reenvía en tiempo real solo al operador de esa partida.

**Architecture:** Relay puro sobre el `SesionHub` existente (SP-3f-2, alcanzable vía gateway tras SP-3g). Nuevo método client→servidor `EnviarUbicacion(latitud, longitud)`; la identidad (partidaId+participanteId) se toma de `Context.Items` poblado en `SuscribirAPartida` (sin lectura DB por tick → RNF-15); difusión servidor→cliente `UbicacionActualizada` a un grupo operador-scoped `operador:partida:{id}` al que solo se auto-añaden los operadores (BR-B07: solo el operador ve el mapa). Sin persistencia, sin evento de dominio, sin cambio de Domain/Application/Infrastructure.

**Tech Stack:** .NET 8, ASP.NET Core SignalR, xUnit, fakes a mano (sin Moq).

## Global Constraints

- **Backend-only, servicio Operaciones de Sesión.** Todo el cambio de producción vive en `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/` (+ el contrato y traceability). Cero cambios en Domain/Application/Infrastructure.
- **Relay puro:** sin persistencia, sin escritura DB, sin evento de dominio, sin tocar entidades. Estado transitorio de conexión únicamente.
- **Privacidad (BR-B07):** `UbicacionActualizada` se difunde SOLO al grupo `operador:partida:{id}`. El participante emisor NO está en ese grupo. Nunca difundir ubicación a `partida:{id}`.
- **Sin lectura DB por tick (RNF-15):** `EnviarUbicacion` lee identidad de `Context.Items` (memoria de conexión), nunca del repositorio.
- **Timestamp server-stamped** vía `TimeProvider` inyectado (ya registrado en DI: `Application/DependencyInjection.cs:14` `AddSingleton(TimeProvider.System)`; Program.cs llama `AddOperacionesSesionApplication()`). **No** registrar DI nueva.
- **Fakes a mano, sin Moq.** Reusar `FakeTimeProvider` (`tests/…/UnitTests/Application/Fakes/FakeTimeProvider.cs`, namespace `Umbral.OperacionesSesion.UnitTests.Application.Fakes`, ya importado por `SesionHubTests`).
- **Git carve-out (estricto):** dejar SIEMPRE sin commitear `docs/04-sdd/traceability-matrix.md`, `docs/superpowers/specs/2026-06-27-sp3c-runtime-trivia-design.md`, `docs/04-sdd/auditorias/`. Las tareas ESCRIBEN la fila de traceability pero NO la commitean. **Nunca** `git add -A` / `git add .` / `git add docs/`. Stagear SOLO los archivos exactos nombrados en cada Step de commit.
- **Prohibido** a los implementers: `git checkout` / `restore` / `clean` / `stash` / `reset` amplios (un archivo se perdió así antes).
- **Cada commit** termina con exactamente esta línea (sin línea de Claude-Session):
  ```
  Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
  ```
- **Modelo por tarea:** Task 1 → haiku (mecánico, declaraciones); Task 2 → sonnet (lógica de hub + tests multi-archivo); Task 3 → sonnet (contrato + test + traceability).
- **Comando de test del servicio:**
  ```bash
  dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj
  dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/Umbral.OperacionesSesion.ContractTests.csproj
  ```

---

## File Structure

- `…/Api/Realtime/SesionRealtimeMessages.cs` — **modify**: const `UbicacionActualizada` + helper `GrupoOperadorPartida`.
- `…/Api/Realtime/SesionRealtimePayloads.cs` — **modify**: record `UbicacionParticipantePayload`.
- `…/Api/Realtime/SesionHub.cs` — **modify**: ctor `TimeProvider`, `Context.Items` en `SuscribirAPartida`, auto-join/remoción del grupo operador, nuevo `EnviarUbicacion`.
- `tests/…/UnitTests/Api/Realtime/SesionRealtimeMessagesTests.cs` — **create**: locka el formato del grupo operador (contrato de wire que los clientes hardcodean).
- `tests/…/UnitTests/Api/Realtime/SesionHubTests.cs` — **modify**: `FakeHubCallerContext.Items` real, `FakeClients`, `Construir` inyecta `TimeProvider`+`Clients`, nuevos tests.
- `contracts/http/operaciones-sesion-api.md` — **modify**: sección Realtime (client→server + server→client + notas).
- `tests/…/ContractTests/RealtimeContractTests.cs` — **modify**: `InlineData` para `UbicacionActualizada`.
- `docs/04-sdd/traceability-matrix.md` — **modify (NO commit)**: fila SP-3f-3.

---

## Task 1: Consts de mensaje/grupo + payload de ubicación

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SesionRealtimeMessages.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SesionRealtimePayloads.cs`
- Test (create): `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/Realtime/SesionRealtimeMessagesTests.cs`

**Interfaces:**
- Produces:
  - `SesionRealtimeMessages.UbicacionActualizada` (string const, valor `"UbicacionActualizada"`).
  - `SesionRealtimeMessages.GrupoOperadorPartida(Guid partidaId) => $"operador:partida:{partidaId}"`.
  - `record UbicacionParticipantePayload(Guid PartidaId, Guid ParticipanteId, double Latitud, double Longitud, DateTime TimestampUtc)`.

- [ ] **Step 1: Escribir el test que falla** (locka el formato del grupo operador — string que los clientes web/móvil hardcodean)

Crear `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/Realtime/SesionRealtimeMessagesTests.cs`:

```csharp
using System;
using Umbral.OperacionesSesion.Api.Realtime;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Api.Realtime;

public class SesionRealtimeMessagesTests
{
    [Fact]
    public void GrupoOperadorPartida_tiene_formato_estable()
    {
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Assert.Equal("operador:partida:11111111-1111-1111-1111-111111111111",
            SesionRealtimeMessages.GrupoOperadorPartida(id));
    }

    [Fact]
    public void GrupoOperadorPartida_difiere_del_grupo_de_partida()
    {
        var id = Guid.NewGuid();
        Assert.NotEqual(SesionRealtimeMessages.GrupoPartida(id),
            SesionRealtimeMessages.GrupoOperadorPartida(id));
    }
}
```

- [ ] **Step 2: Correr el test para verificar que falla**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`
Expected: FALLA a compilar — `'SesionRealtimeMessages' does not contain a definition for 'GrupoOperadorPartida'`.

- [ ] **Step 3: Añadir const + helper**

En `SesionRealtimeMessages.cs`, tras la línea `public const string EtapaGanada = nameof(EtapaGanada);` añadir la const, y tras `GrupoPartida` añadir el helper:

```csharp
    public const string UbicacionActualizada = nameof(UbicacionActualizada);

    public static string GrupoPartida(Guid partidaId) => $"partida:{partidaId}";
    public static string GrupoOperadorPartida(Guid partidaId) => $"operador:partida:{partidaId}";
```

(La const `UbicacionActualizada` va junto a las otras consts; el helper `GrupoOperadorPartida` junto a `GrupoPartida`. No dupliques `GrupoPartida` — el bloque de arriba muestra ambos helpers juntos para contexto.)

- [ ] **Step 4: Añadir el record de payload**

En `SesionRealtimePayloads.cs`, al final (tras `EtapaGanadaPayload`):

```csharp
public sealed record UbicacionParticipantePayload(Guid PartidaId, Guid ParticipanteId, double Latitud, double Longitud, DateTime TimestampUtc);
```

- [ ] **Step 5: Correr el test para verificar que pasa**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`
Expected: PASA (incluye los 2 nuevos + todos los previos verdes).

- [ ] **Step 6: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SesionRealtimeMessages.cs \
        services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SesionRealtimePayloads.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/Realtime/SesionRealtimeMessagesTests.cs
git commit -m "$(cat <<'EOF'
SP-3f-3 T1: const UbicacionActualizada + grupo operador-scoped + payload de ubicación

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
)"
```

---

## Task 2: Hub — Context.Items, grupo operador y EnviarUbicacion

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SesionHub.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/Realtime/SesionHubTests.cs`

**Interfaces:**
- Consumes (de Task 1): `SesionRealtimeMessages.GrupoOperadorPartida(Guid)`, `SesionRealtimeMessages.UbicacionActualizada`, `UbicacionParticipantePayload`.
- Consumes (existente): `ISesionPartidaRepository.GetByParticipanteActivoAsync`, `TimeProvider` (DI).
- Produces (de este task): `SesionHub(ISesionPartidaRepository repo, TimeProvider timeProvider)` ctor; `Task EnviarUbicacion(double latitud, double longitud)`.

**Nota DI:** el hub gana un segundo parámetro de ctor `TimeProvider`. Ya es resoluble (registrado en Application DI). No añadir registros.

- [ ] **Step 1: Preparar los fakes de test (Items real + FakeClients) y ajustar `Construir`**

En `SesionHubTests.cs`:

1. Cambiar `FakeHubCallerContext.Items` de `throw new NotImplementedException()` a un diccionario real:

```csharp
    private sealed class FakeHubCallerContext : HubCallerContext
    {
        private readonly ClaimsPrincipal _user;
        private readonly string _connId;
        private readonly Dictionary<object, object?> _items = new();
        public FakeHubCallerContext(ClaimsPrincipal user, string connId) { _user = user; _connId = connId; }
        public override string ConnectionId => _connId;
        public override string? UserIdentifier => null;
        public override ClaimsPrincipal? User => _user;
        public override IDictionary<object, object?> Items => _items;
        public override Microsoft.AspNetCore.Http.Features.IFeatureCollection Features => throw new NotImplementedException();
        public override CancellationToken ConnectionAborted => CancellationToken.None;
        public override void Abort() => throw new NotImplementedException();
    }
```

2. Añadir un fake de clientes que captura los `SendAsync` por grupo (al final, junto a los otros fakes locales):

```csharp
    private sealed class FakeClientProxy : IClientProxy
    {
        public List<(string Method, object?[] Args)> Sent { get; } = new();
        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
        { Sent.Add((method, args)); return Task.CompletedTask; }
    }

    private sealed class FakeClients : IHubCallerClients
    {
        public Dictionary<string, FakeClientProxy> Grupos { get; } = new();
        public IClientProxy Group(string groupName)
        {
            if (!Grupos.TryGetValue(groupName, out var p)) { p = new FakeClientProxy(); Grupos[groupName] = p; }
            return p;
        }
        public IClientProxy All => throw new NotImplementedException();
        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => throw new NotImplementedException();
        public IClientProxy Client(string connectionId) => throw new NotImplementedException();
        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => throw new NotImplementedException();
        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => throw new NotImplementedException();
        public IClientProxy Groups(IReadOnlyList<string> groupNames) => throw new NotImplementedException();
        public IClientProxy OthersInGroup(string groupName) => throw new NotImplementedException();
        public IClientProxy User(string userId) => throw new NotImplementedException();
        public IClientProxy Users(IReadOnlyList<string> userIds) => throw new NotImplementedException();
        public IClientProxy Caller => throw new NotImplementedException();
        public IClientProxy Others => throw new NotImplementedException();
    }
```

3. Ajustar `Construir` para inyectar `TimeProvider` y exponer los `FakeClients` (nuevo parámetro opcional con default para no romper las llamadas existentes):

```csharp
    private static readonly DateTime T0 = new(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc);

    private static SesionHub Construir(ISesionPartidaRepositorioFake repo, ClaimsPrincipal user,
        FakeGroupManager groups, FakeClients? clients = null, string connId = "c1")
    {
        var hub = new SesionHub(repo.Repo, new FakeTimeProvider(T0))
        {
            Context = new FakeHubCallerContext(user, connId),
            Groups = groups,
            Clients = clients ?? new FakeClients()
        };
        return hub;
    }
```

`FakeTimeProvider` ya está importado vía `using Umbral.OperacionesSesion.UnitTests.Application.Fakes;` (presente en el archivo). Añadir `using System.Collections.Generic;` ya está presente. Todas las llamadas existentes a `Construir(repo, user, groups)` siguen compilando (el nuevo parámetro tiene default).

- [ ] **Step 2: Escribir los tests que fallan (comportamiento nuevo del hub)**

Añadir estos `[Fact]`/`[Theory]` a `SesionHubTests` (antes del bloque `// ---- Fakes locales ----`):

```csharp
    [Fact]
    public async Task Operador_tambien_se_une_al_grupo_operador()
    {
        var partidaId = Guid.NewGuid();
        var repo = new ISesionPartidaRepositorioFake();
        var groups = new FakeGroupManager();
        var hub = Construir(repo, Usuario(sub: null, rol: "Operador"), groups);

        await hub.SuscribirAPartida(partidaId);

        Assert.Contains(("c1", SesionRealtimeMessages.GrupoPartida(partidaId)), groups.Added);
        Assert.Contains(("c1", SesionRealtimeMessages.GrupoOperadorPartida(partidaId)), groups.Added);
    }

    [Fact]
    public async Task Inscrito_no_se_une_al_grupo_operador()
    {
        var partidaId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();
        var repo = new ISesionPartidaRepositorioFake();
        repo.Inner.Add(SesionDe(partidaId, participanteId));
        var groups = new FakeGroupManager();
        var hub = Construir(repo, Usuario(sub: participanteId.ToString(), rol: "Participante"), groups);

        await hub.SuscribirAPartida(partidaId);

        Assert.Contains(("c1", SesionRealtimeMessages.GrupoPartida(partidaId)), groups.Added);
        Assert.DoesNotContain(("c1", SesionRealtimeMessages.GrupoOperadorPartida(partidaId)), groups.Added);
    }

    [Fact]
    public async Task EnviarUbicacion_difunde_al_grupo_operador_con_payload()
    {
        var partidaId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();
        var repo = new ISesionPartidaRepositorioFake();
        repo.Inner.Add(SesionDe(partidaId, participanteId));
        var groups = new FakeGroupManager();
        var clients = new FakeClients();
        var hub = Construir(repo, Usuario(sub: participanteId.ToString(), rol: "Participante"), groups, clients);
        await hub.SuscribirAPartida(partidaId);

        await hub.EnviarUbicacion(10.5, -66.9);

        var proxy = clients.Grupos[SesionRealtimeMessages.GrupoOperadorPartida(partidaId)];
        var (metodo, args) = Assert.Single(proxy.Sent);
        Assert.Equal(SesionRealtimeMessages.UbicacionActualizada, metodo);
        var payload = Assert.IsType<UbicacionParticipantePayload>(args[0]);
        Assert.Equal(partidaId, payload.PartidaId);
        Assert.Equal(participanteId, payload.ParticipanteId);
        Assert.Equal(10.5, payload.Latitud);
        Assert.Equal(-66.9, payload.Longitud);
        Assert.Equal(new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc), payload.TimestampUtc);
    }

    [Theory]
    [InlineData(91, 0)]
    [InlineData(-91, 0)]
    [InlineData(0, 181)]
    [InlineData(0, -181)]
    public async Task EnviarUbicacion_coordenadas_fuera_de_rango_lanza_y_no_difunde(double lat, double lng)
    {
        var partidaId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();
        var repo = new ISesionPartidaRepositorioFake();
        repo.Inner.Add(SesionDe(partidaId, participanteId));
        var groups = new FakeGroupManager();
        var clients = new FakeClients();
        var hub = Construir(repo, Usuario(sub: participanteId.ToString(), rol: "Participante"), groups, clients);
        await hub.SuscribirAPartida(partidaId);

        await Assert.ThrowsAsync<HubException>(() => hub.EnviarUbicacion(lat, lng));
        Assert.Empty(clients.Grupos);
    }

    [Fact]
    public async Task EnviarUbicacion_sin_suscripcion_lanza()
    {
        var repo = new ISesionPartidaRepositorioFake();
        var groups = new FakeGroupManager();
        var clients = new FakeClients();
        var hub = Construir(repo, Usuario(sub: Guid.NewGuid().ToString(), rol: "Participante"), groups, clients);

        await Assert.ThrowsAsync<HubException>(() => hub.EnviarUbicacion(1.0, 1.0));
        Assert.Empty(clients.Grupos);
    }

    [Fact]
    public async Task EnviarUbicacion_operador_lanza()
    {
        var partidaId = Guid.NewGuid();
        var repo = new ISesionPartidaRepositorioFake();
        var groups = new FakeGroupManager();
        var clients = new FakeClients();
        var hub = Construir(repo, Usuario(sub: null, rol: "Operador"), groups, clients);
        await hub.SuscribirAPartida(partidaId);

        await Assert.ThrowsAsync<HubException>(() => hub.EnviarUbicacion(1.0, 1.0));
        Assert.Empty(clients.Grupos);
    }
```

- [ ] **Step 3: Correr los tests para verificar que fallan**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`
Expected: FALLA a compilar — `SesionHub` no define `EnviarUbicacion` y el ctor de un solo parámetro ya no existe tras el Step 4 (o, antes del Step 4, falla porque `EnviarUbicacion` no existe). El error confirma el red.

- [ ] **Step 4: Implementar el hub**

Reemplazar el contenido de `SesionHub.cs` por:

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
    private const string ClavePartidaId = "partidaId";
    private const string ClaveParticipanteId = "participanteId";

    private readonly ISesionPartidaRepository _repo;
    private readonly TimeProvider _timeProvider;

    public SesionHub(ISesionPartidaRepository repo, TimeProvider timeProvider)
    {
        _repo = repo;
        _timeProvider = timeProvider;
    }

    public async Task SuscribirAPartida(Guid partidaId)
    {
        var user = Context.User;
        var esOperador = user?.IsInRole("Operador") ?? false;
        if (esOperador)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, SesionRealtimeMessages.GrupoPartida(partidaId), Context.ConnectionAborted);
            await Groups.AddToGroupAsync(Context.ConnectionId, SesionRealtimeMessages.GrupoOperadorPartida(partidaId), Context.ConnectionAborted);
            return;
        }

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

        Context.Items[ClavePartidaId] = partidaId;
        Context.Items[ClaveParticipanteId] = participanteId;
        await Groups.AddToGroupAsync(Context.ConnectionId, SesionRealtimeMessages.GrupoPartida(partidaId), Context.ConnectionAborted);
    }

    public async Task DesuscribirDePartida(Guid partidaId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, SesionRealtimeMessages.GrupoPartida(partidaId), Context.ConnectionAborted);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, SesionRealtimeMessages.GrupoOperadorPartida(partidaId), Context.ConnectionAborted);
    }

    public async Task EnviarUbicacion(double latitud, double longitud)
    {
        if (!TryObtenerSuscripcion(out var partidaId, out var participanteId))
        {
            throw new HubException("Suscríbete a la partida antes de enviar ubicación.");
        }

        if (latitud is < -90 or > 90 || longitud is < -180 or > 180)
        {
            throw new HubException("Coordenadas fuera de rango.");
        }

        var payload = new UbicacionParticipantePayload(
            partidaId, participanteId, latitud, longitud, _timeProvider.GetUtcNow().UtcDateTime);

        await Clients.Group(SesionRealtimeMessages.GrupoOperadorPartida(partidaId))
            .SendAsync(SesionRealtimeMessages.UbicacionActualizada, payload, Context.ConnectionAborted);
    }

    private bool TryObtenerSuscripcion(out Guid partidaId, out Guid participanteId)
    {
        partidaId = default;
        participanteId = default;
        if (Context.Items.TryGetValue(ClavePartidaId, out var p) && p is Guid pid &&
            Context.Items.TryGetValue(ClaveParticipanteId, out var u) && u is Guid uid)
        {
            partidaId = pid;
            participanteId = uid;
            return true;
        }
        return false;
    }
}
```

- [ ] **Step 5: Correr los tests para verificar que pasan**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`
Expected: PASA — los 6 nuevos tests + todos los previos de `SesionHubTests` (Operador/Inscrito/No_inscrito/Sin_sub/Desuscribir/otra_partida) verdes.

- [ ] **Step 6: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SesionHub.cs \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/Realtime/SesionHubTests.cs
git commit -m "$(cat <<'EOF'
SP-3f-3 T2: EnviarUbicacion relay al grupo operador + Context.Items + auto-join operador

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
)"
```

---

## Task 3: Contrato + test doc↔constantes + traceability

**Files:**
- Modify: `contracts/http/operaciones-sesion-api.md`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/RealtimeContractTests.cs`
- Modify (NO commit): `docs/04-sdd/traceability-matrix.md`

**Interfaces:**
- Consumes (de Task 1): `SesionRealtimeMessages.UbicacionActualizada`.

- [ ] **Step 1: Escribir el test que falla (doc↔constantes)**

En `RealtimeContractTests.cs`, añadir un `[InlineData]` al `Theory` `Cada_mensaje_del_codigo_esta_documentado`, tras la línea de `EtapaGanada`:

```csharp
    [InlineData(nameof(SesionRealtimeMessages.UbicacionActualizada))]
```

- [ ] **Step 2: Correr el test para verificar que falla**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/Umbral.OperacionesSesion.ContractTests.csproj`
Expected: FALLA — `Cada_mensaje_del_codigo_esta_documentado(mensaje: "UbicacionActualizada")`: el contrato no contiene `UbicacionActualizada`.

- [ ] **Step 3: Actualizar el contrato**

En `contracts/http/operaciones-sesion-api.md`, sección `## Realtime / SignalR (SP-3f-2)`:

1. En el bloque `Cliente → servidor:`, tras la línea de `DesuscribirDePartida`, añadir:

```markdown
- `EnviarUbicacion(latitud, longitud)` — **solo participante**, requiere `SuscribirAPartida` previo (la partida se toma de la conexión, no como parámetro). Valida rango (`latitud ∈ [-90,90]`, `longitud ∈ [-180,180]`); fuera de rango o sin suscripción → error de hub. Relay puro: no persiste. (SP-3f-3)
```

2. En la tabla `Servidor → cliente`, tras la fila `EtapaGanada`, añadir:

```markdown
| `UbicacionActualizada` *(operador-only)* | `{ partidaId, participanteId, latitud, longitud, timestampUtc }` |
```

3. En el párrafo `Notas:` (final de la sección), añadir al final:

```markdown
 `UbicacionActualizada` (SP-3f-3) se difunde SOLO al grupo `operador:partida:{id}` (BR-B07: únicamente el operador ve el mapa; el participante emisor no lo recibe); `timestampUtc` es server-stamped; el relay no persiste ni emite evento de dominio (audit de ubicación → broker, diferido).
```

- [ ] **Step 4: Correr el test para verificar que pasa**

Run: `dotnet test services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/Umbral.OperacionesSesion.ContractTests.csproj`
Expected: PASA — los 11 mensajes (incl. `UbicacionActualizada`) documentados; `El_hub_esta_documentado` verde.

- [ ] **Step 5: Escribir la fila de traceability (NO commit — carve-out)**

En `docs/04-sdd/traceability-matrix.md`, añadir una fila tras la de SP-3g (misma estructura de columnas que las filas SP-3f-2/SP-3g). Contenido:

```markdown
| Geolocalización BDT (SP-3f-3) | Canal SignalR de ubicación sobre SesionHub: método client→servidor EnviarUbicacion(latitud, longitud) (solo participante, partida tomada de la conexión, rango validado) reenvía UbicacionParticipantePayload solo al grupo operador-scoped `operador:partida:{id}` (BR-B07: solo el operador ve el mapa); identidad desde Context.Items poblado en SuscribirAPartida (sin lectura DB por tick → RNF-15); timestamp server-stamped (TimeProvider). Relay puro: sin persistencia, sin evento, sin cambio de dominio | Operaciones de Sesión | Gateway (passthrough WS); clientes web/móvil (cableado en follow-up) | docs/superpowers/specs/2026-07-01-sp3f3-geolocalizacion-bdt-design.md · docs/superpowers/plans/2026-07-01-sp3f3-geolocalizacion-bdt.md | contracts/http/operaciones-sesion-api.md | Implemented — suite verde. **Diferido:** persistencia/last-known + audit de ubicación (TipoEventoHistorial.Ubicacion)→broker RabbitMQ, GeolocalizacionAutorizada backend→modelo participación BDT, gate server-side de tipo-de-juego por tick (contra RNF-15), cableado clientes→follow-up, Equipo→slice-E. **Gap documentado:** relay WS end-to-end no testeable en el harness (cubierto por unit tests del hub). |
```

**IMPORTANTE:** NO stagear ni commitear `docs/04-sdd/traceability-matrix.md`. Se escribe y se deja unstaged (carve-out).

- [ ] **Step 6: Commit (SOLO contrato + test; traceability queda unstaged)**

```bash
git add contracts/http/operaciones-sesion-api.md \
        services/operaciones-sesion/tests/Umbral.OperacionesSesion.ContractTests/RealtimeContractTests.cs
git commit -m "$(cat <<'EOF'
SP-3f-3 T3: contrato Realtime += EnviarUbicacion/UbicacionActualizada (operador-only) + test doc↔constantes

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
)"
```

Tras el commit, verificar que el carve-out sigue intacto:

```bash
git status --short
# Esperado: docs/04-sdd/traceability-matrix.md (M, unstaged),
#           docs/superpowers/specs/2026-06-27-sp3c-runtime-trivia-design.md (M, unstaged),
#           docs/04-sdd/auditorias/ (??, untracked). Nada de eso commiteado.
```

---

## Self-Review (autor del plan)

**Spec coverage:**
- Método hub `EnviarUbicacion` client→servidor solo participante → Task 2. ✅
- Relay al grupo operador-scoped `operador:partida:{id}`, operadores auto-join → Task 2 (SuscribirAPartida) + Task 1 (helper). ✅
- Payload `UbicacionParticipantePayload {partidaId, participanteId, latitud, longitud, timestampUtc}` → Task 1. ✅
- Sin lectura DB por tick (Context.Items) → Task 2. ✅
- Timestamp server-stamped (TimeProvider) → Task 2. ✅
- Validación de rango de coords + no-suscrito → Task 2 (tests + impl). ✅
- Privacidad: participante no recibe (no en grupo operador) → Task 2 test `Inscrito_no_se_une_al_grupo_operador`. ✅
- Remoción del grupo operador en DesuscribirDePartida (riesgo del spec) → Task 2. ✅
- Contrato Realtime + test doc↔constantes → Task 3. ✅
- Fila traceability carve-out (no commit) → Task 3 Step 5. ✅
- Fuera de alcance (persistencia, evento audit, GeolocalizacionAutorizada, gate tipo-juego, clientes, Equipo) → no hay tareas; documentado en traceability. ✅

**Placeholder scan:** sin TODO/TBD; todo el código está completo. ✅

**Type consistency:** `EnviarUbicacion(double latitud, double longitud)`, `UbicacionParticipantePayload(Guid, Guid, double, double, DateTime)`, `GrupoOperadorPartida(Guid)`, const `UbicacionActualizada` — consistentes entre Task 1 (produce), Task 2 (consume) y Task 3 (documenta/testea). ✅
