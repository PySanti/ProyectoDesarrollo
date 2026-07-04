using System.Text.Json;
using Umbral.Puntuaciones.Api.Workers;
using Umbral.Puntuaciones.Application.Commands;
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.UnitTests.Workers;

public class ProyeccionEventMapperTests
{
    private static EnvelopeResumen Envelope(string eventType, string payloadJson)
    {
        using var doc = JsonDocument.Parse(payloadJson);
        return new EnvelopeResumen(Guid.NewGuid(), eventType, 1, DateTime.UtcNow, doc.RootElement.Clone());
    }

    [Fact]
    public void Mapea_PartidaPublicadaEnLobby()
    {
        var partidaId = Guid.NewGuid();
        var sesionId = Guid.NewGuid();
        var envelope = Envelope("PartidaPublicadaEnLobby",
            $$"""{"partidaId":"{{partidaId}}","sesionPartidaId":"{{sesionId}}","modalidad":"Equipo","minimosParticipacion":1,"maximosParticipacion":10}""");

        var cmd = Assert.IsType<ProyectarPartidaPublicadaCommand>(ProyeccionEventMapper.Map(envelope));

        Assert.Equal(envelope.EventId, cmd.EventId);
        Assert.Equal(partidaId, cmd.PartidaId);
        Assert.Equal(Modalidad.Equipo, cmd.Modalidad);
    }

    [Fact]
    public void Mapea_PuntajeTriviaIncrementado_individual_y_equipo()
    {
        var juegoId = Guid.NewGuid();
        var equipoId = Guid.NewGuid();
        var individual = Envelope("PuntajeTriviaIncrementado",
            $$"""{"partidaId":"{{Guid.NewGuid()}}","sesionPartidaId":"{{Guid.NewGuid()}}","juegoId":"{{juegoId}}","preguntaId":"{{Guid.NewGuid()}}","participanteId":"{{Guid.NewGuid()}}","puntaje":10,"tiempoRespuestaMs":1234,"equipoId":null}""");
        var equipo = Envelope("PuntajeTriviaIncrementado",
            $$"""{"partidaId":"{{Guid.NewGuid()}}","sesionPartidaId":"{{Guid.NewGuid()}}","juegoId":"{{juegoId}}","preguntaId":"{{Guid.NewGuid()}}","participanteId":"{{Guid.NewGuid()}}","puntaje":10,"tiempoRespuestaMs":1234,"equipoId":"{{equipoId}}"}""");

        var cmdIndividual = Assert.IsType<ProyectarPuntajeTriviaCommand>(ProyeccionEventMapper.Map(individual));
        var cmdEquipo = Assert.IsType<ProyectarPuntajeTriviaCommand>(ProyeccionEventMapper.Map(equipo));

        Assert.Null(cmdIndividual.EquipoId);
        Assert.Equal(equipoId, cmdEquipo.EquipoId);
        Assert.Equal(10, cmdEquipo.Puntaje);
        Assert.Equal(1234, cmdEquipo.TiempoRespuestaMs);
    }

    [Fact]
    public void Mapea_EtapaBDTGanada()
    {
        var envelope = Envelope("EtapaBDTGanada",
            $$"""{"partidaId":"{{Guid.NewGuid()}}","sesionPartidaId":"{{Guid.NewGuid()}}","juegoId":"{{Guid.NewGuid()}}","etapaId":"{{Guid.NewGuid()}}","participanteId":"{{Guid.NewGuid()}}","puntaje":25,"tiempoResolucionMs":4000,"equipoId":null}""");

        var cmd = Assert.IsType<ProyectarEtapaBdtGanadaCommand>(ProyeccionEventMapper.Map(envelope));

        Assert.Equal(25, cmd.Puntaje);
        Assert.Equal(4000, cmd.TiempoResolucionMs);
    }

    [Fact]
    public void Mapea_JuegoActivado_con_tipo_enum_string()
    {
        var envelope = Envelope("JuegoActivado",
            $$"""{"partidaId":"{{Guid.NewGuid()}}","sesionPartidaId":"{{Guid.NewGuid()}}","juegoId":"{{Guid.NewGuid()}}","orden":2,"tipoJuego":"BusquedaDelTesoro"}""");

        var cmd = Assert.IsType<ProyectarJuegoActivadoCommand>(ProyeccionEventMapper.Map(envelope));

        Assert.Equal(2, cmd.Orden);
        Assert.Equal(TipoJuego.BusquedaDelTesoro, cmd.TipoJuego);
    }

    [Theory]
    [InlineData("PartidaIniciada", """{"partidaId":"0e6bd10a-9088-4a4e-8b1a-111111111111","sesionPartidaId":"0e6bd10a-9088-4a4e-8b1a-222222222222","fechaInicio":"2026-07-04T10:00:00Z","primerJuegoId":"0e6bd10a-9088-4a4e-8b1a-333333333333","primerJuegoOrden":1}""", typeof(ProyectarPartidaIniciadaCommand))]
    [InlineData("PartidaCancelada", """{"partidaId":"0e6bd10a-9088-4a4e-8b1a-111111111111","sesionPartidaId":"0e6bd10a-9088-4a4e-8b1a-222222222222","motivo":"MinimosNoAlcanzados","fechaCancelacion":"2026-07-04T10:00:00Z"}""", typeof(ProyectarPartidaCanceladaCommand))]
    [InlineData("PartidaFinalizada", """{"partidaId":"0e6bd10a-9088-4a4e-8b1a-111111111111","sesionPartidaId":"0e6bd10a-9088-4a4e-8b1a-222222222222","fechaFin":"2026-07-04T10:30:00Z"}""", typeof(ProyectarPartidaFinalizadaCommand))]
    public void Mapea_los_eventos_de_ciclo_de_vida(string eventType, string payload, Type esperado)
    {
        var cmd = ProyeccionEventMapper.Map(Envelope(eventType, payload));

        Assert.NotNull(cmd);
        Assert.IsType(esperado, cmd);
    }

    [Theory]
    [InlineData("RespuestaTriviaValidada")] // evento real sin proyección en 4a
    [InlineData("UbicacionActualizada")]
    [InlineData("EventoInventado")]
    public void Eventos_sin_proyeccion_devuelven_null(string eventType)
    {
        Assert.Null(ProyeccionEventMapper.Map(Envelope(eventType, """{"x":1}""")));
    }

    [Fact]
    public void Payload_no_deserializable_devuelve_null()
    {
        var envelope = Envelope("JuegoActivado", """{"tipoJuego":"NoExisteEsteTipo"}""");

        Assert.Null(ProyeccionEventMapper.Map(envelope));
    }
}
