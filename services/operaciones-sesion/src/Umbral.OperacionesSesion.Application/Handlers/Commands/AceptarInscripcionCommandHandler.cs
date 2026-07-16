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

        // La convocatoria del lider nace Aceptada (preinscribir ya era su declaracion de
        // intencion), pero no puede saltarse BR-G09: si ya participa en otra partida, su
        // convocatoria se queda Pendiente igual que si intentara aceptarla a mano.
        var liderPuedeAutoAceptar = inscripcion is { Modalidad: Modalidad.Equipo }
            && !await _sesiones.ParticipanteTieneParticipacionActivaAsync(
                inscripcion.LiderId, request.PartidaId, cancellationToken);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var convocatorias = sesion.AceptarInscripcion(
            request.InscripcionId, inscritosActivos, now, liderPuedeAutoAceptar);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var aceptada = sesion.Inscripciones.First(i => i.Id.Valor == request.InscripcionId);
        var esEquipo = aceptada.Modalidad == Modalidad.Equipo;

        foreach (var c in convocatorias)
        {
            await _events.PublicarConvocatoriaCreadaAsync(
                new ConvocatoriaCreadaEvent(sesion.PartidaId, sesion.Id.Valor, c.Id.Valor, c.EquipoId, c.UsuarioId),
                cancellationToken);

            // ConvocatoriaCreadaEvent no lleva Estado, asi que un consumidor asumiria Pendiente y
            // proyectaria un estado falso para la del lider, que nace Aceptada. Se anuncia igual
            // que el camino manual.
            if (c.EstaAceptada)
            {
                await _events.PublicarConvocatoriaRespondidaAsync(
                    new ConvocatoriaRespondidaEvent(
                        sesion.PartidaId, sesion.Id.Valor, c.Id.Valor, c.UsuarioId, c.Estado.ToString()),
                    cancellationToken);
            }
        }

        // Individual: el solicitante. Equipo: el snapshot de miembros — el lider no se guarda
        // (InscripcionPartida.ParticipanteId = Guid.Empty en Equipo), asi que se notifica al conjunto.
        var destinatarios = esEquipo
            ? aceptada.MiembrosSnapshot
            : (IReadOnlyList<Guid>)new[] { aceptada.ParticipanteId };

        await _events.PublicarInscripcionAceptadaAsync(
            new InscripcionAceptadaEvent(
                sesion.PartidaId, sesion.Id.Valor, aceptada.Id.Valor, aceptada.Modalidad.ToString(),
                esEquipo ? null : aceptada.ParticipanteId, esEquipo ? aceptada.EquipoId : null, now),
            destinatarios,
            cancellationToken);

        return PublicarPartidaCommandHandler.MapearLobby(sesion);
    }
}
