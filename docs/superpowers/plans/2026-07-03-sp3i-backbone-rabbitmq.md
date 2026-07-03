# SP-3i — Backbone RabbitMQ · Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Primer publisher RabbitMQ real (dual-write en Operaciones de Sesión), evento de ubicación al broker, re-push SignalR de convocatorias pendientes al conectar, y consumidor de humo en Puntuaciones.

**Architecture:** El seam `ISesionEventsPublisher` (16 métodos, Composite que aísla fallos por delegado) gana un tercer publisher `RabbitMqSesionEventsPublisher` registrado condicionalmente por config. La publicación real se separa en un canal mínimo propio (`IRabbitMqPublishChannel`, 1 método) para que envelope/routing/best-effort sean unit-testeables sin broker. El seam crece a 17 métodos (ubicación). Puntuaciones consume con un `BackgroundService` que solo declara infraestructura y loguea.

**Tech Stack:** .NET 8, RabbitMQ.Client 6.8.1 (API `IModel`), System.Text.Json, xUnit.

**Spec:** `docs/superpowers/specs/2026-07-03-sp3i-backbone-rabbitmq-design.md`

## Global Constraints

- Rama: `feature/code-migration-sp-5`. Traceability/auditorías committeables (carve-out levantado desde SP-3h).
- Commits terminan SOLO con: `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`.
- `git add` SOLO de archivos exactos (nunca `-A`, `.`, ni directorios). PROHIBIDO `git checkout/restore/clean/stash/reset` de rango amplio.
- Cero `DateTime.Now/UtcNow` en `src/` — `TimeProvider` inyectado (doctrina D4). `Guid.NewGuid()` sí está permitido.
- Exchange: `umbral.operaciones-sesion` (topic, durable). Routing keys: `operaciones-sesion.<evento-kebab>.v1` por **mapa explícito** (sin kebab-case algorítmico). Envelope camelCase: `{ eventId, eventType, version, occurredAt, payload }`.
- Publisher best-effort estricto: nunca lanza al caller (try/catch + `LogError`).
- Baseline suites Operaciones: Unit 327 / Integration 28 / Contract 48. Comandos (desde `services/operaciones-sesion/`):
  - `dotnet test tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`
  - `dotnet test tests/Umbral.OperacionesSesion.IntegrationTests/Umbral.OperacionesSesion.IntegrationTests.csproj`
  - `dotnet test tests/Umbral.OperacionesSesion.ContractTests/Umbral.OperacionesSesion.ContractTests.csproj`
- Puntuaciones: `dotnet test tests/Umbral.Puntuaciones.UnitTests/Umbral.Puntuaciones.UnitTests.csproj` (desde `services/puntuaciones/`).
- No se comparte código entre servicios: el consumidor de Puntuaciones define su **propio** record de envelope (duplicado deliberado, doctrina de límites).

---

### Task B1: Transporte base (options, envelope, routing) + contrato

**Files:**
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Services/Messaging/RabbitMqOptions.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Services/Messaging/EventEnvelope.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Services/Messaging/SesionEventRouting.cs`
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Infrastructure/Messaging/SesionEventRoutingTests.cs` (create)
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Infrastructure/Messaging/EventEnvelopeTests.cs` (create)
- Modify: `contracts/events/operaciones-sesion-events.md` (sección Transport reemplaza "## Rule")

**Interfaces:**
- Produces: `RabbitMqOptions { Enabled(bool,false), Host(string?), Port(int,5672), User("guest"), Password("guest"), Exchange("umbral.operaciones-sesion") }`; `EventEnvelope.Create(string eventType, object payload, DateTime occurredAtUtc)` → `EventEnvelope(Guid EventId, string EventType, int Version, DateTime OccurredAt, object Payload)` + `EventEnvelope.SerializerOptions` (camelCase + enums string); `SesionEventRouting.RoutingKeyFor(string eventType)` → string (lanza `KeyNotFoundException` si el evento no está mapeado).

- [ ] **Step 1: Tests de routing (RED)**

```csharp
using System;
using Umbral.OperacionesSesion.Infrastructure.Services.Messaging;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Infrastructure.Messaging;

public class SesionEventRoutingTests
{
    [Theory]
    [InlineData("PartidaPublicadaEnLobby", "operaciones-sesion.partida-publicada-en-lobby.v1")]
    [InlineData("PartidaIniciada", "operaciones-sesion.partida-iniciada.v1")]
    [InlineData("JuegoActivado", "operaciones-sesion.juego-activado.v1")]
    [InlineData("PartidaCancelada", "operaciones-sesion.partida-cancelada.v1")]
    [InlineData("PartidaFinalizada", "operaciones-sesion.partida-finalizada.v1")]
    [InlineData("RespuestaTriviaValidada", "operaciones-sesion.respuesta-trivia-validada.v1")]
    [InlineData("PuntajeTriviaIncrementado", "operaciones-sesion.puntaje-trivia-incrementado.v1")]
    [InlineData("PreguntaTriviaActivada", "operaciones-sesion.pregunta-trivia-activada.v1")]
    [InlineData("PreguntaTriviaCerrada", "operaciones-sesion.pregunta-trivia-cerrada.v1")]
    [InlineData("TesoroQRValidado", "operaciones-sesion.tesoro-qr-validado.v1")]
    [InlineData("EtapaBDTGanada", "operaciones-sesion.etapa-bdt-ganada.v1")]
    [InlineData("EtapaBDTCerrada", "operaciones-sesion.etapa-bdt-cerrada.v1")]
    [InlineData("EtapaBDTActivada", "operaciones-sesion.etapa-bdt-activada.v1")]
    [InlineData("PistaEnviada", "operaciones-sesion.pista-enviada.v1")]
    [InlineData("ConvocatoriaCreada", "operaciones-sesion.convocatoria-creada.v1")]
    [InlineData("ConvocatoriaRespondida", "operaciones-sesion.convocatoria-respondida.v1")]
    [InlineData("UbicacionActualizada", "operaciones-sesion.ubicacion-actualizada.v1")]
    public void RoutingKeyFor_mapea_los_17_eventos(string eventType, string esperado)
        => Assert.Equal(esperado, SesionEventRouting.RoutingKeyFor(eventType));

    [Fact]
    public void RoutingKeyFor_evento_desconocido_lanza()
        => Assert.Throws<System.Collections.Generic.KeyNotFoundException>(
            () => SesionEventRouting.RoutingKeyFor("EventoInventado"));
}
```

- [ ] **Step 2: Tests de envelope (RED)**

```csharp
using System;
using System.Text.Json;
using Umbral.OperacionesSesion.Infrastructure.Services.Messaging;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Infrastructure.Messaging;

