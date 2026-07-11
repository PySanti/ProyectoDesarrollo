using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Umbral.OperacionesSesion.Application.Interfaces;

namespace Umbral.OperacionesSesion.Infrastructure.Services;

public sealed class CompositeSesionEventsPublisher : ISesionEventsPublisher
{
    private readonly IReadOnlyList<ISesionEventsPublisher> _publishers;
    private readonly ILogger<CompositeSesionEventsPublisher> _logger;

    public CompositeSesionEventsPublisher(
        IEnumerable<ISesionEventsPublisher> publishers,
        ILogger<CompositeSesionEventsPublisher> logger)
    {
        _publishers = publishers.ToList();
        _logger = logger;
    }

    private async Task FanOut(Func<ISesionEventsPublisher, Task> call)
    {
        foreach (var p in _publishers)
        {
            try
            {
                await call(p);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Publicador {Publicador} falló al emitir evento de sesión", p.GetType().Name);
            }
        }
    }

    public Task PublicarPartidaPublicadaEnLobbyAsync(PartidaPublicadaEnLobbyEvent evento, CancellationToken cancellationToken) => FanOut(p => p.PublicarPartidaPublicadaEnLobbyAsync(evento, cancellationToken));
    public Task PublicarPartidaIniciadaAsync(PartidaIniciadaEvent evento, CancellationToken cancellationToken) => FanOut(p => p.PublicarPartidaIniciadaAsync(evento, cancellationToken));
    public Task PublicarJuegoActivadoAsync(JuegoActivadoEvent evento, CancellationToken cancellationToken) => FanOut(p => p.PublicarJuegoActivadoAsync(evento, cancellationToken));
    public Task PublicarPartidaCanceladaAsync(PartidaCanceladaEvent evento, CancellationToken cancellationToken) => FanOut(p => p.PublicarPartidaCanceladaAsync(evento, cancellationToken));
    public Task PublicarPartidaFinalizadaAsync(PartidaFinalizadaEvent evento, CancellationToken cancellationToken) => FanOut(p => p.PublicarPartidaFinalizadaAsync(evento, cancellationToken));
    public Task PublicarRespuestaTriviaValidadaAsync(RespuestaTriviaValidadaEvent evento, CancellationToken cancellationToken) => FanOut(p => p.PublicarRespuestaTriviaValidadaAsync(evento, cancellationToken));
    public Task PublicarPuntajeTriviaIncrementadoAsync(PuntajeTriviaIncrementadoEvent evento, CancellationToken cancellationToken) => FanOut(p => p.PublicarPuntajeTriviaIncrementadoAsync(evento, cancellationToken));
    public Task PublicarPreguntaTriviaActivadaAsync(PreguntaTriviaActivadaEvent evento, CancellationToken cancellationToken) => FanOut(p => p.PublicarPreguntaTriviaActivadaAsync(evento, cancellationToken));
    public Task PublicarPreguntaTriviaCerradaAsync(PreguntaTriviaCerradaEvent evento, CancellationToken cancellationToken) => FanOut(p => p.PublicarPreguntaTriviaCerradaAsync(evento, cancellationToken));
    public Task PublicarTesoroQRValidadoAsync(TesoroQRValidadoEvent evento, CancellationToken cancellationToken) => FanOut(p => p.PublicarTesoroQRValidadoAsync(evento, cancellationToken));
    public Task PublicarEtapaBDTGanadaAsync(EtapaBDTGanadaEvent evento, CancellationToken cancellationToken) => FanOut(p => p.PublicarEtapaBDTGanadaAsync(evento, cancellationToken));
    public Task PublicarEtapaBDTCerradaAsync(EtapaBDTCerradaEvent evento, CancellationToken cancellationToken) => FanOut(p => p.PublicarEtapaBDTCerradaAsync(evento, cancellationToken));
    public Task PublicarEtapaBDTActivadaAsync(EtapaBDTActivadaEvent evento, CancellationToken cancellationToken) => FanOut(p => p.PublicarEtapaBDTActivadaAsync(evento, cancellationToken));
    public Task PublicarPistaEnviadaAsync(PistaEnviadaEvent evento, CancellationToken cancellationToken) => FanOut(p => p.PublicarPistaEnviadaAsync(evento, cancellationToken));
    public Task PublicarConvocatoriaCreadaAsync(ConvocatoriaCreadaEvent evento, CancellationToken cancellationToken) => FanOut(p => p.PublicarConvocatoriaCreadaAsync(evento, cancellationToken));
    public Task PublicarConvocatoriaRespondidaAsync(ConvocatoriaRespondidaEvent evento, CancellationToken cancellationToken) => FanOut(p => p.PublicarConvocatoriaRespondidaAsync(evento, cancellationToken));
    public Task PublicarUbicacionActualizadaAsync(UbicacionActualizadaEvent evento, CancellationToken cancellationToken) => FanOut(p => p.PublicarUbicacionActualizadaAsync(evento, cancellationToken));
    public Task PublicarInscripcionEquipoCreadaAsync(InscripcionEquipoCreadaEvent evento, CancellationToken cancellationToken) => FanOut(p => p.PublicarInscripcionEquipoCreadaAsync(evento, cancellationToken));
    public Task PublicarInscripcionEquipoCanceladaAsync(InscripcionEquipoCanceladaEvent evento, CancellationToken cancellationToken) => FanOut(p => p.PublicarInscripcionEquipoCanceladaAsync(evento, cancellationToken));
    public Task PublicarInscripcionSolicitadaAsync(InscripcionSolicitadaEvent evento, CancellationToken cancellationToken) => FanOut(p => p.PublicarInscripcionSolicitadaAsync(evento, cancellationToken));
    public Task PublicarInscripcionAceptadaAsync(InscripcionAceptadaEvent evento, CancellationToken cancellationToken) => FanOut(p => p.PublicarInscripcionAceptadaAsync(evento, cancellationToken));
    public Task PublicarInscripcionRechazadaAsync(InscripcionRechazadaEvent evento, CancellationToken cancellationToken) => FanOut(p => p.PublicarInscripcionRechazadaAsync(evento, cancellationToken));
}
