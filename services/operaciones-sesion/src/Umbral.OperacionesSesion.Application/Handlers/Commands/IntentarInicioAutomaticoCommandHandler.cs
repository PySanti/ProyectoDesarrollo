using MediatR;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Interfaces;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;
using Umbral.OperacionesSesion.Domain.Results;

namespace Umbral.OperacionesSesion.Application.Handlers.Commands;

public sealed class IntentarInicioAutomaticoCommandHandler : IRequestHandler<IntentarInicioAutomaticoCommand, InicioPartidaResponse>
{
    private readonly ISesionPartidaRepository _sesiones;
    private readonly IOperacionesSesionUnitOfWork _unitOfWork;
    private readonly ISesionEventsPublisher _events;
    private readonly TimeProvider _timeProvider;

    public IntentarInicioAutomaticoCommandHandler(
        ISesionPartidaRepository sesiones,
        IOperacionesSesionUnitOfWork unitOfWork,
        ISesionEventsPublisher events,
        TimeProvider timeProvider)
    {
        _sesiones = sesiones;
        _unitOfWork = unitOfWork;
        _events = events;
        _timeProvider = timeProvider;
    }

    public async Task<InicioPartidaResponse> Handle(IntentarInicioAutomaticoCommand request, CancellationToken cancellationToken)
    {
        var sesion = await _sesiones.GetByPartidaIdAsync(request.PartidaId, cancellationToken)
            ?? throw new SesionNoEncontradaException(request.PartidaId);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var resultado = sesion.IntentarInicioAutomatico(now);

        if (resultado.Tipo != TipoResultadoInicio.NoCorresponde)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await IniciarPartidaCommandHandler.PublicarEventosInicioAsync(_events, sesion, resultado, now, cancellationToken);
        }

        return IniciarPartidaCommandHandler.MapearInicio(sesion, resultado);
    }
}
