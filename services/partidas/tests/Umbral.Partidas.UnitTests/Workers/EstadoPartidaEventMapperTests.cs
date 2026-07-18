using System.Text.Json;
using Umbral.Partidas.Api.Workers;
using Umbral.Partidas.Application.Commands;
using Umbral.Partidas.Domain.Enums;

namespace Umbral.Partidas.UnitTests.Workers;

public class EstadoPartidaEventMapperTests
{
    private static EnvelopeResumen Envelope(string eventType, string payloadJson)
    {
        using var doc = JsonDocument.Parse(payloadJson);
        return new EnvelopeResumen(Guid.NewGuid(), eventType, 1, DateTime.UtcNow, doc.RootElement.Clone());
    }

    [Theory]
    [InlineData("PartidaPublicadaEnLobby", EstadoPartida.Lobby)]
    [InlineData("PartidaIniciada", EstadoPartida.Iniciada)]
    [InlineData("PartidaCancelada", EstadoPartida.Cancelada)]
    [InlineData("PartidaFinalizada", EstadoPartida.Terminada)]
    public void Mapea_cada_evento_de_estado_a_su_comando(string eventType, EstadoPartida esperado)
    {
        var partidaId = Guid.NewGuid();
        var envelope = Envelope(eventType,
            $$"""{"partidaId":"{{partidaId}}","sesionPartidaId":"{{Guid.NewGuid()}}"}""");

        var cmd = Assert.IsType<ProyectarEstadoPartidaCommand>(EstadoPartidaEventMapper.Map(envelope));

        Assert.Equal(partidaId, cmd.PartidaId);
        Assert.Equal(esperado, cmd.Estado);
    }

    [Fact]
    public void Ignora_eventos_sin_proyeccion_de_estado()
    {
        var envelope = Envelope("PreguntaTriviaCerrada",
            $$"""{"partidaId":"{{Guid.NewGuid()}}"}""");

        Assert.Null(EstadoPartidaEventMapper.Map(envelope));
    }
}