public class EventEnvelopeTests
{
    private static readonly DateTime T0 = new(2026, 7, 3, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_asigna_eventId_unico_y_version_1()
    {
        var a = EventEnvelope.Create("PartidaIniciada", new { partidaId = Guid.NewGuid() }, T0);
        var b = EventEnvelope.Create("PartidaIniciada", new { partidaId = Guid.NewGuid() }, T0);
        Assert.NotEqual(a.EventId, b.EventId);
        Assert.Equal(1, a.Version);
        Assert.Equal("PartidaIniciada", a.EventType);
        Assert.Equal(T0, a.OccurredAt);
    }

    [Fact]
    public void Serializa_camelCase_con_payload_anidado()
    {
        var envelope = EventEnvelope.Create("EtapaBDTGanada", new PayloadDePrueba(Guid.Empty, 10), T0);
        var json = JsonSerializer.Serialize(envelope, EventEnvelope.SerializerOptions);
        Assert.Contains("\"eventId\"", json);
        Assert.Contains("\"eventType\":\"EtapaBDTGanada\"", json);
        Assert.Contains("\"version\":1", json);
        Assert.Contains("\"payload\":{", json);
        Assert.Contains("\"puntaje\":10", json);
        Assert.DoesNotContain("\"EventId\"", json);
    }

    private sealed record PayloadDePrueba(Guid PartidaId, int Puntaje);
}
```

- [ ] **Step 3: Correr y verificar RED**

Run: `dotnet test tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj --filter "SesionEventRoutingTests|EventEnvelopeTests"`
Expected: FAIL de compilación (tipos no existen).

- [ ] **Step 4: Implementar los 3 tipos**

`RabbitMqOptions.cs`:
```csharp
namespace Umbral.OperacionesSesion.Infrastructure.Services.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";
    public bool Enabled { get; set; }
    public string? Host { get; set; }
    public int Port { get; set; } = 5672;
    public string User { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string Exchange { get; set; } = "umbral.operaciones-sesion";
}
```

`EventEnvelope.cs`:
```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Umbral.OperacionesSesion.Infrastructure.Services.Messaging;

public sealed record EventEnvelope(Guid EventId, string EventType, int Version, DateTime OccurredAt, object Payload)
{
    public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static EventEnvelope Create(string eventType, object payload, DateTime occurredAtUtc)
        => new(Guid.NewGuid(), eventType, 1, occurredAtUtc, payload);
}
```

`SesionEventRouting.cs`:
```csharp
namespace Umbral.OperacionesSesion.Infrastructure.Services.Messaging;

public static class SesionEventRouting
{
    // Mapa explícito (sin kebab algorítmico): el contrato documenta esta tabla 1:1.
    private static readonly IReadOnlyDictionary<string, string> Keys = new Dictionary<string, string>
    {
        ["PartidaPublicadaEnLobby"] = "operaciones-sesion.partida-publicada-en-lobby.v1",
        ["PartidaIniciada"] = "operaciones-sesion.partida-iniciada.v1",
        ["JuegoActivado"] = "operaciones-sesion.juego-activado.v1",
        ["PartidaCancelada"] = "operaciones-sesion.partida-cancelada.v1",
        ["PartidaFinalizada"] = "operaciones-sesion.partida-finalizada.v1",
        ["RespuestaTriviaValidada"] = "operaciones-sesion.respuesta-trivia-validada.v1",
        ["PuntajeTriviaIncrementado"] = "operaciones-sesion.puntaje-trivia-incrementado.v1",
        ["PreguntaTriviaActivada"] = "operaciones-sesion.pregunta-trivia-activada.v1",
        ["PreguntaTriviaCerrada"] = "operaciones-sesion.pregunta-trivia-cerrada.v1",
        ["TesoroQRValidado"] = "operaciones-sesion.tesoro-qr-validado.v1",
        ["EtapaBDTGanada"] = "operaciones-sesion.etapa-bdt-ganada.v1",
        ["EtapaBDTCerrada"] = "operaciones-sesion.etapa-bdt-cerrada.v1",
        ["EtapaBDTActivada"] = "operaciones-sesion.etapa-bdt-activada.v1",
        ["PistaEnviada"] = "operaciones-sesion.pista-enviada.v1",
        ["ConvocatoriaCreada"] = "operaciones-sesion.convocatoria-creada.v1",
        ["ConvocatoriaRespondida"] = "operaciones-sesion.convocatoria-respondida.v1",
        ["UbicacionActualizada"] = "operaciones-sesion.ubicacion-actualizada.v1",
    };

