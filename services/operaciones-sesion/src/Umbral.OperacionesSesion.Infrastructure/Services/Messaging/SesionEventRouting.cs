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
