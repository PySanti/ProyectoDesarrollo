using System.Linq;
using MediatR;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Interfaces;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;
using Umbral.OperacionesSesion.Domain.Enums;

namespace Umbral.OperacionesSesion.Application.Handlers.Commands;

public sealed class PreinscribirEquipoCommandHandler
    : IRequestHandler<PreinscribirEquipoCommand, PreinscripcionEquipoResponse>
{
    private readonly ISesionPartidaRepository _sesiones;
    private readonly IEquipoDirectoryClient _directory;
    private readonly ISesionEventsPublisher _events;
    private readonly IOperacionesSesionUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public PreinscribirEquipoCommandHandler(
        ISesionPartidaRepository sesiones, IEquipoDirectoryClient directory, ISesionEventsPublisher events,
        IOperacionesSesionUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _sesiones = sesiones;
        _directory = directory;
        _events = events;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<PreinscripcionEquipoResponse> Handle(
        PreinscribirEquipoCommand request, CancellationToken cancellationToken)
    {
        var sesion = await _sesiones.GetByPartidaIdAsync(request.PartidaId, cancellationToken)
            ?? throw new SesionNoEncontradaException(request.PartidaId);

        var equipo = await _directory.ObtenerMiEquipoAsync(request.BearerToken, cancellationToken)
            ?? throw new SinEquipoActivoException(request.LiderId);

        var callerEsLider = equipo.Miembros.Any(m => m.UsuarioId == request.LiderId && m.EsLider);
        var miembros = equipo.Miembros.Select(m => m.UsuarioId).ToList();

        var equipoActivaEnOtra = await _sesiones.EquipoTieneParticipacionActivaAsync(
            equipo.EquipoId, request.PartidaId, cancellationToken);
        var equiposActivos = sesion.Inscripciones.Count(i => i.Modalidad == Modalidad.Equipo && i.EsActiva);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var inscripcion = sesion.PreinscribirEquipo(
            equipo.EquipoId, callerEsLider, miembros, equipoActivaEnOtra, equiposActivos, now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // HU-19: preinscribir ya NO convoca; las convocatorias se difieren a la aceptación del operador.
        await _events.PublicarInscripcionSolicitadaAsync(
            new InscripcionSolicitadaEvent(
                sesion.PartidaId, sesion.Id.Valor, inscripcion.Id.Valor, Modalidad.Equipo.ToString(),
                null, equipo.EquipoId, now),
            cancellationToken);

        await _events.PublicarInscripcionEquipoCreadaAsync(
            new InscripcionEquipoCreadaEvent(
                sesion.PartidaId, sesion.Id.Valor, inscripcion.Id.Valor, equipo.EquipoId, now),
            cancellationToken);

        return new PreinscripcionEquipoResponse(inscripcion.Id.Valor, equipo.EquipoId, inscripcion.Convocatorias.Count);
    }
}
