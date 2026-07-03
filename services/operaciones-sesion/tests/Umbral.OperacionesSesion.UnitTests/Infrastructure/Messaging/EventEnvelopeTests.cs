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