    public static string RoutingKeyFor(string eventType) => Keys[eventType];
}
```

- [ ] **Step 5: Correr y verificar GREEN + suite Unit completa**

Run: `dotnet test tests/Umbral.OperacionesSesion.UnitTests/Umbral.OperacionesSesion.UnitTests.csproj`
Expected: PASS, 347/347 (327 + 18 routing + 2 envelope).

- [ ] **Step 6: Contrato — sección Transport**

En `contracts/events/operaciones-sesion-events.md`, reemplazar la sección `## Rule` completa (líneas "Concrete exchange names… defines them.") por:

````markdown
## Transport (SP-3i)

Events are published to RabbitMQ (best-effort, after `SaveChanges`; see ADR-0012) **and** to SignalR where a realtime payload is documented. Delivery to the broker is enabled per environment via `RabbitMq__Enabled`.

- **Exchange:** `umbral.operaciones-sesion` — type `topic`, durable. Convention: one exchange per producing service.
- **Routing key:** `operaciones-sesion.<event-kebab>.v1` (explicit map, table below). Incompatible payload changes bump to `.v2` (new key; `v1` consumers keep working).
- **Envelope** (JSON camelCase, `content_type: application/json`): `{ "eventId": "guid", "eventType": "PascalCase name", "version": 1, "occurredAt": "datetime (UTC)", "payload": { …documented shape… } }`. Producers do not guarantee exactly-once; **consumers deduplicate by `eventId`**.
- **Smoke queue (SP-3i):** `puntuaciones.operaciones-sesion.all`, durable, binding `operaciones-sesion.#` (Puntuaciones; replaced by finer queues in SP-4).

| Event | Routing key |
|---|---|
| `PartidaPublicadaEnLobby` | `operaciones-sesion.partida-publicada-en-lobby.v1` |
| `PartidaIniciada` | `operaciones-sesion.partida-iniciada.v1` |
| `JuegoActivado` | `operaciones-sesion.juego-activado.v1` |
| `PartidaCancelada` | `operaciones-sesion.partida-cancelada.v1` |
| `PartidaFinalizada` | `operaciones-sesion.partida-finalizada.v1` |
| `RespuestaTriviaValidada` | `operaciones-sesion.respuesta-trivia-validada.v1` |
| `PuntajeTriviaIncrementado` | `operaciones-sesion.puntaje-trivia-incrementado.v1` |
| `PreguntaTriviaActivada` | `operaciones-sesion.pregunta-trivia-activada.v1` |
| `PreguntaTriviaCerrada` | `operaciones-sesion.pregunta-trivia-cerrada.v1` |
| `TesoroQRValidado` | `operaciones-sesion.tesoro-qr-validado.v1` |
| `EtapaBDTGanada` | `operaciones-sesion.etapa-bdt-ganada.v1` |
| `EtapaBDTCerrada` | `operaciones-sesion.etapa-bdt-cerrada.v1` |
| `EtapaBDTActivada` | `operaciones-sesion.etapa-bdt-activada.v1` |
| `PistaEnviada` | `operaciones-sesion.pista-enviada.v1` |
| `ConvocatoriaCreada` | `operaciones-sesion.convocatoria-creada.v1` |
| `ConvocatoriaRespondida` | `operaciones-sesion.convocatoria-respondida.v1` |
| `UbicacionActualizada` | `operaciones-sesion.ubicacion-actualizada.v1` |
````

(La fila `UbicacionActualizada` referencia el evento que la Task B3 añade al registry/payloads del mismo doc.)

- [ ] **Step 7: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Services/Messaging/RabbitMqOptions.cs services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Services/Messaging/EventEnvelope.cs services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Services/Messaging/SesionEventRouting.cs services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Infrastructure/Messaging/SesionEventRoutingTests.cs services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Infrastructure/Messaging/EventEnvelopeTests.cs contracts/events/operaciones-sesion-events.md
git commit -m "SP-3i B1: transporte base (options/envelope/routing) + contrato Transport

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task B2: Publisher RabbitMQ + registro condicional

**Files:**
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Services/Messaging/IRabbitMqPublishChannel.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Services/Messaging/RabbitMqPublishChannel.cs`
- Create: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Services/RabbitMqSesionEventsPublisher.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Umbral.OperacionesSesion.Infrastructure.csproj` (paquete)
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Program.cs:16-26` (registro condicional)
- Test: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Infrastructure/Messaging/RabbitMqSesionEventsPublisherTests.cs` (create)

**Interfaces:**
- Consumes: `EventEnvelope`, `SesionEventRouting`, `RabbitMqOptions` (B1); `ISesionEventsPublisher` (16 métodos actuales); `CompositeSesionEventsPublisher`.
- Produces: `IRabbitMqPublishChannel { void Publish(string routingKey, byte[] body); }`; `RabbitMqSesionEventsPublisher(IRabbitMqPublishChannel canal, TimeProvider timeProvider, ILogger<RabbitMqSesionEventsPublisher> logger) : ISesionEventsPublisher` — best-effort, nunca lanza. Task B3 le añadirá el método 17.

- [ ] **Step 1: Paquete**

En el `<ItemGroup>` de PackageReference del csproj de Infrastructure añadir:

```xml
    <PackageReference Include="RabbitMQ.Client" Version="6.8.1" />
```

- [ ] **Step 2: Canal mínimo**

`IRabbitMqPublishChannel.cs`:
```csharp
namespace Umbral.OperacionesSesion.Infrastructure.Services.Messaging;

// Seam mínimo de publicación: el publisher es unit-testeable sin broker;
// la conexión real solo se cubre con el integration test opt-in (B6).
public interface IRabbitMqPublishChannel
{
    void Publish(string routingKey, byte[] body);
}
```

`RabbitMqPublishChannel.cs`:
```csharp
using RabbitMQ.Client;

namespace Umbral.OperacionesSesion.Infrastructure.Services.Messaging;

public sealed class RabbitMqPublishChannel : IRabbitMqPublishChannel, IDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly object _lock = new();
    private IConnection? _connection;
    private IModel? _channel;

    public RabbitMqPublishChannel(RabbitMqOptions options) => _options = options;

    public void Publish(string routingKey, byte[] body)
    {
        lock (_lock)
        {
            EnsureChannel();
            var props = _channel!.CreateBasicProperties();
            props.ContentType = "application/json";
            props.DeliveryMode = 2; // persistent
            _channel.BasicPublish(_options.Exchange, routingKey, basicProperties: props, body: body);
        }
    }

    private void EnsureChannel()
    {
        if (_channel is { IsOpen: true }) return;
        _channel?.Dispose();
        if (_connection is not { IsOpen: true })
        {
            _connection?.Dispose();
            var factory = new ConnectionFactory
            {
                HostName = _options.Host,
                Port = _options.Port,
                UserName = _options.User,
                Password = _options.Password
            };
            _connection = factory.CreateConnection("umbral-operaciones-sesion-publisher");
        }
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(_options.Exchange, ExchangeType.Topic, durable: true, autoDelete: false);
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
```

- [ ] **Step 3: Tests del publisher (RED)**

```csharp
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Umbral.OperacionesSesion.Application.Interfaces;
using Umbral.OperacionesSesion.Infrastructure.Services;
using Umbral.OperacionesSesion.Infrastructure.Services.Messaging;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Infrastructure.Messaging;

public class RabbitMqSesionEventsPublisherTests
{
    private static readonly DateTime T0 = new(2026, 7, 3, 10, 0, 0, DateTimeKind.Utc);

    private sealed class CanalFake : IRabbitMqPublishChannel
    {
        public List<(string RoutingKey, byte[] Body)> Publicados { get; } = new();
        public void Publish(string routingKey, byte[] body) => Publicados.Add((routingKey, body));
    }

    private sealed class CanalRoto : IRabbitMqPublishChannel
    {
        public void Publish(string routingKey, byte[] body) => throw new InvalidOperationException("broker caído");
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;
        public FakeTimeProvider(DateTime now) => _now = new DateTimeOffset(now, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => _now;
    }

    private static RabbitMqSesionEventsPublisher Publisher(IRabbitMqPublishChannel canal) =>
        new(canal, new FakeTimeProvider(T0), NullLogger<RabbitMqSesionEventsPublisher>.Instance);

    [Fact]
    public async Task Publica_con_routing_key_y_envelope_correctos()
    {
        var canal = new CanalFake();
        var evento = new EtapaBDTGanadaEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), 10, 1234);

        await Publisher(canal).PublicarEtapaBDTGanadaAsync(evento, default);

        var (key, body) = Assert.Single(canal.Publicados);
        Assert.Equal("operaciones-sesion.etapa-bdt-ganada.v1", key);
        using var doc = JsonDocument.Parse(body);
        Assert.Equal("EtapaBDTGanada", doc.RootElement.GetProperty("eventType").GetString());
        Assert.Equal(1, doc.RootElement.GetProperty("version").GetInt32());
        Assert.NotEqual(Guid.Empty, doc.RootElement.GetProperty("eventId").GetGuid());
        Assert.Equal(10, doc.RootElement.GetProperty("payload").GetProperty("puntaje").GetInt32());
    }

    [Fact]
    public async Task Broker_caido_no_propaga_la_excepcion()
    {
        var publisher = Publisher(new CanalRoto());
        var evento = new PartidaIniciadaEvent(Guid.NewGuid(), Guid.NewGuid(), T0, Guid.NewGuid(), 1);

        var ex = await Record.ExceptionAsync(() => publisher.PublicarPartidaIniciadaAsync(evento, default));

        Assert.Null(ex);
    }

    [Fact]
    public async Task Cada_publicacion_lleva_eventId_distinto()
    {
        var canal = new CanalFake();
        var publisher = Publisher(canal);
        var evento = new ConvocatoriaCreadaEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        await publisher.PublicarConvocatoriaCreadaAsync(evento, default);
        await publisher.PublicarConvocatoriaCreadaAsync(evento, default);

        using var a = JsonDocument.Parse(canal.Publicados[0].Body);
        using var b = JsonDocument.Parse(canal.Publicados[1].Body);
        Assert.NotEqual(a.RootElement.GetProperty("eventId").GetGuid(), b.RootElement.GetProperty("eventId").GetGuid());
    }
}
```

Nota: verificar contra `PartidaIniciadaEvent`/`ConvocatoriaCreadaEvent` reales (en `Application/Interfaces/`) el orden de argumentos; si difiere, ajustar el arrange, no la aserción.

- [ ] **Step 4: Correr y verificar RED** (falla de compilación: `RabbitMqSesionEventsPublisher` no existe)

- [ ] **Step 5: Publisher**

`RabbitMqSesionEventsPublisher.cs` (16 métodos — patrón único `Publicar` privado):
```csharp
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Umbral.OperacionesSesion.Application.Interfaces;
using Umbral.OperacionesSesion.Infrastructure.Services.Messaging;

namespace Umbral.OperacionesSesion.Infrastructure.Services;

public sealed class RabbitMqSesionEventsPublisher : ISesionEventsPublisher
{
    private readonly IRabbitMqPublishChannel _canal;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<RabbitMqSesionEventsPublisher> _logger;

    public RabbitMqSesionEventsPublisher(IRabbitMqPublishChannel canal, TimeProvider timeProvider,
        ILogger<RabbitMqSesionEventsPublisher> logger)
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
            _canal.Publish(SesionEventRouting.RoutingKeyFor(eventType), body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fallo publicando {EventType} a RabbitMQ (best-effort, se continúa)", eventType);
        }
        return Task.CompletedTask;
    }

    public Task PublicarPartidaPublicadaEnLobbyAsync(PartidaPublicadaEnLobbyEvent evento, CancellationToken cancellationToken) => Publicar("PartidaPublicadaEnLobby", evento);
    public Task PublicarPartidaIniciadaAsync(PartidaIniciadaEvent evento, CancellationToken cancellationToken) => Publicar("PartidaIniciada", evento);
    public Task PublicarJuegoActivadoAsync(JuegoActivadoEvent evento, CancellationToken cancellationToken) => Publicar("JuegoActivado", evento);
    public Task PublicarPartidaCanceladaAsync(PartidaCanceladaEvent evento, CancellationToken cancellationToken) => Publicar("PartidaCancelada", evento);
    public Task PublicarPartidaFinalizadaAsync(PartidaFinalizadaEvent evento, CancellationToken cancellationToken) => Publicar("PartidaFinalizada", evento);
    public Task PublicarRespuestaTriviaValidadaAsync(RespuestaTriviaValidadaEvent evento, CancellationToken cancellationToken) => Publicar("RespuestaTriviaValidada", evento);
    public Task PublicarPuntajeTriviaIncrementadoAsync(PuntajeTriviaIncrementadoEvent evento, CancellationToken cancellationToken) => Publicar("PuntajeTriviaIncrementado", evento);
    public Task PublicarPreguntaTriviaActivadaAsync(PreguntaTriviaActivadaEvent evento, CancellationToken cancellationToken) => Publicar("PreguntaTriviaActivada", evento);
    public Task PublicarPreguntaTriviaCerradaAsync(PreguntaTriviaCerradaEvent evento, CancellationToken cancellationToken) => Publicar("PreguntaTriviaCerrada", evento);
    public Task PublicarTesoroQRValidadoAsync(TesoroQRValidadoEvent evento, CancellationToken cancellationToken) => Publicar("TesoroQRValidado", evento);
    public Task PublicarEtapaBDTGanadaAsync(EtapaBDTGanadaEvent evento, CancellationToken cancellationToken) => Publicar("EtapaBDTGanada", evento);
    public Task PublicarEtapaBDTCerradaAsync(EtapaBDTCerradaEvent evento, CancellationToken cancellationToken) => Publicar("EtapaBDTCerrada", evento);
    public Task PublicarEtapaBDTActivadaAsync(EtapaBDTActivadaEvent evento, CancellationToken cancellationToken) => Publicar("EtapaBDTActivada", evento);
    public Task PublicarPistaEnviadaAsync(PistaEnviadaEvent evento, CancellationToken cancellationToken) => Publicar("PistaEnviada", evento);
    public Task PublicarConvocatoriaCreadaAsync(ConvocatoriaCreadaEvent evento, CancellationToken cancellationToken) => Publicar("ConvocatoriaCreada", evento);
    public Task PublicarConvocatoriaRespondidaAsync(ConvocatoriaRespondidaEvent evento, CancellationToken cancellationToken) => Publicar("ConvocatoriaRespondida", evento);
}
```

- [ ] **Step 6: Registro condicional en Program.cs**

Reemplazar el bloque actual de líneas 17-26 por:

```csharp
builder.Services.AddScoped<Umbral.OperacionesSesion.Infrastructure.Services.NoOpSesionEventsPublisher>();
builder.Services.AddScoped<Umbral.OperacionesSesion.Api.Realtime.SignalRSesionEventsPublisher>();

var rabbitOptions = builder.Configuration
    .GetSection(Umbral.OperacionesSesion.Infrastructure.Services.Messaging.RabbitMqOptions.SectionName)
    .Get<Umbral.OperacionesSesion.Infrastructure.Services.Messaging.RabbitMqOptions>()
    ?? new Umbral.OperacionesSesion.Infrastructure.Services.Messaging.RabbitMqOptions();
var rabbitHabilitado = rabbitOptions.Enabled && !string.IsNullOrWhiteSpace(rabbitOptions.Host);
if (rabbitHabilitado)
{
    builder.Services.AddSingleton(rabbitOptions);
    builder.Services.AddSingleton<Umbral.OperacionesSesion.Infrastructure.Services.Messaging.IRabbitMqPublishChannel,
        Umbral.OperacionesSesion.Infrastructure.Services.Messaging.RabbitMqPublishChannel>();
    builder.Services.AddSingleton(TimeProvider.System);
    builder.Services.AddScoped<Umbral.OperacionesSesion.Infrastructure.Services.RabbitMqSesionEventsPublisher>();
}

builder.Services.AddScoped<ISesionEventsPublisher>(sp =>
{
    var publishers = new List<ISesionEventsPublisher>
    {
        sp.GetRequiredService<Umbral.OperacionesSesion.Infrastructure.Services.NoOpSesionEventsPublisher>(),
        sp.GetRequiredService<Umbral.OperacionesSesion.Api.Realtime.SignalRSesionEventsPublisher>(),
    };
    if (rabbitHabilitado)
    {
        publishers.Add(sp.GetRequiredService<Umbral.OperacionesSesion.Infrastructure.Services.RabbitMqSesionEventsPublisher>());
    }
    return new Umbral.OperacionesSesion.Infrastructure.Services.CompositeSesionEventsPublisher(
        publishers,
        sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Umbral.OperacionesSesion.Infrastructure.Services.CompositeSesionEventsPublisher>>());
});
```

Nota: `TimeProvider.System` ya se registra en `Application/DependencyInjection.cs` con `TryAdd`/`AddSingleton` — verificar; si ya está registrado, OMITIR la línea `AddSingleton(TimeProvider.System)` de arriba (duplicarla con `AddSingleton` plano no rompe, pero no ensuciar).

- [ ] **Step 7: Correr GREEN + 3 suites completas**

Run: las 3 suites de Operaciones.
Expected: Unit 350/350 (347 + 3 publisher), Integration 28/28, Contract 48/48. (Los ContractTests levantan el host vía `WebApplicationFactory`: sin `RabbitMq__Enabled` el publisher no se registra y nada cambia.)

- [ ] **Step 8: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Services/Messaging/IRabbitMqPublishChannel.cs services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Services/Messaging/RabbitMqPublishChannel.cs services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Services/RabbitMqSesionEventsPublisher.cs services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Umbral.OperacionesSesion.Infrastructure.csproj services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Program.cs services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Infrastructure/Messaging/RabbitMqSesionEventsPublisherTests.cs
git commit -m "SP-3i B2: publisher RabbitMQ best-effort + registro condicional en Composite

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task B3: Ubicación al broker (seam 17 + hub + impls + contrato)

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Interfaces/BdtRuntimeEvents.cs` (evento nuevo al final)
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Interfaces/ISesionEventsPublisher.cs` (método 17)
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Services/NoOpSesionEventsPublisher.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Services/CompositeSesionEventsPublisher.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Services/RabbitMqSesionEventsPublisher.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SignalRSesionEventsPublisher.cs`
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SesionHub.cs` (ctor + `EnviarUbicacion`)
- Modify: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/Fakes/FakeSesionEventsPublisher.cs` (+ cualquier otro fake/impl de test del seam — búsqueda repo-wide obligatoria)
- Modify: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/Realtime/SesionHubTests.cs` (helper `Construir` + tests nuevos)
- Modify: `contracts/events/operaciones-sesion-events.md` (registry + payload `UbicacionActualizada`)

**Interfaces:**
- Consumes: hub actual `SesionHub(ISesionPartidaRepository, TimeProvider)` con `EnviarUbicacion(double, double)` que ya construye `UbicacionParticipantePayload` y publica al grupo operador; `Publicar(eventType, payload)` privado del publisher Rabbit (B2).
- Produces: `UbicacionActualizadaEvent(Guid PartidaId, Guid ParticipanteId, double Latitud, double Longitud, DateTime Instante)`; `ISesionEventsPublisher.PublicarUbicacionActualizadaAsync(UbicacionActualizadaEvent, CancellationToken)`; hub ctor pasa a `SesionHub(ISesionPartidaRepository, TimeProvider, ISesionEventsPublisher)`. (Task B4 lo ampliará de nuevo.)

- [ ] **Step 1: Tests (RED)** — en `SesionHubTests.cs`, añadir junto a los tests de `EnviarUbicacion` existentes:

```csharp
    [Fact]
    public async Task EnviarUbicacion_dispara_el_seam_de_eventos_ademas_del_relay()
    {
        var partidaId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();
        var repo = new ISesionPartidaRepositorioFake();
        repo.Inner.Add(SesionDe(partidaId, participanteId));
        var groups = new FakeGroupManager();
        var eventos = new FakeSesionEventsPublisher();
        var hub = Construir(repo, Usuario(sub: participanteId.ToString(), rol: "Participante"), groups, eventos: eventos);
        await hub.SuscribirAPartida(partidaId);

        await hub.EnviarUbicacion(10.5, -66.9);

        var evento = Assert.Single(eventos.UbicacionesActualizadas);
        Assert.Equal(partidaId, evento.PartidaId);
        Assert.Equal(participanteId, evento.ParticipanteId);
        Assert.Equal(10.5, evento.Latitud);
        Assert.Equal(-66.9, evento.Longitud);
        Assert.Equal(T0.UtcDateTime, evento.Instante);
    }
```

(Si `T0` del archivo es `DateTime`, usar `T0` directo — mirar el tipo real; el helper `Construir` gana parámetro opcional `FakeSesionEventsPublisher? eventos = null`.)

En `RabbitMqSesionEventsPublisherTests.cs`:
```csharp
    [Fact]
    public async Task Ubicacion_publica_con_routing_key_de_ubicacion()
    {
        var canal = new CanalFake();
        var evento = new UbicacionActualizadaEvent(Guid.NewGuid(), Guid.NewGuid(), 10.5, -66.9, T0);

        await Publisher(canal).PublicarUbicacionActualizadaAsync(evento, default);

        var (key, _) = Assert.Single(canal.Publicados);
        Assert.Equal("operaciones-sesion.ubicacion-actualizada.v1", key);
    }
```

- [ ] **Step 2: Correr y verificar RED** (compilación: evento/método no existen)

- [ ] **Step 3: Implementación**

Al final de `BdtRuntimeEvents.cs`:
```csharp
// Sin SesionPartidaId (deliberado): el hub no lo tiene por conexión y no se consulta
// la sesión por cada ubicación (~2 s); Puntuaciones resuelve por PartidaId.
public sealed record UbicacionActualizadaEvent(
    Guid PartidaId, Guid ParticipanteId, double Latitud, double Longitud, DateTime Instante);
```

En `ISesionEventsPublisher.cs`, línea final del interface:
```csharp
    Task PublicarUbicacionActualizadaAsync(UbicacionActualizadaEvent evento, CancellationToken cancellationToken);
```

Implementaciones (una línea cada una, mismo patrón del archivo):
- NoOp: `=> Task.CompletedTask;`
- Composite: `=> FanOut(p => p.PublicarUbicacionActualizadaAsync(evento, cancellationToken));`
- RabbitMq: `=> Publicar("UbicacionActualizada", evento);`
- SignalR:
```csharp
    // No difunde: el relay vivo al grupo operador lo hace SesionHub.EnviarUbicacion directamente (BR-B07).
    public Task PublicarUbicacionActualizadaAsync(UbicacionActualizadaEvent evento, CancellationToken cancellationToken) =>
        Task.CompletedTask;
```
- `FakeSesionEventsPublisher` (tests): añadir `public List<UbicacionActualizadaEvent> UbicacionesActualizadas { get; } = new();` y el método que agrega. **Búsqueda repo-wide obligatoria** de TODOS los implementadores: `grep -rln "ISesionEventsPublisher" services/operaciones-sesion --include="*.cs"` — cada clase que implemente la interface necesita el método (lección B13).

`SesionHub.cs`: ctor pasa a
```csharp
    private readonly ISesionEventsPublisher _events;

    public SesionHub(ISesionPartidaRepository repo, TimeProvider timeProvider, ISesionEventsPublisher events)
    {
        _repo = repo;
        _timeProvider = timeProvider;
        _events = events;
    }
```
y `EnviarUbicacion`, tras el `SendAsync` existente al grupo operador:
```csharp
        await _events.PublicarUbicacionActualizadaAsync(
            new UbicacionActualizadaEvent(partidaId, participanteId, latitud, longitud, payload.TimestampUtc),
            Context.ConnectionAborted);
```
(`using Umbral.OperacionesSesion.Application.Interfaces;` nuevo en el hub. El nombre del campo timestamp del payload es `TimestampUtc` — verificar contra `UbicacionParticipantePayload`.)

Helper `Construir` de `SesionHubTests.cs`: parámetro opcional `FakeSesionEventsPublisher? eventos = null`, pasa `eventos ?? new FakeSesionEventsPublisher()` al ctor.

- [ ] **Step 4: Contrato** — en `contracts/events/operaciones-sesion-events.md`: fila nueva al final del Event Registry:

```markdown
| `UbicacionActualizada` (SP-3i) | Un participante BDT envía su ubicación (~cada 2 s) durante un juego activo. | Defined by SDD | Payload registered (SP-3i) |
```

y sección de payload al final del doc:

````markdown
### `UbicacionActualizada` (SP-3i)

Emitted to the broker for deferred audit each time a participant sends their location during an active BDT game. The live relay to the operator group stays in SignalR (`SesionHub.EnviarUbicacion`, BR-B07) — this event is transport for audit only. **No `sesionPartidaId`** (deliberate: the hub does not hold it per-connection and no query is made per location ping; consumers resolve by `partidaId`).

```json
{
  "partidaId": "guid",
  "participanteId": "guid",
  "latitud": 10.5,
  "longitud": -66.9,
  "instante": "datetime"
}
```
````

- [ ] **Step 5: GREEN + 3 suites.** Expected: Unit 352/352 (350 + 1 hub + 1 publisher), Integration 28, Contract 48.

- [ ] **Step 6: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Interfaces/BdtRuntimeEvents.cs services/operaciones-sesion/src/Umbral.OperacionesSesion.Application/Interfaces/ISesionEventsPublisher.cs services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Services/NoOpSesionEventsPublisher.cs services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Services/CompositeSesionEventsPublisher.cs services/operaciones-sesion/src/Umbral.OperacionesSesion.Infrastructure/Services/RabbitMqSesionEventsPublisher.cs services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SignalRSesionEventsPublisher.cs services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SesionHub.cs services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Application/Fakes/FakeSesionEventsPublisher.cs services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/Realtime/SesionHubTests.cs contracts/events/operaciones-sesion-events.md
git commit -m "SP-3i B3: UbicacionActualizada al broker (seam 17) + hub dispara seam

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

(Si la búsqueda repo-wide encontró más implementadores del seam, añadirlos al `git add` con su path exacto.)

---

### Task B4: Re-push de convocatorias pendientes al conectar

**Files:**
- Modify: `services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SesionHub.cs` (ctor + `OnConnectedAsync`)
- Modify: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/Realtime/SesionHubTests.cs` (helper + 4 tests)

**Interfaces:**
- Consumes: `ObtenerMisConvocatoriasPendientesQuery(Guid UsuarioId) : IRequest<IReadOnlyList<ConvocatoriaPendienteDto>>`; `ConvocatoriaPendienteDto(Guid ConvocatoriaId, Guid PartidaId, Guid EquipoId, DateTime FechaEnvio)`; `ConvocatoriaCreadaPayload(Guid PartidaId, Guid EquipoId, Guid ConvocatoriaId, Guid UsuarioId)`; `SesionRealtimeMessages.ConvocatoriaCreada`; `MediatR.ISender`; hub ctor de B3.
- Produces: hub ctor final `SesionHub(ISesionPartidaRepository, TimeProvider, ISesionEventsPublisher, ISender, ILogger<SesionHub>)`; `OnConnectedAsync` re-emite `ConvocatoriaCreada` al `Clients.Caller` por cada pendiente.

- [ ] **Step 1: Tests (RED)** — en `SesionHubTests.cs`. El helper `Construir` gana `ISender? sender = null` (default: fake que devuelve lista vacía). Usar el `FakeSender` del proyecto si es accesible (namespace `Umbral.OperacionesSesion.UnitTests.Api`); si su ctor no encaja con `IReadOnlyList<ConvocatoriaPendienteDto>`, definir fake local mínimo:

```csharp
    private sealed class SenderDeConvocatorias : MediatR.ISender
    {
        private readonly IReadOnlyList<Umbral.OperacionesSesion.Application.DTOs.ConvocatoriaPendienteDto> _pendientes;
        public bool Lanza { get; init; }
        public SenderDeConvocatorias(IReadOnlyList<Umbral.OperacionesSesion.Application.DTOs.ConvocatoriaPendienteDto> pendientes)
            => _pendientes = pendientes;

