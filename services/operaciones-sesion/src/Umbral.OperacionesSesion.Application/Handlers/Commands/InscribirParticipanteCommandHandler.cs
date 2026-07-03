using System.Linq;
using MediatR;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;
using Umbral.OperacionesSesion.Domain.Enums;

namespace Umbral.OperacionesSesion.Application.Handlers.Commands;

public sealed class InscribirParticipanteCommandHandler : IRequestHandler<InscribirParticipanteCommand, InscripcionResponse>
{
    private readonly ISesionPartidaRepository _sesiones;
    private readonly IOperacionesSesionUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public InscribirParticipanteCommandHandler(
        ISesionPartidaRepository sesiones, IOperacionesSesionUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _sesiones = sesiones;
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

        var inscripcion = sesion.Inscribir(
            request.ParticipanteId, activaEnOtra, inscritosActivos, _timeProvider.GetUtcNow().UtcDateTime);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new InscripcionResponse(inscripcion.Id.Valor, request.PartidaId, request.ParticipanteId);
    }
}
