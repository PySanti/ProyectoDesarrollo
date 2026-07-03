using MediatR;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Interfaces;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;
using Umbral.OperacionesSesion.Domain.Exceptions;

namespace Umbral.OperacionesSesion.Application.Handlers.Commands;

public sealed class ResponderConvocatoriaCommandHandler
    : IRequestHandler<ResponderConvocatoriaCommand, ConvocatoriaResponse>
{
    private readonly ISesionPartidaRepository _sesiones;
    private readonly ISesionEventsPublisher _events;
    private readonly IOperacionesSesionUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public ResponderConvocatoriaCommandHandler(
        ISesionPartidaRepository sesiones, ISesionEventsPublisher events,
        IOperacionesSesionUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _sesiones = sesiones;
        _events = events;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<ConvocatoriaResponse> Handle(
        ResponderConvocatoriaCommand request, CancellationToken cancellationToken)
    {
        var sesion = await _sesiones.GetByConvocatoriaIdAsync(request.ConvocatoriaId, cancellationToken)
            ?? throw new ConvocatoriaNoEncontradaException(request.ConvocatoriaId);

        var activaEnOtra = await _sesiones.ParticipanteTieneParticipacionActivaAsync(
            request.UsuarioId, sesion.PartidaId, cancellationToken);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var convocatoria = sesion.ResponderConvocatoria(
            request.ConvocatoriaId, request.UsuarioId, request.Aceptar, activaEnOtra, now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _events.PublicarConvocatoriaRespondidaAsync(
            new ConvocatoriaRespondidaEvent(
                sesion.PartidaId, sesion.Id.Valor, convocatoria.Id.Valor,
                convocatoria.UsuarioId, convocatoria.Estado.ToString()),
            cancellationToken);

        return new ConvocatoriaResponse(convocatoria.Id.Valor, convocatoria.Estado.ToString());
    }
}
