# SP-5b — Gobernanza backend: Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Materializar BR-R02/BR-R04: gestión de permisos funcionales por rol (GET matriz + PUT set completo) y cambio de rol de usuario (nunca-admin, 409 con equipo activo), con propagación Keycloak-first vía Admin API (composites, ADR-0013) y broker RabbitMQ real en Identity (patrón SP-3i) que publica los eventos de gobernanza y rewirea los de equipos.

**Architecture:** Identity gana su backbone RabbitMQ (exchange topic durable `umbral.identity`, mapa explícito de routing keys, envelope camelCase idéntico a Operaciones, publisher best-effort estricto sobre seam `IRabbitMqPublishChannel`, registro condicional → Composite). El seam `IEquipoEventsPublisher` se renombra a `IIdentityEventsPublisher` y absorbe los 2 eventos de gobernanza. Escrituras de gobernanza: validar → Keycloak Admin API → persistir `permisos_rol`/`usuarios` → evento → 200; fallo Keycloak → 502 sin persistir. Spec: `docs/superpowers/specs/2026-07-04-sp5b-gobernanza-backend-design.md`.

**Tech Stack:** .NET 8, RabbitMQ.Client 6.8.1, EF Core + PostgreSQL (tabla `permisos_rol`), Keycloak Admin REST API, FluentValidation, xUnit + WebApplicationFactory.

## Global Constraints

- Commits terminan EXACTAMENTE con: `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>` (nada después).
- `git add` SOLO archivos exactos, uno por uno. PROHIBIDO `git add -A` / `git add .` / directorios.
- PROHIBIDO `git checkout`/`restore`/`clean`/`stash`/`reset` de rango amplio. Árbol raro → reportar.
- Código NUEVO usa `TimeProvider` (cero `DateTime.Now`/`UtcNow` nuevos en `src/`); los `DateTime.UtcNow` pre-existentes de los handlers de equipos NO se tocan (deuda heredada SP-1, fuera de alcance).
- Controllers sin lógica de negocio; controller unit tests obligatorios para acciones nuevas.
- Nombres EXACTOS case-sensitive: `GestionarPartidas`, `GestionarEquipos`, `ParticiparEnPartidas`, roles `Administrador`/`Operador`/`Participante`.
- Suites Identity al cierre de cada tarea: verdes (baseline 144 unit / 37 integration / 30 contract, crecen). Gateway baseline 14. Test pre-existente roto por cambios → arreglar la causa, NUNCA debilitar asserts.
- Orden Keycloak-antes-de-DB en toda escritura de gobernanza (E2): si el port lanza, el repo no debe haber recibido escrituras.

---

### Task 1 (G1): Broker RabbitMQ Identity — backbone + rename del publisher + eventos de gobernanza

**Files:**
- Create: `services/identity-service/src/Umbral.IdentityService.Infrastructure/Services/Messaging/RabbitMqOptions.cs`
- Create: `services/identity-service/src/Umbral.IdentityService.Infrastructure/Services/Messaging/IRabbitMqPublishChannel.cs`
- Create: `services/identity-service/src/Umbral.IdentityService.Infrastructure/Services/Messaging/RabbitMqPublishChannel.cs`
- Create: `services/identity-service/src/Umbral.IdentityService.Infrastructure/Services/Messaging/EventEnvelope.cs`
- Create: `services/identity-service/src/Umbral.IdentityService.Infrastructure/Services/Messaging/IdentityEventRouting.cs`
- Create: `services/identity-service/src/Umbral.IdentityService.Infrastructure/Services/Events/RabbitMqIdentityEventsPublisher.cs`
- Create: `services/identity-service/src/Umbral.IdentityService.Infrastructure/Services/Events/CompositeIdentityEventsPublisher.cs`
- Rename+Modify: `services/identity-service/src/Umbral.IdentityService.Application/Interfaces/IEquipoEventsPublisher.cs` → `IIdentityEventsPublisher.cs`
- Rename+Modify: `services/identity-service/src/Umbral.IdentityService.Infrastructure/Services/Events/NoOpEquipoEventsPublisher.cs` → `NoOpIdentityEventsPublisher.cs`
- Modify: `services/identity-service/src/Umbral.IdentityService.Infrastructure/DependencyInjection.cs`
- Modify: `services/identity-service/src/Umbral.IdentityService.Infrastructure/Umbral.IdentityService.Infrastructure.csproj` (PackageReference RabbitMQ.Client)
- Modify: consumidores del rename (4 handlers de equipos + fakes de test — localizar con grep)
- Create: `services/identity-service/tests/Umbral.IdentityService.UnitTests/Infrastructure/Messaging/RabbitMqIdentityEventsPublisherTests.cs`
- Create: `services/identity-service/tests/Umbral.IdentityService.IntegrationTests/RabbitMqRoundTripTests.cs`

**Interfaces:**
- Produces: `IIdentityEventsPublisher` con 6 métodos (4 equipos existentes + `PublishRolUsuarioModificadoAsync` + `PublishPermisosRolActualizadosAsync`); records `RolUsuarioModificadoIntegrationEvent(Guid UsuarioId, string RolAnterior, string RolNuevo, DateTime OccurredOnUtc)` y `PermisosRolActualizadosIntegrationEvent(string Rol, IReadOnlyList<string> Permisos, DateTime OccurredOnUtc)`. G4/G5 los consumen. `TimeProvider` registrado en DI (singleton `TimeProvider.System`).

- [ ] **Step 1: Rename de interfaz + eventos nuevos**

`git mv` del archivo de interfaz; contenido final de `IIdentityEventsPublisher.cs` (los 4 records de equipos EXISTENTES quedan idénticos — solo se muestra lo nuevo; NO tocar los records existentes):

```csharp
namespace Umbral.IdentityService.Application.Interfaces;

public interface IIdentityEventsPublisher
{
    Task PublishEquipoCreadoAsync(EquipoCreadoIntegrationEvent integrationEvent, CancellationToken cancellationToken);
    Task PublishInvitacionEquipoCreadaAsync(InvitacionEquipoCreadaIntegrationEvent integrationEvent, CancellationToken cancellationToken);
    Task PublishInvitacionEquipoAceptadaAsync(InvitacionEquipoAceptadaIntegrationEvent integrationEvent, CancellationToken cancellationToken);
    Task PublishInvitacionEquipoRechazadaAsync(InvitacionEquipoRechazadaIntegrationEvent integrationEvent, CancellationToken cancellationToken);
    Task PublishRolUsuarioModificadoAsync(RolUsuarioModificadoIntegrationEvent integrationEvent, CancellationToken cancellationToken);
    Task PublishPermisosRolActualizadosAsync(PermisosRolActualizadosIntegrationEvent integrationEvent, CancellationToken cancellationToken);
}

// ... (records de equipos existentes, sin cambios) ...

public sealed record RolUsuarioModificadoIntegrationEvent(
    Guid UsuarioId,
    string RolAnterior,
    string RolNuevo,
    DateTime OccurredOnUtc);

public sealed record PermisosRolActualizadosIntegrationEvent(
    string Rol,
    IReadOnlyList<string> Permisos,
    DateTime OccurredOnUtc);
```

Luego rename global mecánico: `grep -rln "IEquipoEventsPublisher" services/identity-service --include="*.cs" | grep -v bin | grep -v obj | xargs sed -i 's/IEquipoEventsPublisher/IIdentityEventsPublisher/g'`. Verificar con grep = 0 restantes.

- [ ] **Step 2: NoOp renombrado + extendido**

`git mv` a `NoOpIdentityEventsPublisher.cs`; clase `NoOpIdentityEventsPublisher : IIdentityEventsPublisher` — los 4 métodos existentes iguales + los 2 nuevos retornando `Task.CompletedTask`. Rename global de la clase: sed `NoOpEquipoEventsPublisher` → `NoOpIdentityEventsPublisher` en todo el servicio (mismo comando-patrón del Step 1).

- [ ] **Step 3: Messaging (5 archivos nuevos, patrón SP-3i)**

`RabbitMqOptions.cs`:

```csharp
namespace Umbral.IdentityService.Infrastructure.Services.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";
    public bool Enabled { get; set; }
    public string? Host { get; set; }
    public int Port { get; set; } = 5672;
    public string User { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string Exchange { get; set; } = "umbral.identity";
}
```

`IRabbitMqPublishChannel.cs`:

```csharp
namespace Umbral.IdentityService.Infrastructure.Services.Messaging;

// Seam mínimo de publicación: el publisher es unit-testeable sin broker;
// la conexión real solo se cubre con el integration test opt-in.
public interface IRabbitMqPublishChannel
{
    void Publish(string routingKey, byte[] body);
}
```

`RabbitMqPublishChannel.cs`: copia EXACTA del de Operaciones (`services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Services/Messaging/RabbitMqPublishChannel.cs`) con dos cambios: namespace `Umbral.IdentityService.Infrastructure.Services.Messaging` y nombre de conexión `"umbral-identity-publisher"`.

`EventEnvelope.cs`: copia EXACTA del de Operaciones (mismo record, `SerializerOptions` Web + `JsonStringEnumConverter`, `Create` con version 1) cambiando solo el namespace.

`IdentityEventRouting.cs`:

```csharp
namespace Umbral.IdentityService.Infrastructure.Services.Messaging;

public static class IdentityEventRouting
{
    // Mapa explícito (sin kebab algorítmico): el contrato documenta esta tabla 1:1.
    private static readonly IReadOnlyDictionary<string, string> Keys = new Dictionary<string, string>
    {
        ["EquipoCreado"] = "identity.equipo-creado.v1",
        ["InvitacionEquipoCreada"] = "identity.invitacion-equipo-creada.v1",
        ["InvitacionEquipoAceptada"] = "identity.invitacion-equipo-aceptada.v1",
        ["InvitacionEquipoRechazada"] = "identity.invitacion-equipo-rechazada.v1",
        ["RolUsuarioModificado"] = "identity.rol-usuario-modificado.v1",
        ["PermisosRolActualizados"] = "identity.permisos-rol-actualizados.v1",
    };

    public static string RoutingKeyFor(string eventType) => Keys[eventType];
}
```

