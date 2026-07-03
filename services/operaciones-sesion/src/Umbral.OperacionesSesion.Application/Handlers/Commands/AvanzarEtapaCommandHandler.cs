using MediatR;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Interfaces;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;

namespace Umbral.OperacionesSesion.Application.Handlers.Commands;

public sealed class AvanzarEtapaCommandHandler : IRequestHandler<AvanzarEtapaCommand, AvanceEtapaResponse>
{
    private readonly ISesionPartidaRepository _sesiones;
    private readonly IOperacionesSesionUnitOfWork _unitOfWork;
    private readonly ISesionEventsPublisher _events;
    private readonly TimeProvider _timeProvider;

    public AvanzarEtapaCommandHandler(
        ISesionPartidaRepository sesiones, IOperacionesSesionUnitOfWork unitOfWork,
        ISesionEventsPublisher events, TimeProvider timeProvider)
    {
        _sesiones = sesiones;
        _unitOfWork = unitOfWork;
        _events = events;
        _timeProvider = timeProvider;
    }

    public async Task<AvanceEtapaResponse> Handle(AvanzarEtapaCommand request, CancellationToken cancellationToken)
    {
        var sesion = await _sesiones.GetByPartidaIdAsync(request.PartidaId, cancellationToken)
            ?? throw new SesionNoEncontradaException(request.PartidaId);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var r = sesion.AvanzarEtapa(now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _events.PublicarEtapaBDTCerradaAsync(
            new EtapaBDTCerradaEvent(
                sesion.PartidaId, sesion.Id.Valor, r.JuegoId, r.EtapaCerradaId,
                r.MotivoCierre.ToString(), now, null),
            cancellationToken);

        if (r.EtapaActivadaId is not null)
        {
            await _events.PublicarEtapaBDTActivadaAsync(
                new EtapaBDTActivadaEvent(
                    sesion.PartidaId, sesion.Id.Valor, r.JuegoId, r.EtapaActivadaId.Value,
                    r.EtapaActivadaOrden!.Value, r.TiempoLimiteActivadaSegundos!.Value, r.FechaActivacionActivada!.Value),
                cancellationToken);
        }

        return new AvanceEtapaResponse(sesion.PartidaId, r.EtapaCerradaOrden, r.EtapaActivadaOrden, r.SinMasEtapas);
    }
}
