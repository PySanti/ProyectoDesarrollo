using MediatR;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Interfaces;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;

namespace Umbral.OperacionesSesion.Application.Handlers.Commands;

// HU-40: cancelación manual de la partida por el operador.
public sealed class CancelarPartidaCommandHandler : IRequestHandler<CancelarPartidaCommand, CancelacionPartidaResponse>
{
    private readonly ISesionPartidaRepository _sesiones;
    private readonly IOperacionesSesionUnitOfWork _unitOfWork;
    private readonly ISesionEventsPublisher _events;
    private readonly TimeProvider _timeProvider;

    public CancelarPartidaCommandHandler(
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

    public async Task<CancelacionPartidaResponse> Handle(CancelarPartidaCommand request, CancellationToken cancellationToken)
    {
        var sesion = await _sesiones.GetByPartidaIdAsync(request.PartidaId, cancellationToken)
            ?? throw new SesionNoEncontradaException(request.PartidaId);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        sesion.Cancelar(now);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _events.PublicarPartidaCanceladaAsync(
            new PartidaCanceladaEvent(sesion.PartidaId, sesion.Id.Valor, "CanceladaPorOperador", now),
            cancellationToken);

        return new CancelacionPartidaResponse(sesion.PartidaId, sesion.Estado.ToString());
    }
}
