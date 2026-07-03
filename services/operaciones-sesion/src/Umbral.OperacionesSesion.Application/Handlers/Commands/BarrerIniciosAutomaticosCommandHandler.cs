using MediatR;
using Microsoft.Extensions.Logging;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.Interfaces;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;
using Umbral.OperacionesSesion.Domain.Results;

namespace Umbral.OperacionesSesion.Application.Handlers.Commands;

public sealed class BarrerIniciosAutomaticosCommandHandler : IRequestHandler<BarrerIniciosAutomaticosCommand, int>
{
    private readonly ISesionPartidaRepository _sesiones;
    private readonly IOperacionesSesionUnitOfWork _unitOfWork;
    private readonly ISesionEventsPublisher _events;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<BarrerIniciosAutomaticosCommandHandler> _logger;

    public BarrerIniciosAutomaticosCommandHandler(
        ISesionPartidaRepository sesiones, IOperacionesSesionUnitOfWork unitOfWork,
        ISesionEventsPublisher events, TimeProvider timeProvider,
        ILogger<BarrerIniciosAutomaticosCommandHandler> logger)
    {
        _sesiones = sesiones;
        _unitOfWork = unitOfWork;
        _events = events;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<int> Handle(BarrerIniciosAutomaticosCommand request, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var candidatos = await _sesiones.GetSesionesAutoInicioPendienteAsync(now, cancellationToken);
        var aplicadas = 0;

        foreach (var sesion in candidatos)
        {
            try
            {
                var resultado = sesion.IntentarInicioAutomatico(now);
                if (resultado.Tipo == TipoResultadoInicio.NoCorresponde) continue;
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await IniciarPartidaCommandHandler.PublicarEventosInicioAsync(_events, sesion, resultado, now, cancellationToken);
                aplicadas++;
            }
            catch (OperationCanceledException)
            {
                throw; // shutdown del host: no tragar la cancelación, abortar el barrido
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Barrido de auto-inicio: candidato {PartidaId} saltado.", sesion.PartidaId);
            }
        }

        return aplicadas;
    }
}
