using Umbral.OperacionesSesion.Application.Interfaces;

namespace Umbral.OperacionesSesion.Infrastructure.Services;

// No-Op until the dedicated RabbitMQ backbone slice (mirrors Identity's NoOpEquipoEventsPublisher).
// The publish seam is exercised end-to-end; nothing is delivered yet.
public sealed class NoOpSesionEventsPublisher : ISesionEventsPublisher
{
    public Task PublicarPartidaPublicadaEnLobbyAsync(PartidaPublicadaEnLobbyEvent evento, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task PublicarPartidaIniciadaAsync(PartidaIniciadaEvent evento, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task PublicarJuegoActivadoAsync(JuegoActivadoEvent evento, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task PublicarPartidaCanceladaAsync(PartidaCanceladaEvent evento, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task PublicarPartidaFinalizadaAsync(PartidaFinalizadaEvent evento, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task PublicarRespuestaTriviaValidadaAsync(RespuestaTriviaValidadaEvent evento, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task PublicarPuntajeTriviaIncrementadoAsync(PuntajeTriviaIncrementadoEvent evento, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task PublicarPreguntaTriviaActivadaAsync(PreguntaTriviaActivadaEvent evento, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task PublicarPreguntaTriviaCerradaAsync(PreguntaTriviaCerradaEvent evento, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task PublicarTesoroQRValidadoAsync(TesoroQRValidadoEvent evento, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task PublicarEtapaBDTGanadaAsync(EtapaBDTGanadaEvent evento, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task PublicarEtapaBDTCerradaAsync(EtapaBDTCerradaEvent evento, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task PublicarEtapaBDTActivadaAsync(EtapaBDTActivadaEvent evento, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task PublicarPistaEnviadaAsync(PistaEnviadaEvent evento, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task PublicarConvocatoriaCreadaAsync(ConvocatoriaCreadaEvent evento, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task PublicarConvocatoriaRespondidaAsync(ConvocatoriaRespondidaEvent evento, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