        public Task<TResponse> Send<TResponse>(MediatR.IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (Lanza) throw new InvalidOperationException("query rota");
            return Task.FromResult((TResponse)(object)_pendientes);
        }
        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : MediatR.IRequest
            => throw new NotSupportedException();
        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(MediatR.IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
```

(Los miembros exactos de `ISender` dependen de la versión de MediatR del proyecto — compilar y ajustar las firmas no implementadas; las dos primeras son las que importan.)

Tests (los nombres de fakes de captura — `FakeClients`/`FakeClientProxy` — existen en el archivo; mirar cómo capturan `SendAsync` en los tests de `EnviarUbicacion` y reutilizar el mismo patrón para `Clients.Caller`):

```csharp
    [Fact]
    public async Task Al_conectar_reemite_las_convocatorias_pendientes_al_caller()
    {
        var usuario = Guid.NewGuid();
        var pendientes = new[]
        {
            new Umbral.OperacionesSesion.Application.DTOs.ConvocatoriaPendienteDto(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), T0.UtcDateTime),
            new Umbral.OperacionesSesion.Application.DTOs.ConvocatoriaPendienteDto(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), T0.UtcDateTime),
        };
        var clients = new FakeClients();
        var hub = Construir(new ISesionPartidaRepositorioFake(), Usuario(sub: usuario.ToString(), rol: "Participante"),
            new FakeGroupManager(), clients, sender: new SenderDeConvocatorias(pendientes));

