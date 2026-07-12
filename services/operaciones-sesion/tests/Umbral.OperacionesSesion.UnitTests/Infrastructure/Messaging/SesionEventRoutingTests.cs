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
    [InlineData("InscripcionEquipoCreada", "operaciones-sesion.inscripcion-equipo-creada.v1")]
    [InlineData("InscripcionEquipoCancelada", "operaciones-sesion.inscripcion-equipo-cancelada.v1")]
    public void RoutingKeyFor_mapea_los_19_eventos(string eventType, string esperado)
        => Assert.Equal(esperado, SesionEventRouting.RoutingKeyFor(eventType));

    [Fact]
    public void RoutingKeyFor_evento_desconocido_lanza()
        => Assert.Throws<System.Collections.Generic.KeyNotFoundException>(
            () => SesionEventRouting.RoutingKeyFor("EventoInventado"));
}

public class SesionEventRoutingInscripcionTests
{
    [Theory]
    [InlineData("InscripcionSolicitada", "operaciones-sesion.inscripcion-solicitada.v1")]
    [InlineData("InscripcionAceptada", "operaciones-sesion.inscripcion-aceptada.v1")]
    [InlineData("InscripcionRechazada", "operaciones-sesion.inscripcion-rechazada.v1")]
    public void RoutingKeyFor_mapea_los_eventos_de_aprobacion(string eventType, string expected)
        => Assert.Equal(expected, SesionEventRouting.RoutingKeyFor(eventType));
}
