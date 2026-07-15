using System.Linq;
using MediatR;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Interfaces;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;
using Umbral.OperacionesSesion.Domain.Enums;

namespace Umbral.OperacionesSesion.Application.Handlers.Commands;

public sealed class RechazarInscripcionCommandHandler : IRequestHandler<RechazarInscripcionCommand, LobbyDto>
{
    private readonly ISesionPartidaRepository _sesiones;
    private readonly ISesionEventsPublisher _events;
    private readonly IOperacionesSesionUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public RechazarInscripcionCommandHandler(
        ISesionPartidaRepository sesiones, ISesionEventsPublisher events,
        IOperacionesSesionUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _sesiones = sesiones;
        _events = events;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<LobbyDto> Handle(RechazarInscripcionCommand request, CancellationToken cancellationToken)
    {
        var sesion = await _sesiones.GetByPartidaIdAsync(request.PartidaId, cancellationToken)
            ?? throw new SesionNoEncontradaException(request.PartidaId);

        var inscripcion = sesion.Inscripciones.FirstOrDefault(i => i.Id.Valor == request.InscripcionId);
        var esEquipo = inscripcion is { Modalidad: Modalidad.Equipo };
        var participanteId = inscripcion?.ParticipanteId;

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var (inscId, equipoId) = sesion.RechazarInscripcion(request.InscripcionId, now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // `inscripcion` no es null aqui: RechazarInscripcion (arriba) ya habria lanzado si no existiera.
        var destinatarios = esEquipo
            ? inscripcion!.MiembrosSnapshot
            : (IReadOnlyList<Guid>)new[] { participanteId!.Value };

        await _events.PublicarInscripcionRechazadaAsync(
            new InscripcionRechazadaEvent(
                sesion.PartidaId, sesion.Id.Valor, inscId, esEquipo ? "Equipo" : "Individual",
                esEquipo ? null : participanteId, equipoId, now),
            destinatarios,
            cancellationToken);

        if (esEquipo && equipoId is { } eq)
        {
            await _events.PublicarInscripcionEquipoCanceladaAsync(
                new InscripcionEquipoCanceladaEvent(sesion.PartidaId, inscId, eq, now),
                cancellationToken);
        }

        return PublicarPartidaCommandHandler.MapearLobby(sesion);
    }
}
