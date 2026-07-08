namespace Umbral.OperacionesSesion.Application.Interfaces;

public interface ISesionEventsPublisher
{
    Task PublicarPartidaPublicadaEnLobbyAsync(PartidaPublicadaEnLobbyEvent evento, CancellationToken cancellationToken);
    Task PublicarPartidaIniciadaAsync(PartidaIniciadaEvent evento, CancellationToken cancellationToken);
    Task PublicarJuegoActivadoAsync(JuegoActivadoEvent evento, CancellationToken cancellationToken);
    Task PublicarPartidaCanceladaAsync(PartidaCanceladaEvent evento, CancellationToken cancellationToken);
    Task PublicarPartidaFinalizadaAsync(PartidaFinalizadaEvent evento, CancellationToken cancellationToken);
    Task PublicarRespuestaTriviaValidadaAsync(RespuestaTriviaValidadaEvent evento, CancellationToken cancellationToken);
    Task PublicarPuntajeTriviaIncrementadoAsync(PuntajeTriviaIncrementadoEvent evento, CancellationToken cancellationToken);
    Task PublicarPreguntaTriviaActivadaAsync(PreguntaTriviaActivadaEvent evento, CancellationToken cancellationToken);
    Task PublicarPreguntaTriviaCerradaAsync(PreguntaTriviaCerradaEvent evento, CancellationToken cancellationToken);
    Task PublicarTesoroQRValidadoAsync(TesoroQRValidadoEvent evento, CancellationToken cancellationToken);
    Task PublicarEtapaBDTGanadaAsync(EtapaBDTGanadaEvent evento, CancellationToken cancellationToken);
    Task PublicarEtapaBDTCerradaAsync(EtapaBDTCerradaEvent evento, CancellationToken cancellationToken);
    Task PublicarEtapaBDTActivadaAsync(EtapaBDTActivadaEvent evento, CancellationToken cancellationToken);
    Task PublicarPistaEnviadaAsync(PistaEnviadaEvent evento, CancellationToken cancellationToken);
    Task PublicarConvocatoriaCreadaAsync(ConvocatoriaCreadaEvent evento, CancellationToken cancellationToken);
    Task PublicarConvocatoriaRespondidaAsync(ConvocatoriaRespondidaEvent evento, CancellationToken cancellationToken);
    Task PublicarUbicacionActualizadaAsync(UbicacionActualizadaEvent evento, CancellationToken cancellationToken);
    Task PublicarInscripcionEquipoCreadaAsync(InscripcionEquipoCreadaEvent evento, CancellationToken cancellationToken);
    Task PublicarInscripcionEquipoCanceladaAsync(InscripcionEquipoCanceladaEvent evento, CancellationToken cancellationToken);
}