        await hub.OnConnectedAsync();

        Assert.Equal(2, clients.MensajesAlCaller.Count(m => m.Metodo == SesionRealtimeMessages.ConvocatoriaCreada));
    }

    [Fact]
    public async Task Al_conectar_sin_pendientes_no_emite_nada()
    {
        var clients = new FakeClients();
        var hub = Construir(new ISesionPartidaRepositorioFake(), Usuario(sub: Guid.NewGuid().ToString(), rol: "Participante"),
            new FakeGroupManager(), clients, sender: new SenderDeConvocatorias(Array.Empty<Umbral.OperacionesSesion.Application.DTOs.ConvocatoriaPendienteDto>()));

        await hub.OnConnectedAsync();

        Assert.Empty(clients.MensajesAlCaller);
    }

    [Fact]
    public async Task Operador_al_conectar_no_dispara_query_ni_mensajes()
    {
        var clients = new FakeClients();
        var sender = new SenderDeConvocatorias(Array.Empty<Umbral.OperacionesSesion.Application.DTOs.ConvocatoriaPendienteDto>()) { Lanza = true };
        var hub = Construir(new ISesionPartidaRepositorioFake(), Usuario(sub: null, rol: "Operador"),
            new FakeGroupManager(), clients, sender: sender);

        await hub.OnConnectedAsync(); // si consultara, SenderDeConvocatorias lanzaría

        Assert.Empty(clients.MensajesAlCaller);
    }

