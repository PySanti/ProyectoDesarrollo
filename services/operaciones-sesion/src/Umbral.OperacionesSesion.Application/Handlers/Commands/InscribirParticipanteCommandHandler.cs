using System.Linq;
using MediatR;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Interfaces;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;
using Umbral.OperacionesSesion.Domain.Enums;

namespace Umbral.OperacionesSesion.Application.Handlers.Commands;

public sealed class InscribirParticipanteCommandHandler : IRequestHandler<InscribirParticipanteCommand, InscripcionResponse>
{
    private readonly ISesionPartidaRepository _sesiones;
    private readonly ISesionEventsPublisher _events;
    private readonly IOperacionesSesionUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public InscribirParticipanteCommandHandler(
        ISesionPartidaRepository sesiones, ISesionEventsPublisher events,
        IOperacionesSesionUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _sesiones = sesiones;
        _events = events;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<InscripcionResponse> Handle(InscribirParticipanteCommand request, CancellationToken cancellationToken)
    {
        var sesion = await _sesiones.GetByPartidaIdAsync(request.PartidaId, cancellationToken)
            ?? throw new SesionNoEncontradaException(request.PartidaId);

        var activaEnOtra = await _sesiones.ParticipanteTieneParticipacionActivaAsync(
            request.ParticipanteId, request.PartidaId, cancellationToken);
        var inscritosActivos = sesion.Inscripciones.Count(i => i.EsActiva);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var inscripcion = sesion.Inscribir(request.ParticipanteId, activaEnOtra, inscritosActivos, now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _events.PublicarInscripcionSolicitadaAsync(
            new InscripcionSolicitadaEvent(
                sesion.PartidaId, sesion.Id.Valor, inscripcion.Id.Valor, Modalidad.Individual.ToString(),
                request.ParticipanteId, null, now),
            cancellationToken);

        return new InscripcionResponse(inscripcion.Id.Valor, request.PartidaId, request.ParticipanteId);
    }
}
