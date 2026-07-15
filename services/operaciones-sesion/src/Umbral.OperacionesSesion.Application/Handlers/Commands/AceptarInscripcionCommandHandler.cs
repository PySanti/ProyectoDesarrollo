using System.Linq;
using MediatR;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Interfaces;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;
using Umbral.OperacionesSesion.Domain.Enums;

namespace Umbral.OperacionesSesion.Application.Handlers.Commands;

public sealed class AceptarInscripcionCommandHandler : IRequestHandler<AceptarInscripcionCommand, LobbyDto>
{
    private readonly ISesionPartidaRepository _sesiones;
    private readonly ISesionEventsPublisher _events;
    private readonly IOperacionesSesionUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public AceptarInscripcionCommandHandler(
        ISesionPartidaRepository sesiones, ISesionEventsPublisher events,
        IOperacionesSesionUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _sesiones = sesiones;
        _events = events;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<LobbyDto> Handle(AceptarInscripcionCommand request, CancellationToken cancellationToken)
    {
        var sesion = await _sesiones.GetByPartidaIdAsync(request.PartidaId, cancellationToken)
            ?? throw new SesionNoEncontradaException(request.PartidaId);

        var inscripcion = sesion.Inscripciones.FirstOrDefault(i => i.Id.Valor == request.InscripcionId);
        var inscritosActivos = inscripcion is { Modalidad: Modalidad.Equipo }
            ? sesion.Inscripciones.Count(i => i.Modalidad == Modalidad.Equipo && i.EsActiva)
            : sesion.Inscripciones.Count(i => i.Modalidad == Modalidad.Individual && i.EsActiva);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var convocatorias = sesion.AceptarInscripcion(request.InscripcionId, inscritosActivos, now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var aceptada = sesion.Inscripciones.First(i => i.Id.Valor == request.InscripcionId);
        var esEquipo = aceptada.Modalidad == Modalidad.Equipo;

        foreach (var c in convocatorias)
        {
            await _events.PublicarConvocatoriaCreadaAsync(
                new ConvocatoriaCreadaEvent(sesion.PartidaId, sesion.Id.Valor, c.Id.Valor, c.EquipoId, c.UsuarioId),
                cancellationToken);
        }

        await _events.PublicarInscripcionAceptadaAsync(
            new InscripcionAceptadaEvent(
                sesion.PartidaId, sesion.Id.Valor, aceptada.Id.Valor, aceptada.Modalidad.ToString(),
                esEquipo ? null : aceptada.ParticipanteId, esEquipo ? aceptada.EquipoId : null, now),
            cancellationToken);

        return PublicarPartidaCommandHandler.MapearLobby(sesion);
    }
}