    [Fact]
    public async Task Fallo_de_la_query_no_tumba_la_conexion()
    {
        var sender = new SenderDeConvocatorias(Array.Empty<Umbral.OperacionesSesion.Application.DTOs.ConvocatoriaPendienteDto>()) { Lanza = true };
        var hub = Construir(new ISesionPartidaRepositorioFake(), Usuario(sub: Guid.NewGuid().ToString(), rol: "Participante"),
            new FakeGroupManager(), new FakeClients(), sender: sender);

        var ex = await Record.ExceptionAsync(() => hub.OnConnectedAsync());

        Assert.Null(ex);
    }
```

Nota: si `FakeClients` no expone captura del Caller (`MensajesAlCaller`), ampliar el fake existente con esa lista siguiendo su propio estilo — sin crear un framework nuevo.

- [ ] **Step 2: RED** (OnConnectedAsync no existe / ctor no encaja)

- [ ] **Step 3: Implementación en `SesionHub.cs`**

Ctor final (campos nuevos `_sender`, `_logger`):
```csharp
    public SesionHub(ISesionPartidaRepository repo, TimeProvider timeProvider, ISesionEventsPublisher events,
        ISender sender, ILogger<SesionHub> logger)
    {
        _repo = repo;
        _timeProvider = timeProvider;
        _events = events;
        _sender = sender;
        _logger = logger;
    }
```

```csharp
    // Re-push de cortesía (SP-3i): el convocado offline recibe sus convocatorias pendientes al volver.
    // Datos → MediatR (ADR-0011 reserva el repositorio del hub para membresía de grupos).
    public override async Task OnConnectedAsync()
    {
        var user = Context.User;
        var sub = user?.FindFirst("sub")?.Value ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!(user?.IsInRole("Operador") ?? false) && sub is not null && Guid.TryParse(sub, out var usuarioId))
        {
            try
            {
                var pendientes = await _sender.Send(new ObtenerMisConvocatoriasPendientesQuery(usuarioId), Context.ConnectionAborted);
                foreach (var c in pendientes)
                {
                    await Clients.Caller.SendAsync(SesionRealtimeMessages.ConvocatoriaCreada,
                        new ConvocatoriaCreadaPayload(c.PartidaId, c.EquipoId, c.ConvocatoriaId, usuarioId),
                        Context.ConnectionAborted);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Fallo el re-push de convocatorias pendientes para {UsuarioId}; la conexión continúa", usuarioId);
            }
        }
        await base.OnConnectedAsync();
    }
```

Usings nuevos: `MediatR`, `Microsoft.Extensions.Logging`, `Umbral.OperacionesSesion.Application.Queries`, `Umbral.OperacionesSesion.Application.DTOs` (según dónde viva `ConvocatoriaCreadaPayload` — está en `Api/Realtime`, mismo namespace, sin using).

Helper `Construir`: `ISender? sender = null, ...` → default `new SenderDeConvocatorias(Array.Empty<ConvocatoriaPendienteDto>())`; `ILogger<SesionHub>` → `NullLogger<SesionHub>.Instance`.

- [ ] **Step 4: GREEN + 3 suites.** Expected: Unit 356/356 (352 + 4), Integration 28, Contract 48.

- [ ] **Step 5: Commit**

```bash
git add services/operaciones-sesion/src/Umbral.OperacionesSesion.Api/Realtime/SesionHub.cs services/operaciones-sesion/tests/Umbral.OperacionesSesion.UnitTests/Api/Realtime/SesionHubTests.cs
git commit -m "SP-3i B4: re-push SignalR de convocatorias pendientes en OnConnectedAsync

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task B5: Consumidor de humo en Puntuaciones

**Files:**
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Api/Workers/OperacionesSesionEventsConsumer.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Api/Workers/RabbitMqConsumerOptions.cs`
- Create: `services/puntuaciones/src/Umbral.Puntuaciones.Api/Workers/EnvelopeReader.cs`
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Api/Program.cs` (options + AddHostedService)
- Modify: `services/puntuaciones/src/Umbral.Puntuaciones.Api/Umbral.Puntuaciones.Api.csproj` (paquete RabbitMQ.Client)
- Test: `services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Workers/EnvelopeReaderTests.cs` (create)

**Interfaces:**
- Consumes: envelope del contrato (`eventId`/`eventType`/`version`/`occurredAt`/`payload`), exchange `umbral.operaciones-sesion`, cola `puntuaciones.operaciones-sesion.all`, binding `operaciones-sesion.#`. **Record propio** — no se referencia código de Operaciones (límite duro entre servicios).
- Produces: `EnvelopeReader.TryRead(ReadOnlySpan<byte> body, out EnvelopeResumen? envelope)`; `EnvelopeResumen(Guid EventId, string EventType, int Version, DateTime OccurredAt)`; worker `OperacionesSesionEventsConsumer : BackgroundService`.

- [ ] **Step 1: Test del reader (RED)**

```csharp
using System;
using System.Text;
using Umbral.Puntuaciones.Api.Workers;
using Xunit;

namespace Umbral.Puntuaciones.UnitTests.Workers;

public class EnvelopeReaderTests
{
    [Fact]
    public void TryRead_envelope_valido_extrae_los_campos()
    {
        var json = "{\"eventId\":\"3f2504e0-4f89-11d3-9a0c-0305e82c3301\",\"eventType\":\"EtapaBDTGanada\",\"version\":1,\"occurredAt\":\"2026-07-03T10:00:00Z\",\"payload\":{\"puntaje\":10}}";

        var ok = EnvelopeReader.TryRead(Encoding.UTF8.GetBytes(json), out var envelope);

        Assert.True(ok);
        Assert.Equal("EtapaBDTGanada", envelope!.EventType);
        Assert.Equal(Guid.Parse("3f2504e0-4f89-11d3-9a0c-0305e82c3301"), envelope.EventId);
        Assert.Equal(1, envelope.Version);
    }

    [Theory]
    [InlineData("no es json")]
    [InlineData("{}")]
    [InlineData("{\"eventType\":\"X\"}")]
    public void TryRead_malformado_devuelve_false(string body)
    {
        var ok = EnvelopeReader.TryRead(System.Text.Encoding.UTF8.GetBytes(body), out var envelope);
        Assert.False(ok);
        Assert.Null(envelope);
    }
}
```

Nota: si `Umbral.Puntuaciones.UnitTests` no referencia el proyecto Api, añadir el `ProjectReference` al csproj de tests (mirar cómo lo hace Operaciones).

- [ ] **Step 2: RED** (tipos no existen)

- [ ] **Step 3: Implementación**

`RabbitMqConsumerOptions.cs`:
```csharp
namespace Umbral.Puntuaciones.Api.Workers;

public sealed class RabbitMqConsumerOptions
{
    public const string SectionName = "RabbitMq";
    public bool Enabled { get; set; }
    public string? Host { get; set; }
    public int Port { get; set; } = 5672;
    public string User { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string Exchange { get; set; } = "umbral.operaciones-sesion";
    public string Queue { get; set; } = "puntuaciones.operaciones-sesion.all";
    public string Binding { get; set; } = "operaciones-sesion.#";
}
```

`EnvelopeReader.cs`:
```csharp
using System.Text.Json;

namespace Umbral.Puntuaciones.Api.Workers;

public sealed record EnvelopeResumen(Guid EventId, string EventType, int Version, DateTime OccurredAt);

public static class EnvelopeReader
{
    public static bool TryRead(ReadOnlySpan<byte> body, out EnvelopeResumen? envelope)
    {
        envelope = null;
        try
        {
            using var doc = JsonDocument.Parse(body.ToArray());
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("eventId", out var id) || !id.TryGetGuid(out var eventId) ||
                !root.TryGetProperty("eventType", out var type) || type.GetString() is not { Length: > 0 } eventType ||
                !root.TryGetProperty("version", out var ver) || !ver.TryGetInt32(out var version) ||
                !root.TryGetProperty("occurredAt", out var at) || !at.TryGetDateTime(out var occurredAt))
            {
                return false;
            }
            envelope = new EnvelopeResumen(eventId, eventType, version, occurredAt);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
```

`OperacionesSesionEventsConsumer.cs`:
```csharp
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Umbral.Puntuaciones.Api.Workers;

// Consumidor de humo (SP-3i): declara la infraestructura para SP-4 y loguea cada evento.
// Sin DB, sin proyecciones. SP-4 lo reemplaza por el consumidor real.
public sealed class OperacionesSesionEventsConsumer : BackgroundService
{
    private readonly RabbitMqConsumerOptions _options;
    private readonly ILogger<OperacionesSesionEventsConsumer> _logger;

    public OperacionesSesionEventsConsumer(RabbitMqConsumerOptions options, ILogger<OperacionesSesionEventsConsumer> logger)
    {
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.Host))
        {
            _logger.LogWarning("RabbitMQ deshabilitado o sin host: el consumidor de eventos no arranca.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _options.Host,
                    Port = _options.Port,
                    UserName = _options.User,
                    Password = _options.Password,
                    DispatchConsumersAsync = false
                };
                using var connection = factory.CreateConnection("umbral-puntuaciones-consumer");
                using var channel = connection.CreateModel();
                channel.ExchangeDeclare(_options.Exchange, ExchangeType.Topic, durable: true, autoDelete: false);
                channel.QueueDeclare(_options.Queue, durable: true, exclusive: false, autoDelete: false);
                channel.QueueBind(_options.Queue, _options.Exchange, _options.Binding);

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (_, ea) =>
                {
                    if (EnvelopeReader.TryRead(ea.Body.Span, out var envelope))
                    {
                        _logger.LogInformation(
                            "Evento recibido {EventType} {EventId} v{Version} (rk {RoutingKey}, occurredAt {OccurredAt:O})",
                            envelope!.EventType, envelope.EventId, envelope.Version, ea.RoutingKey, envelope.OccurredAt);
                    }
                    else
                    {
                        _logger.LogWarning("Envelope malformado en {RoutingKey}; se descarta (ack).", ea.RoutingKey);
                    }
                    channel.BasicAck(ea.DeliveryTag, multiple: false);
                };
                channel.BasicConsume(_options.Queue, autoAck: false, consumer);

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Conexión RabbitMQ caída; reintento en 30 s.");
                try { await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); }
                catch (OperationCanceledException) { return; }
            }
        }
    }
}
```

`Program.cs` de Puntuaciones — tras `AddControllers()`:
```csharp
var rabbitOptions = builder.Configuration
    .GetSection(Umbral.Puntuaciones.Api.Workers.RabbitMqConsumerOptions.SectionName)
    .Get<Umbral.Puntuaciones.Api.Workers.RabbitMqConsumerOptions>()
    ?? new Umbral.Puntuaciones.Api.Workers.RabbitMqConsumerOptions();
builder.Services.AddSingleton(rabbitOptions);
builder.Services.AddHostedService<Umbral.Puntuaciones.Api.Workers.OperacionesSesionEventsConsumer>();
```

Csproj de Api de Puntuaciones: `<PackageReference Include="RabbitMQ.Client" Version="6.8.1" />` (misma versión que Operaciones).

- [ ] **Step 4: GREEN.** Run: `dotnet test tests/Umbral.Puntuaciones.UnitTests/Umbral.Puntuaciones.UnitTests.csproj` (desde `services/puntuaciones/`) — los 4 tests nuevos (1 válido + 3 malformados) + los existentes del proyecto verdes. Correr también las otras 2 suites de Puntuaciones si tienen tests (`IntegrationTests`, `ContractTests`) para confirmar que el host sigue levantando.

- [ ] **Step 5: Commit**

```bash
git add services/puntuaciones/src/Umbral.Puntuaciones.Api/Workers/OperacionesSesionEventsConsumer.cs services/puntuaciones/src/Umbral.Puntuaciones.Api/Workers/RabbitMqConsumerOptions.cs services/puntuaciones/src/Umbral.Puntuaciones.Api/Workers/EnvelopeReader.cs services/puntuaciones/src/Umbral.Puntuaciones.Api/Program.cs services/puntuaciones/src/Umbral.Puntuaciones.Api/Umbral.Puntuaciones.Api.csproj services/puntuaciones/tests/Umbral.Puntuaciones.UnitTests/Workers/EnvelopeReaderTests.cs
git commit -m "SP-3i B5: consumidor de humo RabbitMQ en Puntuaciones (cola + log)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

(Si el csproj de UnitTests de Puntuaciones ganó un ProjectReference, incluirlo en el add.)

---

### Task B6: Integration opt-in + ADR-0012 + cierre

**Files:**
- Create: `services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests/RabbitMqRoundTripTests.cs`
- Create: `docs/05-decisions/ADR-0012-publicacion-eventos-best-effort.md`
- Modify: `docs/04-sdd/traceability-matrix.md` (fila SP-3i)

**Interfaces:**
- Consumes: `RabbitMqOptions`, `RabbitMqPublishChannel`, `RabbitMqSesionEventsPublisher`, `EventEnvelope` (B1/B2); hashes reales de B1..B5 (`git log --oneline -6`).

- [ ] **Step 1: Integration test opt-in**

```csharp
using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Client;
using Umbral.OperacionesSesion.Application.Interfaces;
using Umbral.OperacionesSesion.Infrastructure.Services;
using Umbral.OperacionesSesion.Infrastructure.Services.Messaging;
using Xunit;

namespace Umbral.OperacionesSesion.IntegrationTests;

// Opt-in: requiere broker real. Correr con:
//   docker compose -f infra/docker-compose.yml up -d rabbitmq
//   RABBITMQ_TEST_HOST=localhost dotnet test tests/Umbral.OperacionesSesion.IntegrationTests/... --filter RabbitMqRoundTripTests
// Sin RABBITMQ_TEST_HOST el test retorna sin assertar (skip suave, sin dependencia de paquetes extra).
public class RabbitMqRoundTripTests
{
    [Fact]
    public async Task Publicar_llega_a_una_cola_bindeada_con_el_envelope_esperado()
    {
        var host = Environment.GetEnvironmentVariable("RABBITMQ_TEST_HOST");
        if (string.IsNullOrWhiteSpace(host)) return; // opt-in

        var options = new RabbitMqOptions { Enabled = true, Host = host };
        using var canal = new RabbitMqPublishChannel(options);
        var publisher = new RabbitMqSesionEventsPublisher(canal, TimeProvider.System,
            NullLogger<RabbitMqSesionEventsPublisher>.Instance);

        var factory = new ConnectionFactory { HostName = host, Port = options.Port, UserName = options.User, Password = options.Password };
        using var connection = factory.CreateConnection("umbral-integration-test");
        using var channel = connection.CreateModel();
        channel.ExchangeDeclare(options.Exchange, ExchangeType.Topic, durable: true, autoDelete: false);
        var queue = channel.QueueDeclare($"test.roundtrip.{Guid.NewGuid():N}", durable: false, exclusive: true, autoDelete: true).QueueName;
        channel.QueueBind(queue, options.Exchange, "operaciones-sesion.#");

        var partidaId = Guid.NewGuid();
        await publisher.PublicarPartidaIniciadaAsync(
            new PartidaIniciadaEvent(partidaId, Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), 1), default);

