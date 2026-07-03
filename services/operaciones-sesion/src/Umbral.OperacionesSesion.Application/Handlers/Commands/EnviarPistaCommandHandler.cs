using MediatR;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Interfaces;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;

namespace Umbral.OperacionesSesion.Application.Handlers.Commands;

public sealed class EnviarPistaCommandHandler : IRequestHandler<EnviarPistaCommand, PistaEnviadaResponse>
{
    private readonly ISesionPartidaRepository _sesiones;
    private readonly ISesionEventsPublisher _events;
    private readonly TimeProvider _timeProvider;

    public EnviarPistaCommandHandler(
        ISesionPartidaRepository sesiones, ISesionEventsPublisher events, TimeProvider timeProvider)
    {
        _sesiones = sesiones;
        _events = events;
        _timeProvider = timeProvider;
    }

    public async Task<PistaEnviadaResponse> Handle(EnviarPistaCommand request, CancellationToken cancellationToken)
    {
        var sesion = await _sesiones.GetByPartidaIdAsync(request.PartidaId, cancellationToken)
            ?? throw new SesionNoEncontradaException(request.PartidaId);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var juegoId = request.EquipoDestinoId is { } equipoDestino
            ? sesion.PrepararPistaEquipo(equipoDestino)
            : sesion.PrepararPista(request.ParticipanteDestinoId!.Value); // el validator garantiza exactamente un destino

        await _events.PublicarPistaEnviadaAsync(
            new PistaEnviadaEvent(
                sesion.PartidaId, sesion.Id.Valor, juegoId, request.ParticipanteDestinoId, request.Texto, now,
                request.EquipoDestinoId),
            cancellationToken);

        return new PistaEnviadaResponse(sesion.PartidaId, juegoId, request.ParticipanteDestinoId, now, request.EquipoDestinoId);
    }
}