- [ ] **Step 4: Publishers**

`RabbitMqIdentityEventsPublisher.cs`:

```csharp
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Umbral.IdentityService.Application.Interfaces;
using Umbral.IdentityService.Infrastructure.Services.Messaging;

namespace Umbral.IdentityService.Infrastructure.Services.Events;

public sealed class RabbitMqIdentityEventsPublisher : IIdentityEventsPublisher
{
    private readonly IRabbitMqPublishChannel _canal;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<RabbitMqIdentityEventsPublisher> _logger;

    public RabbitMqIdentityEventsPublisher(IRabbitMqPublishChannel canal, TimeProvider timeProvider,
        ILogger<RabbitMqIdentityEventsPublisher> logger)
    {
        _canal = canal;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    // Best-effort estricto: fallo de broker se loguea y NUNCA llega al caller (ADR-0012).
    private Task Publicar(string eventType, object payload)
    {
        try
        {
            var envelope = EventEnvelope.Create(eventType, payload, _timeProvider.GetUtcNow().UtcDateTime);
            var body = JsonSerializer.SerializeToUtf8Bytes(envelope, EventEnvelope.SerializerOptions);
            _canal.Publish(IdentityEventRouting.RoutingKeyFor(eventType), body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fallo publicando {EventType} a RabbitMQ (best-effort, se continúa)", eventType);
        }
        return Task.CompletedTask;
    }

    public Task PublishEquipoCreadoAsync(EquipoCreadoIntegrationEvent e, CancellationToken ct) => Publicar("EquipoCreado", e);
    public Task PublishInvitacionEquipoCreadaAsync(InvitacionEquipoCreadaIntegrationEvent e, CancellationToken ct) => Publicar("InvitacionEquipoCreada", e);
    public Task PublishInvitacionEquipoAceptadaAsync(InvitacionEquipoAceptadaIntegrationEvent e, CancellationToken ct) => Publicar("InvitacionEquipoAceptada", e);
    public Task PublishInvitacionEquipoRechazadaAsync(InvitacionEquipoRechazadaIntegrationEvent e, CancellationToken ct) => Publicar("InvitacionEquipoRechazada", e);
    public Task PublishRolUsuarioModificadoAsync(RolUsuarioModificadoIntegrationEvent e, CancellationToken ct) => Publicar("RolUsuarioModificado", e);
    public Task PublishPermisosRolActualizadosAsync(PermisosRolActualizadosIntegrationEvent e, CancellationToken ct) => Publicar("PermisosRolActualizados", e);
}
```

`CompositeIdentityEventsPublisher.cs`: copia del `CompositeSesionEventsPublisher` de Operaciones adaptada: namespace `Umbral.IdentityService.Infrastructure.Services.Events`, interfaz `IIdentityEventsPublisher`, mismo `FanOut` (re-lanza solo `OperationCanceledException`, loguea Warning por publisher caído), y los 6 métodos delegando `FanOut(p => p.PublishXxxAsync(e, ct))`.

- [ ] **Step 5: DI condicional + TimeProvider + csproj**

En `Umbral.IdentityService.Infrastructure.csproj`: `<PackageReference Include="RabbitMQ.Client" Version="6.8.1" />`.

En `DependencyInjection.cs`, reemplazar la línea `services.AddScoped<IIdentityEventsPublisher, NoOpIdentityEventsPublisher>();` (ya renombrada por Steps 1-2) por:

```csharp
        services.AddSingleton(TimeProvider.System);

        var rabbitOptions = configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>()
            ?? new RabbitMqOptions();
        var rabbitHabilitado = rabbitOptions.Enabled && !string.IsNullOrWhiteSpace(rabbitOptions.Host);
        services.AddScoped<NoOpIdentityEventsPublisher>();
        if (rabbitHabilitado)
        {
            services.AddSingleton(rabbitOptions);
            services.AddSingleton<IRabbitMqPublishChannel, RabbitMqPublishChannel>();
            services.AddScoped<RabbitMqIdentityEventsPublisher>();
        }
        services.AddScoped<IIdentityEventsPublisher>(sp =>
        {
            var publishers = new List<IIdentityEventsPublisher> { sp.GetRequiredService<NoOpIdentityEventsPublisher>() };
            if (rabbitHabilitado)
            {
                publishers.Add(sp.GetRequiredService<RabbitMqIdentityEventsPublisher>());
            }
            return new CompositeIdentityEventsPublisher(publishers,
                sp.GetRequiredService<ILogger<CompositeIdentityEventsPublisher>>());
        });
```

(usings necesarios; el método ya recibe `IConfiguration configuration` — verificar firma y usar la existente. Si `TimeProvider.System` ya estuviera registrado por otro camino, no duplicar — grep primero; hoy NO está.)

- [ ] **Step 6: Unit tests del publisher (RED→GREEN)**

`RabbitMqIdentityEventsPublisherTests.cs`:

```csharp
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Umbral.IdentityService.Application.Interfaces;
using Umbral.IdentityService.Infrastructure.Services.Events;
using Umbral.IdentityService.Infrastructure.Services.Messaging;

namespace Umbral.IdentityService.UnitTests.Infrastructure.Messaging;

public class RabbitMqIdentityEventsPublisherTests
{
    private sealed class CanalFake : IRabbitMqPublishChannel
    {
        public readonly List<(string RoutingKey, byte[] Body)> Publicados = new();
        public Exception? Lanzar { get; set; }
        public void Publish(string routingKey, byte[] body)
        {
            if (Lanzar is not null) throw Lanzar;
            Publicados.Add((routingKey, body));
        }
    }

    private static RabbitMqIdentityEventsPublisher CrearPublisher(CanalFake canal) =>
        new(canal, TimeProvider.System, NullLogger<RabbitMqIdentityEventsPublisher>.Instance);

    [Fact]
    public async Task Publica_envelope_camelCase_con_routing_key_correcta()
    {
        var canal = new CanalFake();
        var publisher = CrearPublisher(canal);

        await publisher.PublishPermisosRolActualizadosAsync(
            new PermisosRolActualizadosIntegrationEvent("Operador", new[] { "GestionarPartidas" }, new DateTime(2026, 7, 4, 0, 0, 0, DateTimeKind.Utc)),
            CancellationToken.None);

        var (routingKey, body) = Assert.Single(canal.Publicados);
        Assert.Equal("identity.permisos-rol-actualizados.v1", routingKey);
        using var doc = JsonDocument.Parse(body);
        Assert.Equal("PermisosRolActualizados", doc.RootElement.GetProperty("eventType").GetString());
        Assert.Equal(1, doc.RootElement.GetProperty("version").GetInt32());
        Assert.True(doc.RootElement.TryGetProperty("payload", out var payload));
        Assert.Equal("Operador", payload.GetProperty("rol").GetString());
    }

    [Fact]
    public async Task Fallo_del_canal_no_escapa_al_caller()
    {
        var canal = new CanalFake { Lanzar = new InvalidOperationException("broker caído") };
        var publisher = CrearPublisher(canal);

        var ex = await Record.ExceptionAsync(() => publisher.PublishRolUsuarioModificadoAsync(
            new RolUsuarioModificadoIntegrationEvent(Guid.NewGuid(), "Participante", "Operador", DateTime.UtcNow),
            CancellationToken.None));

        Assert.Null(ex);
    }

    [Fact]
    public async Task Cada_metodo_del_publisher_tiene_routing_key_en_el_mapa()
    {
        var canal = new CanalFake();
        var publisher = CrearPublisher(canal);
        var ahora = DateTime.UtcNow;

        await publisher.PublishEquipoCreadoAsync(new EquipoCreadoIntegrationEvent(Guid.NewGuid(), Guid.NewGuid(), ahora), CancellationToken.None);
        await publisher.PublishInvitacionEquipoCreadaAsync(new InvitacionEquipoCreadaIntegrationEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ahora), CancellationToken.None);
        await publisher.PublishInvitacionEquipoAceptadaAsync(new InvitacionEquipoAceptadaIntegrationEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ahora), CancellationToken.None);
        await publisher.PublishInvitacionEquipoRechazadaAsync(new InvitacionEquipoRechazadaIntegrationEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ahora), CancellationToken.None);
        await publisher.PublishRolUsuarioModificadoAsync(new RolUsuarioModificadoIntegrationEvent(Guid.NewGuid(), "Participante", "Operador", ahora), CancellationToken.None);
        await publisher.PublishPermisosRolActualizadosAsync(new PermisosRolActualizadosIntegrationEvent("Operador", new[] { "GestionarPartidas" }, ahora), CancellationToken.None);

        Assert.Equal(6, canal.Publicados.Count);
        Assert.Equal(6, canal.Publicados.Select(p => p.RoutingKey).Distinct().Count());
        Assert.All(canal.Publicados, p => Assert.StartsWith("identity.", p.RoutingKey));
        Assert.All(canal.Publicados, p => Assert.EndsWith(".v1", p.RoutingKey));
    }
}
```

Correr RED primero (los tipos no existen → CS): `dotnet test services/identity-service/tests/Umbral.IdentityService.UnitTests --filter "RabbitMqIdentityEventsPublisherTests"` — falla de compilación cuenta como RED del ciclo (los Steps 1-5 la vuelven verde).

- [ ] **Step 7: Round-trip opt-in**

