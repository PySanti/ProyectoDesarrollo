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
    [InlineData("{\"eventId\":123,\"eventType\":\"X\",\"version\":1,\"occurredAt\":\"2026-07-03T10:00:00Z\"}")]
    [InlineData("{\"eventId\":\"3f2504e0-4f89-11d3-9a0c-0305e82c3301\",\"eventType\":\"X\",\"version\":\"uno\",\"occurredAt\":\"2026-07-03T10:00:00Z\"}")]
    public void TryRead_malformado_devuelve_false(string body)
    {
        var ok = EnvelopeReader.TryRead(System.Text.Encoding.UTF8.GetBytes(body), out var envelope);
        Assert.False(ok);
        Assert.Null(envelope);
    }
}