        BasicGetResult? result = null;
        for (var i = 0; i < 50 && result is null; i++) // hasta ~5 s
        {
            result = channel.BasicGet(queue, autoAck: true);
            if (result is null) await Task.Delay(100);
        }

        Assert.NotNull(result);
        Assert.Equal("operaciones-sesion.partida-iniciada.v1", result!.RoutingKey);
        using var doc = JsonDocument.Parse(Encoding.UTF8.GetString(result.Body.ToArray()));
        Assert.Equal("PartidaIniciada", doc.RootElement.GetProperty("eventType").GetString());
        Assert.Equal(partidaId, doc.RootElement.GetProperty("payload").GetProperty("partidaId").GetGuid());
    }
}
```

Nota: `DateTime.UtcNow` es aceptable AQUÍ (proyecto de test, no `src/`; la doctrina D4 aplica a producción). Verificar el orden de argumentos de `PartidaIniciadaEvent` contra el record real.

- [ ] **Step 2: Correr las 3 suites de Operaciones SIN la variable** — Expected: Unit 356, Integration 29/29 (28 + este que retorna vacío), Contract 48.

- [ ] **Step 3 (opcional, si hay docker disponible): humo real** — `docker compose -f infra/docker-compose.yml up -d rabbitmq` y correr con `RABBITMQ_TEST_HOST=localhost` el filtro `RabbitMqRoundTripTests`; capturar el PASS en el reporte. Si no hay docker en el entorno, anotarlo en el reporte (no bloquea).

- [ ] **Step 4: ADR-0012**

```markdown
# ADR-0012 — Publicación de eventos best-effort post-save; outbox transaccional diferido

