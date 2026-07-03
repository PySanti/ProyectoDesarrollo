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
    public async Task Ubicacion_publica_con_routing_key_de_ubicacion()
    {
        var canal = new CanalFake();
        var evento = new UbicacionActualizadaEvent(Guid.NewGuid(), Guid.NewGuid(), 10.5, -66.9, T0);

        await Publisher(canal).PublicarUbicacionActualizadaAsync(evento, default);

        var (key, _) = Assert.Single(canal.Publicados);
        Assert.Equal("operaciones-sesion.ubicacion-actualizada.v1", key);
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
