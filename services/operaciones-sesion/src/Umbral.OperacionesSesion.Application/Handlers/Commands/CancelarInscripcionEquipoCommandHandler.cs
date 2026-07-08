using MediatR;
using System.Linq;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Interfaces;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;

namespace Umbral.OperacionesSesion.Application.Handlers.Commands;

public sealed class CancelarInscripcionEquipoCommandHandler : IRequestHandler<CancelarInscripcionEquipoCommand>
{
    private readonly ISesionPartidaRepository _sesiones;
    private readonly IEquipoDirectoryClient _directory;
    private readonly ISesionEventsPublisher _events;
    private readonly IOperacionesSesionUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public CancelarInscripcionEquipoCommandHandler(
        ISesionPartidaRepository sesiones, IEquipoDirectoryClient directory, ISesionEventsPublisher events,
        IOperacionesSesionUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _sesiones = sesiones;
        _directory = directory;
        _events = events;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task Handle(CancelarInscripcionEquipoCommand request, CancellationToken cancellationToken)
    {
        var sesion = await _sesiones.GetByPartidaIdAsync(request.PartidaId, cancellationToken)
            ?? throw new SesionNoEncontradaException(request.PartidaId);

        var equipo = await _directory.ObtenerMiEquipoAsync(request.BearerToken, cancellationToken)
            ?? throw new SinEquipoActivoException(request.LiderId);

        var callerEsLider = equipo.Miembros.Any(m => m.UsuarioId == request.LiderId && m.EsLider);
        var inscripcionId = sesion.CancelarInscripcionEquipo(equipo.EquipoId, callerEsLider);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        await _events.PublicarInscripcionEquipoCanceladaAsync(
            new InscripcionEquipoCanceladaEvent(request.PartidaId, inscripcionId, equipo.EquipoId, now),
            cancellationToken);
    }
}