`RabbitMqRoundTripTests.cs` (IntegrationTests) — leer primero `services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests/RabbitMqRoundTripTests.cs` como referencia de estilo, y escribir el equivalente identity:

```csharp
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Client;
using Umbral.IdentityService.Application.Interfaces;
using Umbral.IdentityService.Infrastructure.Services.Events;
using Umbral.IdentityService.Infrastructure.Services.Messaging;

namespace Umbral.IdentityService.IntegrationTests;

public class RabbitMqRoundTripTests
{
    [Fact]
    public async Task PermisosRolActualizados_llega_al_broker_real()
    {
        var host = Environment.GetEnvironmentVariable("RABBITMQ_TEST_HOST");
        if (string.IsNullOrWhiteSpace(host))
        {
            return; // opt-in: sin broker configurado el test es un no-op (skip suave, patrón SP-3i)
        }

        var options = new RabbitMqOptions { Enabled = true, Host = host, User = "umbral", Password = "16102005" };
        using var canal = new RabbitMqPublishChannel(options);
        var publisher = new RabbitMqIdentityEventsPublisher(canal, TimeProvider.System,
            NullLogger<RabbitMqIdentityEventsPublisher>.Instance);

        var factory = new ConnectionFactory { HostName = host, Port = options.Port, UserName = options.User, Password = options.Password };
        using var connection = factory.CreateConnection("umbral-identity-roundtrip-test");
        using var channel = connection.CreateModel();
        channel.ExchangeDeclare(options.Exchange, ExchangeType.Topic, durable: true, autoDelete: false);
        var queue = channel.QueueDeclare(queue: "", durable: false, exclusive: true, autoDelete: true).QueueName;
        channel.QueueBind(queue, options.Exchange, "identity.#");

        await publisher.PublishPermisosRolActualizadosAsync(
            new PermisosRolActualizadosIntegrationEvent("Operador", new[] { "GestionarPartidas" }, DateTime.UtcNow),
            CancellationToken.None);

        BasicGetResult? result = null;
        for (var i = 0; i < 50 && result is null; i++)
        {
            result = channel.BasicGet(queue, autoAck: true);
            if (result is null) await Task.Delay(100);
        }

        Assert.NotNull(result);
        using var doc = JsonDocument.Parse(Encoding.UTF8.GetString(result!.Body.ToArray()));
        Assert.Equal("PermisosRolActualizados", doc.RootElement.GetProperty("eventType").GetString());
    }
}
```

(Credenciales `umbral/16102005` = las del compose, mismas que usa el equivalente de Operaciones — verificar contra ese archivo y ajustar si difieren.)

- [ ] **Step 8: Suite completa verde + commit**

Run: `dotnet test services/identity-service/Umbral.IdentityService.sln`
Expected: PASS — 144+3 unit / 37+1 integration (round-trip no-op sin env) / 30 contract. Cero referencias `IEquipoEventsPublisher`/`NoOpEquipoEventsPublisher` (grep).

```bash
git add <los 13 archivos creados/renombrados/modificados exactos + cada consumidor del rename, uno por uno>
git commit -m "feat(sp5b): backbone RabbitMQ Identity + publisher unificado IIdentityEventsPublisher

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 2 (G2): Dominio + persistencia — PermisoFuncional, permisos_rol, seed BR-R03, Usuario.CambiarRol

**Files:**
- Create: `services/identity-service/src/Umbral.IdentityService.Domain/Enums/PermisoFuncional.cs`
- Create: `services/identity-service/src/Umbral.IdentityService.Domain/Entities/PermisoRol.cs`
- Create: `services/identity-service/src/Umbral.IdentityService.Domain/Exceptions/RolDeAdministradorInmutableException.cs`
- Modify: `services/identity-service/src/Umbral.IdentityService.Domain/Entities/Usuario.cs` (método `CambiarRol`)
- Create: `services/identity-service/src/Umbral.IdentityService.Domain/Abstractions/Persistence/IPermisosRolRepository.cs`
- Create: `services/identity-service/src/Umbral.IdentityService.Infrastructure/Persistence/PermisosRolRepository.cs`
- Modify: `services/identity-service/src/Umbral.IdentityService.Infrastructure/Persistence/IdentityDbContext.cs` (DbSet + config `permisos_rol`)
- Modify: `services/identity-service/src/Umbral.IdentityService.Infrastructure/DependencyInjection.cs` (registro repo)
- Modify: `services/identity-service/src/Umbral.IdentityService.Api/Program.cs` (DDL + seed)
- Create: `services/identity-service/tests/Umbral.IdentityService.UnitTests/Domain/UsuarioCambiarRolTests.cs`
- Create: `services/identity-service/tests/Umbral.IdentityService.IntegrationTests/PermisosRolRepositoryTests.cs`

**Interfaces:**
- Produces: `enum PermisoFuncional { GestionarPartidas = 1, GestionarEquipos = 2, ParticiparEnPartidas = 3 }`; `IPermisosRolRepository { Task<IReadOnlyDictionary<RolUsuario, IReadOnlyList<PermisoFuncional>>> GetMatrizAsync(CancellationToken); Task<IReadOnlyList<PermisoFuncional>> GetByRolAsync(RolUsuario, CancellationToken); Task ReplaceForRolAsync(RolUsuario, IReadOnlyCollection<PermisoFuncional>, CancellationToken); }`; `Usuario.CambiarRol(RolUsuario nuevo)` (lanza `RolDeAdministradorInmutableException` si `Rol == Administrador`; no-op si `nuevo == Rol`). G4/G5 consumen todo esto.

- [ ] **Step 1: Tests de dominio (RED)**

`UsuarioCambiarRolTests.cs`:

```csharp
using Umbral.IdentityService.Domain.Entities;
using Umbral.IdentityService.Domain.Enums;
using Umbral.IdentityService.Domain.Exceptions;

namespace Umbral.IdentityService.UnitTests.Domain;

public class UsuarioCambiarRolTests
{
    private static Usuario Crear(RolUsuario rol) =>
        Usuario.Crear(Guid.NewGuid().ToString(), "Nombre", "a@b.com", rol);

    [Fact]
    public void Participante_puede_cambiar_a_Operador()
    {
        var usuario = Crear(RolUsuario.Participante);
        usuario.CambiarRol(RolUsuario.Operador);
        Assert.Equal(RolUsuario.Operador, usuario.Rol);
    }

    [Fact]
    public void Operador_puede_promoverse_a_Administrador()
    {
        var usuario = Crear(RolUsuario.Operador);
        usuario.CambiarRol(RolUsuario.Administrador);
        Assert.Equal(RolUsuario.Administrador, usuario.Rol);
    }

    [Fact]
    public void Rol_de_Administrador_es_inmutable()
    {
        var usuario = Crear(RolUsuario.Administrador);
        Assert.Throws<RolDeAdministradorInmutableException>(() => usuario.CambiarRol(RolUsuario.Operador));
    }

    [Fact]
    public void Mismo_rol_es_noop()
    {
        var usuario = Crear(RolUsuario.Participante);
        usuario.CambiarRol(RolUsuario.Participante);
        Assert.Equal(RolUsuario.Participante, usuario.Rol);
    }
}
```

Run RED: `dotnet test services/identity-service/tests/Umbral.IdentityService.UnitTests --filter "UsuarioCambiarRolTests"` → falla compilación (CambiarRol/exception no existen).

- [ ] **Step 2: Dominio**

`PermisoFuncional.cs`:

```csharp
namespace Umbral.IdentityService.Domain.Enums;

public enum PermisoFuncional
{
    GestionarPartidas = 1,
    GestionarEquipos = 2,
    ParticiparEnPartidas = 3
}
```

`RolDeAdministradorInmutableException.cs`:

```csharp
namespace Umbral.IdentityService.Domain.Exceptions;

public sealed class RolDeAdministradorInmutableException : Exception
{
    public RolDeAdministradorInmutableException()
        : base("El rol de un Administrador no puede modificarse (BR-R04).")
    {
    }
}
```

`Usuario.cs` — agregar tras `EditarDatosGenerales`:

```csharp
    public void CambiarRol(RolUsuario nuevoRol)
    {
        if (Rol == RolUsuario.Administrador)
        {
            throw new RolDeAdministradorInmutableException();
        }

        Rol = nuevoRol;
    }
```

`PermisoRol.cs`:

```csharp
using Umbral.IdentityService.Domain.Enums;

namespace Umbral.IdentityService.Domain.Entities;

public sealed class PermisoRol
{
    public RolUsuario Rol { get; private set; }
    public PermisoFuncional Permiso { get; private set; }

    private PermisoRol() { }

    public PermisoRol(RolUsuario rol, PermisoFuncional permiso)
    {
        Rol = rol;
        Permiso = permiso;
    }
}
```

`IPermisosRolRepository.cs`:

```csharp
using Umbral.IdentityService.Domain.Enums;

namespace Umbral.IdentityService.Domain.Abstractions.Persistence;

public interface IPermisosRolRepository
{
    Task<IReadOnlyDictionary<RolUsuario, IReadOnlyList<PermisoFuncional>>> GetMatrizAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<PermisoFuncional>> GetByRolAsync(RolUsuario rol, CancellationToken cancellationToken);
    Task ReplaceForRolAsync(RolUsuario rol, IReadOnlyCollection<PermisoFuncional> permisos, CancellationToken cancellationToken);
}
```

Run GREEN dominio: mismo filtro del Step 1 → 4/4.

- [ ] **Step 3: Persistencia**

`IdentityDbContext.cs` — agregar `public DbSet<PermisoRol> PermisosRol => Set<PermisoRol>();` y en `OnModelCreating`:

```csharp
        modelBuilder.Entity<PermisoRol>(entity =>
        {
            entity.ToTable("permisos_rol");
            entity.HasKey(p => new { p.Rol, p.Permiso });
            entity.Property(p => p.Rol).HasColumnName("rol");
            entity.Property(p => p.Permiso).HasColumnName("permiso");
        });