- **Estado:** Accepted
- **Fecha:** 2026-07-03
- **Contexto de origen:** slice SP-3i (backbone RabbitMQ), spec `docs/superpowers/specs/2026-07-03-sp3i-backbone-rabbitmq-design.md`.

## Contexto

Los handlers de Operaciones de Sesión persisten con `SaveChanges` y luego publican al seam de eventos (patrón save→publish verificado por la auditoría 2026-07-02, dimensión D7). Con RabbitMQ real en el Composite, la publicación al broker ocurre fuera de la transacción de base de datos: un crash del proceso entre el save y el publish pierde el evento; un fallo del broker lo pierde también (se loguea y se continúa).

## Decisión

Se acepta la publicación **best-effort**: el publisher RabbitMQ captura toda excepción, la loguea (`LogError`) y nunca falla el request ni el scheduler. No se implementa outbox transaccional en SP-3i.

## Justificación

1. Puntuaciones (SP-4) es un modelo de proyección **reconstruible**; la pérdida puntual de un evento no corrompe estado de negocio irrecuperable.
2. El Composite ya aísla fallos por delegado; la semántica user-facing (SignalR) no depende del broker.
3. El outbox (tabla + dispatcher + idempotencia de despacho) duplica el tamaño del slice sin necesidad presente.

## Criterio de activación del outbox (cuándo revisar esta decisión)

- SP-4 materializa datos **no reconstruibles** desde el estado de Operaciones, o
- la pérdida observada de eventos afecta rankings/auditoría de forma visible, o
- se añade un consumidor con requisitos de completitud (p. ej. auditoría normativa).

## Referencias

- Spec SP-3i §7; contrato `contracts/events/operaciones-sesion-events.md` §Transport.
- Informe de auditoría 2026-07-02, D7 (save→publish).
```

- [ ] **Step 5: Traceability** — fila SP-3i en `docs/04-sdd/traceability-matrix.md`, mismo formato de columnas que las vecinas (pipes internos de celda **escapados `\|`** — lección SP-3h), columna SDD folder: `docs/superpowers/specs/2026-07-03-sp3i-backbone-rabbitmq-design.md · docs/superpowers/plans/2026-07-03-sp3i-backbone-rabbitmq.md`, commits B1..B5 con hashes reales + este de cierre, alcance "backbone RabbitMQ: publisher dual-write + ubicación + re-push convocatorias + humo Puntuaciones".

- [ ] **Step 6: Suites finales** — 3 de Operaciones (356/29/48) + Unit de Puntuaciones.

- [ ] **Step 7: Commit**

```bash
git add services/operaciones-sesion/tests/Umbral.OperacionesSesion.IntegrationTests/RabbitMqRoundTripTests.cs docs/05-decisions/ADR-0012-publicacion-eventos-best-effort.md docs/04-sdd/traceability-matrix.md
git commit -m "SP-3i B6: integration opt-in round-trip + ADR-0012 + traceability

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```
