using MediatR;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Interfaces;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;

namespace Umbral.OperacionesSesion.Application.Handlers.Commands;

public sealed class AvanzarPreguntaCommandHandler : IRequestHandler<AvanzarPreguntaCommand, AvancePreguntaResponse>
{
    private readonly ISesionPartidaRepository _sesiones;
    private readonly IOperacionesSesionUnitOfWork _unitOfWork;
    private readonly ISesionEventsPublisher _events;
    private readonly TimeProvider _timeProvider;

    public AvanzarPreguntaCommandHandler(
        ISesionPartidaRepository sesiones, IOperacionesSesionUnitOfWork unitOfWork,
        ISesionEventsPublisher events, TimeProvider timeProvider)
    {
        _sesiones = sesiones;
        _unitOfWork = unitOfWork;
        _events = events;
        _timeProvider = timeProvider;
    }

    public async Task<AvancePreguntaResponse> Handle(AvanzarPreguntaCommand request, CancellationToken cancellationToken)
    {
        var sesion = await _sesiones.GetByPartidaIdAsync(request.PartidaId, cancellationToken)
            ?? throw new SesionNoEncontradaException(request.PartidaId);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var r = sesion.AvanzarPregunta(now);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _events.PublicarPreguntaTriviaCerradaAsync(
            new PreguntaTriviaCerradaEvent(sesion.PartidaId, sesion.Id.Valor, r.JuegoId, r.PreguntaCerradaId,
                r.MotivoCierre.ToString(), now, null), cancellationToken);

        if (r.PreguntaActivadaId is not null)
        {
            await _events.PublicarPreguntaTriviaActivadaAsync(
                new PreguntaTriviaActivadaEvent(sesion.PartidaId, sesion.Id.Valor, r.JuegoId, r.PreguntaActivadaId.Value,
                    r.PreguntaActivadaOrden!.Value, r.TiempoLimiteActivadaSegundos!.Value, r.FechaActivacionActivada!.Value),
                cancellationToken);
        }

        return new AvancePreguntaResponse(sesion.PartidaId, r.PreguntaCerradaOrden, r.PreguntaActivadaOrden, r.SinMasPreguntas);
    }
}