```

`PermisosRolRepository.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Domain.Entities;
using Umbral.IdentityService.Domain.Enums;

namespace Umbral.IdentityService.Infrastructure.Persistence;

public sealed class PermisosRolRepository : IPermisosRolRepository
{
    private readonly IdentityDbContext _dbContext;

    public PermisosRolRepository(IdentityDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyDictionary<RolUsuario, IReadOnlyList<PermisoFuncional>>> GetMatrizAsync(CancellationToken cancellationToken)
    {
        var filas = await _dbContext.PermisosRol.AsNoTracking().ToListAsync(cancellationToken);
        return Enum.GetValues<RolUsuario>().ToDictionary(
            rol => rol,
            rol => (IReadOnlyList<PermisoFuncional>)filas.Where(f => f.Rol == rol).Select(f => f.Permiso).OrderBy(p => p).ToList());
    }

    public async Task<IReadOnlyList<PermisoFuncional>> GetByRolAsync(RolUsuario rol, CancellationToken cancellationToken)
        => await _dbContext.PermisosRol.AsNoTracking()
            .Where(f => f.Rol == rol).Select(f => f.Permiso).OrderBy(p => p).ToListAsync(cancellationToken);

    public async Task ReplaceForRolAsync(RolUsuario rol, IReadOnlyCollection<PermisoFuncional> permisos, CancellationToken cancellationToken)
    {
        var actuales = await _dbContext.PermisosRol.Where(f => f.Rol == rol).ToListAsync(cancellationToken);
        _dbContext.PermisosRol.RemoveRange(actuales);
        _dbContext.PermisosRol.AddRange(permisos.Distinct().Select(p => new PermisoRol(rol, p)));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
```

`DependencyInjection.cs`: `services.AddScoped<IPermisosRolRepository, PermisosRolRepository>();` junto a los otros repos.

`Program.cs` — dentro del bloque `if (dbContext.Database.IsRelational())` existente, agregar al SQL raw (o en un `ExecuteSqlRawAsync` adicional inmediatamente después, siguiendo el estilo):

```sql
CREATE TABLE IF NOT EXISTS permisos_rol (
    rol integer NOT NULL,
    permiso integer NOT NULL,
    PRIMARY KEY (rol, permiso)
);

INSERT INTO permisos_rol (rol, permiso)
SELECT v.rol, v.permiso
FROM (VALUES (2, 1), (3, 2), (3, 3)) AS v(rol, permiso)
WHERE NOT EXISTS (SELECT 1 FROM permisos_rol);
```

(Seed BR-R03 solo si la tabla está vacía: Operador(2)→GestionarPartidas(1); Participante(3)→GestionarEquipos(2)+ParticiparEnPartidas(3). Administrador sin filas.)

- [ ] **Step 4: Integration tests (RED→GREEN)**

`PermisosRolRepositoryTests.cs` — seguir el patrón de fixture/DbContext de `PartidaPersistenceTests`-equivalentes de Identity (mirar cómo los integration tests existentes de Identity obtienen `IdentityDbContext`; usar el mismo mecanismo):

```csharp
// Tests (cuerpos exactos; el arnés/fixture se toma del patrón existente del proyecto):

    [Fact]
    public async Task GetMatriz_devuelve_los_tres_roles_incluso_sin_filas()
    {
        var matriz = await _repo.GetMatrizAsync(CancellationToken.None);
        Assert.Equal(3, matriz.Count);
        Assert.Contains(RolUsuario.Administrador, matriz.Keys);
    }

    [Fact]
    public async Task ReplaceForRol_reemplaza_el_set_completo()
    {
        await _repo.ReplaceForRolAsync(RolUsuario.Operador,
            new[] { PermisoFuncional.GestionarPartidas, PermisoFuncional.ParticiparEnPartidas }, CancellationToken.None);
        await _repo.ReplaceForRolAsync(RolUsuario.Operador,
            new[] { PermisoFuncional.GestionarEquipos }, CancellationToken.None);

        var permisos = await _repo.GetByRolAsync(RolUsuario.Operador, CancellationToken.None);
        Assert.Equal(new[] { PermisoFuncional.GestionarEquipos }, permisos);
    }

    [Fact]
    public async Task ReplaceForRol_con_vacio_borra_todo()
    {
        await _repo.ReplaceForRolAsync(RolUsuario.Participante, new[] { PermisoFuncional.GestionarEquipos }, CancellationToken.None);
        await _repo.ReplaceForRolAsync(RolUsuario.Participante, Array.Empty<PermisoFuncional>(), CancellationToken.None);

        Assert.Empty(await _repo.GetByRolAsync(RolUsuario.Participante, CancellationToken.None));
    }
```

- [ ] **Step 5: Suite completa + commit**

Run: `dotnet test services/identity-service/Umbral.IdentityService.sln`
Expected: PASS, unit +4, integration +3.

```bash
git add <los 11 archivos exactos>
git commit -m "feat(sp5b): permisos_rol con seed BR-R03 + Usuario.CambiarRol con guard de admin

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 3 (G3): Adapter Keycloak — composites + cambio de realm role

**Files:**
- Modify: `services/identity-service/src/Umbral.IdentityService.Application/Interfaces/IKeycloakIdentityPort.cs`
- Modify: `services/identity-service/src/Umbral.IdentityService.Infrastructure/Services/Identity/KeycloakIdentityAdapter.cs`
- Modify: fakes de `IKeycloakIdentityPort` en tests (localizar: `grep -rln "IKeycloakIdentityPort" services/identity-service/tests --include="*.cs" | grep -v bin | grep -v obj` — hoy: 2 `IdentityApiFactory.cs`, `CreateUserEndpointIntegrationTests.cs`, `Hu02HandlersTests.cs`, `CreateUserHandlerTests.cs`)

**Interfaces:**
- Produces (en `IKeycloakIdentityPort`):
  - `Task AddCompositeToRoleAsync(string roleName, string compositeRoleName, CancellationToken cancellationToken);`
  - `Task RemoveCompositeFromRoleAsync(string roleName, string compositeRoleName, CancellationToken cancellationToken);`
  - `Task ChangeUserRealmRoleAsync(string keycloakId, string oldRoleName, string newRoleName, CancellationToken cancellationToken);`
  G4 usa los composites; G5 usa el change. Todos lanzan `KeycloakIntegrationException` en fallo (→ 502). Los REMOVE toleran 404 (idempotencia del camino de reparación, spec §10).

- [ ] **Step 1: Interfaz + implementación**

Agregar las 3 firmas al puerto (con doc-comments breves como los existentes). En el adapter, agregar (usa los privados existentes `GetAdminAccessTokenAsync`/`GetRealmRoleAsync`):

```csharp
    public async Task AddCompositeToRoleAsync(string roleName, string compositeRoleName, CancellationToken cancellationToken)
    {
        var accessToken = await GetAdminAccessTokenAsync(cancellationToken);
        var composite = await GetRealmRoleAsync(accessToken, compositeRoleName, cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"{_options.BaseUrl.TrimEnd('/')}/admin/realms/{_options.Realm}/roles/{roleName}/composites")
        {
            Content = JsonContent.Create(new[] { new { id = composite.Id, name = composite.Name } })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new KeycloakIntegrationException($"Failed to add composite '{compositeRoleName}' to role '{roleName}'. StatusCode={(int)response.StatusCode}");
        }
    }

    public async Task RemoveCompositeFromRoleAsync(string roleName, string compositeRoleName, CancellationToken cancellationToken)
    {
        var accessToken = await GetAdminAccessTokenAsync(cancellationToken);
        var composite = await GetRealmRoleAsync(accessToken, compositeRoleName, cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Delete,
            $"{_options.BaseUrl.TrimEnd('/')}/admin/realms/{_options.Realm}/roles/{roleName}/composites")
        {
            Content = JsonContent.Create(new[] { new { id = composite.Id, name = composite.Name } })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        // 404 tolerado: quitar algo ya ausente es idempotente (camino de reparación tras 502 parcial).
        if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NotFound)
        {
            throw new KeycloakIntegrationException($"Failed to remove composite '{compositeRoleName}' from role '{roleName}'. StatusCode={(int)response.StatusCode}");
        }
    }

    public async Task ChangeUserRealmRoleAsync(string keycloakId, string oldRoleName, string newRoleName, CancellationToken cancellationToken)
    {
        var accessToken = await GetAdminAccessTokenAsync(cancellationToken);

        var oldRole = await GetRealmRoleAsync(accessToken, oldRoleName, cancellationToken);
        using (var removeRequest = new HttpRequestMessage(HttpMethod.Delete,
            $"{_options.BaseUrl.TrimEnd('/')}/admin/realms/{_options.Realm}/users/{keycloakId}/role-mappings/realm")
        {
            Content = JsonContent.Create(new[] { new { id = oldRole.Id, name = oldRole.Name } })
        })
        {
            removeRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var removeResponse = await _httpClient.SendAsync(removeRequest, cancellationToken);
            // 404 tolerado: el mapping viejo puede no existir (reintento tras fallo parcial).
            if (!removeResponse.IsSuccessStatusCode && removeResponse.StatusCode != HttpStatusCode.NotFound)
            {
                throw new KeycloakIntegrationException($"Failed to remove realm role '{oldRoleName}' from user. StatusCode={(int)removeResponse.StatusCode}");
            }
        }

        await AssignRealmRoleAsync(accessToken, keycloakId, newRoleName, cancellationToken);
    }
```

- [ ] **Step 2: Extender los fakes de test**

En cada fake de `IKeycloakIdentityPort` hallado por el grep: implementar los 3 métodos. Fakes de unit tests de handlers: registrar las llamadas en listas públicas (`CompositesAgregados`, `CompositesQuitados`, `CambiosDeRol` como `List<(string, string)>`/`List<(string, string, string)>`) y soportar `Exception? Lanzar` para simular fallo — G4/G5 asertan orden y argumentos con esto. Fakes de factories (contract/integration): no-op suficiente, mismas listas si el fake ya registra llamadas.

- [ ] **Step 3: Build + suite + commit**

Run: `dotnet test services/identity-service/Umbral.IdentityService.sln`
Expected: PASS sin regresiones (los métodos nuevos aún sin consumidores).

```bash
git add services/identity-service/src/Umbral.IdentityService.Application/Interfaces/IKeycloakIdentityPort.cs services/identity-service/src/Umbral.IdentityService.Infrastructure/Services/Identity/KeycloakIdentityAdapter.cs
git add <cada fake tocado, uno por uno>
git commit -m "feat(sp5b): adapter Keycloak gana composites add/remove y cambio de realm role

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 4 (G4): Panel de permisos — GET matriz + PUT set completo

**Files:**
- Create: `services/identity-service/src/Umbral.IdentityService.Application/Queries/GetPermisosRolesQuery.cs`
- Create: `services/identity-service/src/Umbral.IdentityService.Application/Handlers/Queries/GetPermisosRolesQueryHandler.cs`
- Create: `services/identity-service/src/Umbral.IdentityService.Application/Commands/ActualizarPermisosRolCommand.cs`
- Create: `services/identity-service/src/Umbral.IdentityService.Application/Handlers/Commands/ActualizarPermisosRolCommandHandler.cs`
- Create: `services/identity-service/src/Umbral.IdentityService.Application/Validators/ActualizarPermisosRolCommandValidator.cs`
- Create: `services/identity-service/src/Umbral.IdentityService.Application/DTOs/PermisosRolesDtos.cs`
- Create: `services/identity-service/src/Umbral.IdentityService.Api/Controllers/GovernanceController.cs`
- Create: `services/identity-service/src/Umbral.IdentityService.Api/Contracts/GovernanceRequests.cs`
- Create: `services/identity-service/tests/Umbral.IdentityService.UnitTests/ActualizarPermisosRolHandlerTests.cs`
- Create: `services/identity-service/tests/Umbral.IdentityService.UnitTests/Api/GovernanceControllerTests.cs`
- Create: `services/identity-service/tests/Umbral.IdentityService.ContractTests/GovernanceContractTests.cs`

**Interfaces:**
- Consumes: `IPermisosRolRepository` (G2), `AddCompositeToRoleAsync`/`RemoveCompositeFromRoleAsync` (G3), `IIdentityEventsPublisher.PublishPermisosRolActualizadosAsync` + record (G1), `TimeProvider` (G1).
- Produces: `GetPermisosRolesQuery() : IRequest<PermisosRolesResponse>`; `ActualizarPermisosRolCommand(string Rol, IReadOnlyList<string> Permisos) : IRequest<RolPermisosDto>`; DTOs `PermisosRolesResponse(IReadOnlyList<RolPermisosDto> Roles)` y `RolPermisosDto(string Rol, IReadOnlyList<string> Permisos, bool PrivilegiosGobernanza)`. Rutas: `GET identity/governance/roles`, `PUT identity/governance/roles/{rol}/permisos`.

- [ ] **Step 1: DTOs, query, command, validator**

`PermisosRolesDtos.cs`:

```csharp
namespace Umbral.IdentityService.Application.DTOs;

public sealed record RolPermisosDto(string Rol, IReadOnlyList<string> Permisos, bool PrivilegiosGobernanza);

public sealed record PermisosRolesResponse(IReadOnlyList<RolPermisosDto> Roles);
```

`GetPermisosRolesQuery.cs`:

```csharp
using MediatR;
using Umbral.IdentityService.Application.DTOs;

namespace Umbral.IdentityService.Application.Queries;

public sealed record GetPermisosRolesQuery() : IRequest<PermisosRolesResponse>;
```

`ActualizarPermisosRolCommand.cs`:

```csharp
using MediatR;
using Umbral.IdentityService.Application.DTOs;

namespace Umbral.IdentityService.Application.Commands;

public sealed record ActualizarPermisosRolCommand(string Rol, IReadOnlyList<string> Permisos) : IRequest<RolPermisosDto>;
```

`ActualizarPermisosRolCommandValidator.cs`:

```csharp
using FluentValidation;
using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Domain.Enums;

namespace Umbral.IdentityService.Application.Validators;

public sealed class ActualizarPermisosRolCommandValidator : AbstractValidator<ActualizarPermisosRolCommand>
{
    public ActualizarPermisosRolCommandValidator()
    {
        RuleFor(c => c.Rol)
            .Must(rol => Enum.TryParse<RolUsuario>(rol, ignoreCase: false, out _))
            .WithMessage("Rol inválido: debe ser Administrador, Operador o Participante.");

        RuleFor(c => c.Permisos).NotNull();

        RuleForEach(c => c.Permisos)
            .Must(p => Enum.TryParse<PermisoFuncional>(p, ignoreCase: false, out _))
            .WithMessage("Permiso inválido: debe ser GestionarPartidas, GestionarEquipos o ParticiparEnPartidas.");
    }
}
```

- [ ] **Step 2: Tests del handler (RED)**

`ActualizarPermisosRolHandlerTests.cs` — fakes: repo en memoria (diccionario), fake Keycloak port de G3, publisher fake que graba eventos:

```csharp
using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Application.Handlers.Commands;
using Umbral.IdentityService.Application.Interfaces;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Domain.Enums;

namespace Umbral.IdentityService.UnitTests;

public class ActualizarPermisosRolHandlerTests
{
    private sealed class RepoFake : IPermisosRolRepository
    {
        public readonly Dictionary<RolUsuario, List<PermisoFuncional>> Datos = new();
        public bool EscrituraRecibida;

        public Task<IReadOnlyDictionary<RolUsuario, IReadOnlyList<PermisoFuncional>>> GetMatrizAsync(CancellationToken ct)
            => Task.FromResult<IReadOnlyDictionary<RolUsuario, IReadOnlyList<PermisoFuncional>>>(
                Enum.GetValues<RolUsuario>().ToDictionary(r => r,
                    r => (IReadOnlyList<PermisoFuncional>)(Datos.TryGetValue(r, out var p) ? p.OrderBy(x => x).ToList() : new List<PermisoFuncional>())));

        public Task<IReadOnlyList<PermisoFuncional>> GetByRolAsync(RolUsuario rol, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<PermisoFuncional>>(Datos.TryGetValue(rol, out var p) ? p.OrderBy(x => x).ToList() : new List<PermisoFuncional>());

        public Task ReplaceForRolAsync(RolUsuario rol, IReadOnlyCollection<PermisoFuncional> permisos, CancellationToken ct)
        {
            EscrituraRecibida = true;
            Datos[rol] = permisos.Distinct().ToList();
            return Task.CompletedTask;
        }
    }

    private sealed class KeycloakFake : Umbral.IdentityService.Application.Interfaces.IKeycloakIdentityPort
    {
        public readonly List<(string Rol, string Permiso)> CompositesAgregados = new();
        public readonly List<(string Rol, string Permiso)> CompositesQuitados = new();
        public Exception? Lanzar;

        public Task AddCompositeToRoleAsync(string roleName, string compositeRoleName, CancellationToken ct)
        { if (Lanzar is not null) throw Lanzar; CompositesAgregados.Add((roleName, compositeRoleName)); return Task.CompletedTask; }
        public Task RemoveCompositeFromRoleAsync(string roleName, string compositeRoleName, CancellationToken ct)
        { if (Lanzar is not null) throw Lanzar; CompositesQuitados.Add((roleName, compositeRoleName)); return Task.CompletedTask; }
        public Task ChangeUserRealmRoleAsync(string keycloakId, string o, string n, CancellationToken ct) => Task.CompletedTask;
        // Resto de la interfaz: stubs con NotImplementedException usando las firmas REALES de
        // IKeycloakIdentityPort (LEER el archivo del puerto y copiar cada firma exacta — las
        // de CreateUserWithInitialRoleAsync etc. no se transcriben aquí para no divergir).
    }

    private sealed class PublisherFake : IIdentityEventsPublisher
    {
        public readonly List<PermisosRolActualizadosIntegrationEvent> Eventos = new();
        public Task PublishPermisosRolActualizadosAsync(PermisosRolActualizadosIntegrationEvent e, CancellationToken ct)
        { Eventos.Add(e); return Task.CompletedTask; }
        public Task PublishEquipoCreadoAsync(EquipoCreadoIntegrationEvent e, CancellationToken ct) => Task.CompletedTask;
        public Task PublishInvitacionEquipoCreadaAsync(InvitacionEquipoCreadaIntegrationEvent e, CancellationToken ct) => Task.CompletedTask;
        public Task PublishInvitacionEquipoAceptadaAsync(InvitacionEquipoAceptadaIntegrationEvent e, CancellationToken ct) => Task.CompletedTask;
        public Task PublishInvitacionEquipoRechazadaAsync(InvitacionEquipoRechazadaIntegrationEvent e, CancellationToken ct) => Task.CompletedTask;
        public Task PublishRolUsuarioModificadoAsync(RolUsuarioModificadoIntegrationEvent e, CancellationToken ct) => Task.CompletedTask;
    }

    private static (ActualizarPermisosRolCommandHandler Handler, RepoFake Repo, KeycloakFake Kc, PublisherFake Pub) Crear()
    {
        var repo = new RepoFake();
        var kc = new KeycloakFake();
        var pub = new PublisherFake();
        return (new ActualizarPermisosRolCommandHandler(repo, kc, pub, TimeProvider.System), repo, kc, pub);
    }

    [Fact]
    public async Task Diff_mixto_aplica_solo_agregados_y_quitados()
    {
        var (handler, repo, kc, pub) = Crear();
        repo.Datos[RolUsuario.Operador] = new List<PermisoFuncional> { PermisoFuncional.GestionarPartidas };

        var result = await handler.Handle(
            new ActualizarPermisosRolCommand("Operador", new[] { "GestionarEquipos" }), CancellationToken.None);

        Assert.Equal(new[] { ("Operador", "GestionarEquipos") }, kc.CompositesAgregados);
        Assert.Equal(new[] { ("Operador", "GestionarPartidas") }, kc.CompositesQuitados);
        Assert.Equal(new List<PermisoFuncional> { PermisoFuncional.GestionarEquipos }, repo.Datos[RolUsuario.Operador]);
        Assert.Equal("Operador", result.Rol);
        Assert.Equal(new[] { "GestionarEquipos" }, result.Permisos);
        var evento = Assert.Single(pub.Eventos);
        Assert.Equal(new[] { "GestionarEquipos" }, evento.Permisos);
    }

    [Fact]
    public async Task Diff_vacio_no_toca_keycloak_ni_db_ni_evento()
    {
        var (handler, repo, kc, pub) = Crear();
        repo.Datos[RolUsuario.Participante] = new List<PermisoFuncional> { PermisoFuncional.GestionarEquipos, PermisoFuncional.ParticiparEnPartidas };

        await handler.Handle(new ActualizarPermisosRolCommand("Participante",
            new[] { "ParticiparEnPartidas", "GestionarEquipos" }), CancellationToken.None);

        Assert.Empty(kc.CompositesAgregados);
        Assert.Empty(kc.CompositesQuitados);
        Assert.False(repo.EscrituraRecibida);
        Assert.Empty(pub.Eventos);
    }

    [Fact]
    public async Task Fallo_de_keycloak_no_persiste_en_db_ni_emite_evento()
    {
        var (handler, repo, kc, pub) = Crear();
        kc.Lanzar = new Umbral.IdentityService.Application.Exceptions.KeycloakIntegrationException("down");

        await Assert.ThrowsAsync<Umbral.IdentityService.Application.Exceptions.KeycloakIntegrationException>(
            () => handler.Handle(new ActualizarPermisosRolCommand("Operador", new[] { "GestionarEquipos" }), CancellationToken.None));

        Assert.False(repo.EscrituraRecibida);
        Assert.Empty(pub.Eventos);
    }

    [Fact]
    public async Task Duplicados_en_el_body_se_normalizan()
    {
        var (handler, repo, kc, _) = Crear();

        var result = await handler.Handle(new ActualizarPermisosRolCommand("Administrador",
            new[] { "GestionarPartidas", "GestionarPartidas" }), CancellationToken.None);

        Assert.Single(kc.CompositesAgregados);
        Assert.Equal(new[] { "GestionarPartidas" }, result.Permisos);
    }
}
```

Run RED (falla compilación — handler no existe).

- [ ] **Step 3: Handlers**

`GetPermisosRolesQueryHandler.cs`:

```csharp
using MediatR;
using Umbral.IdentityService.Application.DTOs;
using Umbral.IdentityService.Application.Queries;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Domain.Enums;

namespace Umbral.IdentityService.Application.Handlers.Queries;

public sealed class GetPermisosRolesQueryHandler : IRequestHandler<GetPermisosRolesQuery, PermisosRolesResponse>
{
    private readonly IPermisosRolRepository _permisosRol;

    public GetPermisosRolesQueryHandler(IPermisosRolRepository permisosRol) => _permisosRol = permisosRol;

    public async Task<PermisosRolesResponse> Handle(GetPermisosRolesQuery request, CancellationToken cancellationToken)
    {
        var matriz = await _permisosRol.GetMatrizAsync(cancellationToken);
        var roles = matriz
            .OrderBy(kv => kv.Key)
            .Select(kv => new RolPermisosDto(
                kv.Key.ToString(),
                kv.Value.Select(p => p.ToString()).ToList(),
                PrivilegiosGobernanza: kv.Key == RolUsuario.Administrador))
            .ToList();
        return new PermisosRolesResponse(roles);
    }
}
```

`ActualizarPermisosRolCommandHandler.cs`:

```csharp
using MediatR;
using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Application.DTOs;
using Umbral.IdentityService.Application.Interfaces;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Domain.Enums;

namespace Umbral.IdentityService.Application.Handlers.Commands;

public sealed class ActualizarPermisosRolCommandHandler : IRequestHandler<ActualizarPermisosRolCommand, RolPermisosDto>
{
    private readonly IPermisosRolRepository _permisosRol;
    private readonly IKeycloakIdentityPort _keycloak;
    private readonly IIdentityEventsPublisher _events;
    private readonly TimeProvider _timeProvider;

    public ActualizarPermisosRolCommandHandler(
        IPermisosRolRepository permisosRol,
        IKeycloakIdentityPort keycloak,
        IIdentityEventsPublisher events,
        TimeProvider timeProvider)
    {
        _permisosRol = permisosRol;
        _keycloak = keycloak;
        _events = events;
        _timeProvider = timeProvider;
    }

    public async Task<RolPermisosDto> Handle(ActualizarPermisosRolCommand request, CancellationToken cancellationToken)
    {
        var rol = Enum.Parse<RolUsuario>(request.Rol);
        var deseados = request.Permisos.Select(p => Enum.Parse<PermisoFuncional>(p)).Distinct().OrderBy(p => p).ToList();

        var actuales = await _permisosRol.GetByRolAsync(rol, cancellationToken);
        var agregar = deseados.Except(actuales).ToList();
        var quitar = actuales.Except(deseados).ToList();

        var permisosFinales = deseados.Select(p => p.ToString()).ToList();
        if (agregar.Count == 0 && quitar.Count == 0)
        {
            return new RolPermisosDto(rol.ToString(), permisosFinales, rol == RolUsuario.Administrador);
        }

        // Keycloak primero (E2): si falla, nada persiste; el PUT re-ejecutado repara.
        foreach (var permiso in agregar)
        {
            await _keycloak.AddCompositeToRoleAsync(rol.ToString(), permiso.ToString(), cancellationToken);
        }
        foreach (var permiso in quitar)
        {
            await _keycloak.RemoveCompositeFromRoleAsync(rol.ToString(), permiso.ToString(), cancellationToken);
        }

        await _permisosRol.ReplaceForRolAsync(rol, deseados, cancellationToken);

        await _events.PublishPermisosRolActualizadosAsync(
            new PermisosRolActualizadosIntegrationEvent(rol.ToString(), permisosFinales, _timeProvider.GetUtcNow().UtcDateTime),
            cancellationToken);

        return new RolPermisosDto(rol.ToString(), permisosFinales, rol == RolUsuario.Administrador);
    }
}
```

Run GREEN: filtro del Step 2 → 4/4.

- [ ] **Step 4: Controller + requests + controller unit tests**

`GovernanceRequests.cs`:

```csharp
namespace Umbral.IdentityService.Api.Contracts;

public sealed record ActualizarPermisosRolRequest(IReadOnlyList<string> Permisos);
```

`GovernanceController.cs`:

```csharp
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Umbral.IdentityService.Api.Contracts;
using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Application.Queries;

namespace Umbral.IdentityService.Api.Controllers;

[ApiController]
[Route("identity/governance")]
[Authorize(Policy = "AdminOnly")]
public sealed class GovernanceController : ControllerBase
{
    private readonly ISender _sender;

    public GovernanceController(ISender sender) => _sender = sender;

    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new GetPermisosRolesQuery(), cancellationToken);
        return Ok(response);
    }

    [HttpPut("roles/{rol}/permisos")]
    public async Task<IActionResult> ActualizarPermisos(
        string rol,
        [FromBody] ActualizarPermisosRolRequest request,
        [FromServices] IValidator<ActualizarPermisosRolCommand> validator,
        CancellationToken cancellationToken)
    {
        var command = new ActualizarPermisosRolCommand(rol, request.Permisos);
        if (await ValidateAsync(validator, command, cancellationToken) is { } problem)
            return problem;

        var response = await _sender.Send(command, cancellationToken);
        return Ok(response);
    }

    private async Task<IActionResult?> ValidateAsync<T>(IValidator<T> validator, T command, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(command, cancellationToken);
        if (result.IsValid) return null;
        var problem = new ValidationProblemDetails();
        foreach (var error in result.Errors)
            problem.Errors[error.PropertyName] = new[] { error.ErrorMessage };
        return new BadRequestObjectResult(problem);
    }
}
```

**IMPORTANTE:** antes de escribir el helper `ValidateAsync`, LEER el de `UsersController` y copiar su forma EXACTA (el bloque de arriba es la forma esperada; si el real difiere, el real gana). Controller unit tests (`GovernanceControllerTests.cs`) siguiendo el patrón de `UsersControllerTests.cs` existente: GetRoles → Ok con response del sender; ActualizarPermisos válido → Ok; inválido → BadRequest sin llamar al sender. Registrar validator en DI si el proyecto los registra explícitamente (verificar cómo se registran los validators existentes — assembly scan o manual — y seguir el mismo mecanismo).

- [ ] **Step 5: Contract tests**

`GovernanceContractTests.cs` — usar `IdentityApiFactory` + `CreateClientAs("Administrador", ...)` (el TestAuthHandler ya simula composites SP-5a):

```csharp
// Casos (cuerpos con el estilo del proyecto; helper de client = el real de la factory):
// 1) GET identity/governance/roles con Administrador → 200; shape: roles.Length==3;
//    el rol "Administrador" trae privilegiosGobernanza=true; "Operador" contiene "GestionarPartidas" (seed BR-R03).
// 2) PUT identity/governance/roles/Operador/permisos body {"permisos":["GestionarEquipos"]} con Administrador
//    → 200; body.permisos == ["GestionarEquipos"]; GET posterior refleja el cambio.
// 3) PUT con permiso inválido {"permisos":["NoExiste"]} → 400.
// 4) PUT a rol inválido identity/governance/roles/SuperUser/permisos → 400.
// 5) GET con X-Test-Role "Participante" → 403 (gobernanza exige rol admin, no permiso funcional).
// 6) GET sin identidad → 401.
```

(El fake de `IKeycloakIdentityPort` de la factory ya soporta los métodos de composites — G3. Los contract tests corren con la DB del arnés del proyecto — el seed BR-R03 corre en Program.cs; si el arnés usa DB efímera relacional, el seed aplica; si el caso 1 no puede asumir seed, arrancar el test con un PUT que fije el estado y asertar sobre eso — documentar cuál camino tomó el arnés real en el reporte.)

- [ ] **Step 6: Suite completa + commit**

Run: `dotnet test services/identity-service/Umbral.IdentityService.sln`
Expected: PASS — unit +4 handler +N controller, contract +6.

```bash
git add <los 11 archivos exactos>
git commit -m "feat(sp5b): panel de permisos por rol — GET matriz + PUT set completo con diff Keycloak-first

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 5 (G5): Cambio de rol de usuario — PATCH /identity/users/{userId}/role

**Files:**
- Create: `services/identity-service/src/Umbral.IdentityService.Application/Commands/CambiarRolUsuarioCommand.cs`
- Create: `services/identity-service/src/Umbral.IdentityService.Application/Handlers/Commands/CambiarRolUsuarioCommandHandler.cs`
- Create: `services/identity-service/src/Umbral.IdentityService.Application/Validators/CambiarRolUsuarioCommandValidator.cs`
- Create: `services/identity-service/src/Umbral.IdentityService.Application/Exceptions/UsuarioConEquipoActivoException.cs`
- Modify: `services/identity-service/src/Umbral.IdentityService.Api/Controllers/UsersController.cs` (acción nueva)
- Modify: `services/identity-service/src/Umbral.IdentityService.Api/Contracts/UserRequests.cs` (request nuevo)
- Modify: `services/identity-service/src/Umbral.IdentityService.Api/Middleware/ExceptionHandlingMiddleware.cs` (2 mapeos 409)
- Create: `services/identity-service/tests/Umbral.IdentityService.UnitTests/CambiarRolUsuarioHandlerTests.cs`
- Modify: `services/identity-service/tests/Umbral.IdentityService.UnitTests/Api/UsersControllerTests.cs` (tests de la acción)
- Create: `services/identity-service/tests/Umbral.IdentityService.ContractTests/ChangeUserRoleContractTests.cs`

**Interfaces:**
- Consumes: `Usuario.CambiarRol` + `RolDeAdministradorInmutableException` (G2), `ChangeUserRealmRoleAsync` (G3), `PublishRolUsuarioModificadoAsync` + record (G1), `IEquipoRepository.ExistsActiveTeamByUserIdAsync(Guid, ct)` (existente), `IUsuarioRepository` (existente).
- Produces: `CambiarRolUsuarioCommand(Guid UserId, string Rol) : IRequest<CambiarRolUsuarioResponse>`; `CambiarRolUsuarioResponse(Guid UsuarioId, string Rol)` (en DTOs). Ruta `PATCH identity/users/{userId:guid}/role`.

- [ ] **Step 1: Command, validator, excepción, response**

```csharp
// Commands/CambiarRolUsuarioCommand.cs
using MediatR;
using Umbral.IdentityService.Application.DTOs;

namespace Umbral.IdentityService.Application.Commands;

public sealed record CambiarRolUsuarioCommand(Guid UserId, string Rol) : IRequest<CambiarRolUsuarioResponse>;
```

```csharp
// agregar a DTOs (archivo nuevo DTOs/CambiarRolUsuarioResponse.cs)
namespace Umbral.IdentityService.Application.DTOs;

public sealed record CambiarRolUsuarioResponse(Guid UsuarioId, string Rol);
```

```csharp
// Validators/CambiarRolUsuarioCommandValidator.cs
using FluentValidation;
using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Domain.Enums;

namespace Umbral.IdentityService.Application.Validators;

public sealed class CambiarRolUsuarioCommandValidator : AbstractValidator<CambiarRolUsuarioCommand>
{
    public CambiarRolUsuarioCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.Rol)
            .Must(rol => Enum.TryParse<RolUsuario>(rol, ignoreCase: false, out _))
            .WithMessage("Rol inválido: debe ser Administrador, Operador o Participante.");
    }
}
```

```csharp
// Exceptions/UsuarioConEquipoActivoException.cs
namespace Umbral.IdentityService.Application.Exceptions;

public sealed class UsuarioConEquipoActivoException : Exception
{
    public UsuarioConEquipoActivoException(Guid usuarioId)
        : base($"El usuario {usuarioId} tiene un equipo activo; debe salir o transferir el liderazgo antes del cambio de rol.")
    {
    }
}
```

- [ ] **Step 2: Tests del handler (RED)**

`CambiarRolUsuarioHandlerTests.cs` — fakes: `IUsuarioRepository` en memoria (lista, `UpdateAsync` marca flag), `IEquipoRepository` fake con `ExistsActiveTeamByUserIdAsync` configurable (resto NotImplemented), KeycloakFake de G4 (lista `CambiosDeRol` de `(keycloakId, viejo, nuevo)`), PublisherFake (graba `RolUsuarioModificadoIntegrationEvent`). Casos:

```csharp
// 1) Usuario inexistente → UserNotFoundException (la existente del proyecto).
// 2) Target Administrador → RolDeAdministradorInmutableException; Keycloak sin llamadas; repo sin Update.
// 3) Mismo rol → response con rol actual; Keycloak sin llamadas; sin evento; sin Update.
// 4) Participante con equipo activo (fake devuelve true para Guid.Parse(usuario.KeycloakId)) →
//    UsuarioConEquipoActivoException; Keycloak sin llamadas.
// 5) Participante→Operador feliz: Keycloak recibe ("kcId","Participante","Operador") ANTES del Update
//    (fake Keycloak lanza en un caso 5b → repo.UpdateRecibido == false y sin evento);
//    usuario.Rol == Operador; evento con RolAnterior "Participante" y RolNuevo "Operador".
// 6) Promoción Operador→Administrador procede.
```

Escribir los 7 [Fact] completos con ese contenido (asserts explícitos, nombres en español, estilo del proyecto). Run RED (compilación).

- [ ] **Step 3: Handler**

`CambiarRolUsuarioCommandHandler.cs`:

```csharp
using MediatR;
using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Application.DTOs;
using Umbral.IdentityService.Application.Exceptions;
using Umbral.IdentityService.Application.Interfaces;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Domain.Enums;

namespace Umbral.IdentityService.Application.Handlers.Commands;

public sealed class CambiarRolUsuarioCommandHandler : IRequestHandler<CambiarRolUsuarioCommand, CambiarRolUsuarioResponse>
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IEquipoRepository _equipos;
    private readonly IKeycloakIdentityPort _keycloak;
    private readonly IIdentityEventsPublisher _events;
    private readonly TimeProvider _timeProvider;

    public CambiarRolUsuarioCommandHandler(
        IUsuarioRepository usuarios,
        IEquipoRepository equipos,
        IKeycloakIdentityPort keycloak,
        IIdentityEventsPublisher events,
        TimeProvider timeProvider)
    {
        _usuarios = usuarios;
        _equipos = equipos;
        _keycloak = keycloak;
        _events = events;
        _timeProvider = timeProvider;
    }

    public async Task<CambiarRolUsuarioResponse> Handle(CambiarRolUsuarioCommand request, CancellationToken cancellationToken)
    {
        var usuario = await _usuarios.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new UserNotFoundException(request.UserId);

        var rolAnterior = usuario.Rol;
        var rolNuevo = Enum.Parse<RolUsuario>(request.Rol);

        if (rolAnterior == rolNuevo)
        {
            return new CambiarRolUsuarioResponse(usuario.UsuarioId, rolAnterior.ToString());
        }

        // Guard de dominio (admin inmutable, spec 5.3 paso 3) ANTES del check de equipo y de Keycloak.
        // Para un admin, CambiarRol lanza sin mutar; para el resto la mutación real ocurre tras Keycloak.
        if (rolAnterior == RolUsuario.Administrador)
        {
            usuario.CambiarRol(rolNuevo); // lanza RolDeAdministradorInmutableException
        }

        // La membresía de equipos está keyeada por el sub de Keycloak (Guid).
        if (Guid.TryParse(usuario.KeycloakId, out var keycloakGuid) &&
            await _equipos.ExistsActiveTeamByUserIdAsync(keycloakGuid, cancellationToken))
        {
            throw new UsuarioConEquipoActivoException(usuario.UsuarioId);
        }

        await _keycloak.ChangeUserRealmRoleAsync(usuario.KeycloakId, rolAnterior.ToString(), rolNuevo.ToString(), cancellationToken);

        usuario.CambiarRol(rolNuevo);
        await _usuarios.UpdateAsync(usuario, cancellationToken);

        await _events.PublishRolUsuarioModificadoAsync(
            new RolUsuarioModificadoIntegrationEvent(usuario.UsuarioId, rolAnterior.ToString(), rolNuevo.ToString(),
                _timeProvider.GetUtcNow().UtcDateTime),
            cancellationToken);

        return new CambiarRolUsuarioResponse(usuario.UsuarioId, rolNuevo.ToString());
    }
}
```

(Nota de orden: el chequeo admin-inmutable dispara vía `CambiarRol` sobre un admin ANTES de Keycloak — la línea `if (rolAnterior == Administrador) usuario.CambiarRol(...)` existe solo para lanzar el guard sin haber llamado a Keycloak; para no-admins la mutación real ocurre después de Keycloak. Verificar que `UserNotFoundException` existente acepte Guid en ctor; si su firma difiere, usar la firma real.)

Run GREEN: tests del Step 2.

- [ ] **Step 4: Controller + request + middleware + tests de controller**

`UserRequests.cs` — agregar: `public sealed record ChangeUserRoleRequest(string Rol);`

`UsersController.cs` — nueva acción después del `Update`:

```csharp
    [HttpPatch("{userId:guid}/role")]
    public async Task<IActionResult> ChangeRole(
        Guid userId,
        [FromBody] ChangeUserRoleRequest request,
        [FromServices] IValidator<CambiarRolUsuarioCommand> validator,
        CancellationToken cancellationToken)
    {
        var command = new CambiarRolUsuarioCommand(userId, request.Rol);
        if (await ValidateAsync(validator, command, cancellationToken) is { } problem)
            return problem;

        var response = await _sender.Send(command, cancellationToken);
        return Ok(response);
    }
```

`ExceptionHandlingMiddleware.cs` — agregar al switch (zona Conflict):

```csharp
            RolDeAdministradorInmutableException => HttpStatusCode.Conflict,
            UsuarioConEquipoActivoException      => HttpStatusCode.Conflict,
```

`UsersControllerTests.cs` — tests de `ChangeRole`: válido → Ok con response; inválido → BadRequest sin sender (patrón existente del archivo).

- [ ] **Step 5: Contract tests**

`ChangeUserRoleContractTests.cs` (factory + fake Keycloak de G3):

```csharp
// Casos:
// 1) PATCH identity/users/{id-inexistente}/role {"rol":"Operador"} con Administrador → 404.
// 2) Crear usuario Operador (flujo POST existente del arnés) → PATCH a "Administrador" → 200 {rol:"Administrador"};
//    PATCH posterior del MISMO usuario a "Operador" → 409 (ya es admin, inmutable).
// 3) PATCH con rol inválido {"rol":"SuperUser"} → 400.
// 4) PATCH con X-Test-Role "Operador" (no admin) → 403.
// 5) PATCH sin identidad → 401.
// (El 409-equipo-activo queda cubierto por el unit test del handler — armar un equipo real en
//  contract requiere el flujo completo de teams; si el arnés lo hace barato, agregarlo; si no,
//  documentar que quedó a nivel handler.)
```

- [ ] **Step 6: Suite completa + commit**

Run: `dotnet test services/identity-service/Umbral.IdentityService.sln`
Expected: PASS — unit +7 handler +2 controller, contract +5.

```bash
git add <los 10 archivos exactos>
git commit -m "feat(sp5b): cambio de rol de usuario — nunca-admin, 409 equipo activo, Keycloak-first

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 6 (G6): Gateway — sub-ruta identity-governance

**Files:**
- Modify: `gateway/src/Umbral.Gateway/appsettings.json`
- Modify: `gateway/tests/Umbral.Gateway.IntegrationTests/GatewayEndpointsTests.cs`

**Interfaces:**
- Consumes: policy `Administrador` y `TestAuthHandler`/`CreateClientWithRoles`/`AssertPolicyPassed` existentes (SP-5a T2).

- [ ] **Step 1: Tests (RED)**

Agregar a `GatewayEndpointsTests.cs`:

```csharp
    [Fact]
    public async Task IdentityGovernance_sin_token_es_401()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/identity/governance/roles");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task IdentityGovernance_con_Participante_es_403()
    {
        var client = CreateClientWithRoles("Participante");
        var response = await client.GetAsync("/identity/governance/roles");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task IdentityGovernance_con_Administrador_pasa_la_politica()
    {
        var client = CreateClientWithRoles("Administrador");
        var response = await client.GetAsync("/identity/governance/roles");
        AssertPolicyPassed(response);
    }
```

Run: `dotnet test gateway/Umbral.Gateway.sln --filter "IdentityGovernance"`
Expected: FAIL — `IdentityGovernance_con_Participante_es_403` da paso (hoy matchea `/identity/{**}` Default).

- [ ] **Step 2: Ruta**

En `appsettings.json`, dentro de `Routes`, agregar ANTES de `identity-users`:

```json
  "identity-governance": {
    "ClusterId": "identity",
    "Order": 1,
    "Match": { "Path": "/identity/governance/{**catch-all}" },
    "AuthorizationPolicy": "Administrador"
  },
```

- [ ] **Step 3: Suite + commit**

Run: `dotnet test gateway/Umbral.Gateway.sln`
Expected: PASS 17/17 (14 + 3).

```bash
git add gateway/src/Umbral.Gateway/appsettings.json gateway/tests/Umbral.Gateway.IntegrationTests/GatewayEndpointsTests.cs
git commit -m "feat(sp5b): sub-ruta identity-governance con política Administrador en gateway

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 7 (G7): Documentación — contratos de eventos §Transport, identity-api §Governance, GUIA, traceability

**Files:**
- Modify: `contracts/events/identity-events.md`
- Modify: `contracts/http/identity-api.md`
- Modify: `contracts/http/gateway-api.md` (fila de la ruta nueva en la matriz)
- Modify: `GUIA-LEVANTAMIENTO.md`
- Modify: `docs/04-sdd/traceability-matrix.md`

**Interfaces:**
- Consumes: todo lo materializado en G1-G6 (hashes con `git log --oneline`).

- [ ] **Step 1: `identity-events.md`**

Reemplazar la sección `## Rule` por `## Transport` (espejo del §Transport de `operaciones-sesion-events.md` — leerlo primero y seguir su estructura): exchange topic durable `umbral.identity`; routing keys de la tabla `IdentityEventRouting` (6, transcribir 1:1); envelope camelCase `{eventId, eventType, version, occurredAt, payload}`; dedupe por `eventId`; publicación best-effort post-save (ADR-0012); propagación de gobernanza per ADR-0013.
Actualizar el Event Registry: los 4 eventos de equipos → Status "Published to the broker since SP-5b (best-effort, ADR-0012)" + routing key; agregar filas `RolUsuarioModificado` `{ usuarioId, rolAnterior, rolNuevo, occurredOnUtc }` y `PermisosRolActualizados` `{ rol, permisos[], occurredOnUtc }` (samples 1:1 contra los records); `UsuarioCreado`/`CredencialTemporalEmitida` quedan "Payload not registered" + nota "diferido al slice de audit/notificaciones (SP-5b no los emite)".

- [ ] **Step 2: `identity-api.md` + `gateway-api.md`**

`identity-api.md`: sección `## Governance (SP-5b)` con los 3 endpoints (método, path, auth = rol `Administrador` vía policy AdminOnly + ruta gateway `Administrador`, request/response shapes de las secciones 5.1-5.3 del spec, errores 400/401/403/404/409/502 por endpoint). Incluir la nota de consistencia del spec §5.2/§10: tras un 502 parcial del PUT pueden quedar composites aplicados en Keycloak sin persistir en DB — el mismo PUT re-ejecutado repara (diff idempotente). Seguir el formato de tablas existente del archivo.
`gateway-api.md`: agregar la fila `identity-governance` (Order 1, `/identity/governance/{**catch-all}`, `Administrador`) a la matriz, y la misma fila en `gateway/gateway-context.md` si esa matriz vive duplicada ahí (verificar — SP-5a la actualizó en ambos).

- [ ] **Step 3: GUIA + traceability**

`GUIA-LEVANTAMIENTO.md`: bajo la sección de Identity (o junto a "Autenticación JWT (Partidas)"), agregar `## Broker RabbitMQ (Identity)` con: `RabbitMq__Enabled=true`, `RabbitMq__Host=localhost`, `RabbitMq__Port=5672`, `RabbitMq__User=umbral`, `RabbitMq__Password=16102005`, `RabbitMq__Exchange=umbral.identity`, nota best-effort (sin broker el servicio arranca igual) y smoke test opt-in `RABBITMQ_TEST_HOST=localhost dotnet test ... --filter RabbitMqRoundTripTests`.
`docs/04-sdd/traceability-matrix.md`: fila SP-5b formato fila SP-5a (mismas columnas, pipes internos escapados `\|`): spec `2026-07-04-sp5b-gobernanza-backend-design.md` (5128bf0), plan (hash del commit del plan), commits G1-G6 reales, suites finales reales.

- [ ] **Step 4: Verificación + commit**

```bash
git diff --check
grep -n "## Rule" contracts/events/identity-events.md   # esperado: sin output
```

```bash
git add contracts/events/identity-events.md contracts/http/identity-api.md contracts/http/gateway-api.md gateway/gateway-context.md GUIA-LEVANTAMIENTO.md docs/04-sdd/traceability-matrix.md
git commit -m "docs(sp5b): contrato de eventos identity con §Transport + governance API + GUIA + traceability

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

(Si `gateway-context.md` no necesitó cambio, omitir su add.)

---

## Verificación final del slice (controller, antes del review whole-branch)

```bash
dotnet test services/identity-service/Umbral.IdentityService.sln
dotnet test gateway/Umbral.Gateway.sln
python3 scripts/check-realm-composites.py
# opcional con broker vivo:
# docker compose -f infra/docker-compose.yml up -d rabbitmq
# RABBITMQ_TEST_HOST=localhost dotnet test services/identity-service/tests/Umbral.IdentityService.IntegrationTests --filter RabbitMqRoundTripTests
```

Todo verde + review final whole-branch (opus) sobre el rango del slice.
