using MediatR;
using Umbral.IdentityService.Application.Exceptions;
using Umbral.IdentityService.Application.Interfaces;

using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Domain.Enums;
using Umbral.IdentityService.Domain.Exceptions;

using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Application.DTOs;
namespace Umbral.IdentityService.Application.Handlers.Commands;

public sealed class AceptarInvitacionEquipoCommandHandler : IRequestHandler<AceptarInvitacionEquipoCommand, AceptarInvitacionEquipoResponse>
{
    private readonly IInvitacionEquipoRepository _invitacionRepository;
    private readonly IEquipoRepository _equipoRepository;
    private readonly IIdentityEventsPublisher _eventsPublisher;

    public AceptarInvitacionEquipoCommandHandler(
        IInvitacionEquipoRepository invitacionRepository,
        IEquipoRepository equipoRepository,
        IIdentityEventsPublisher eventsPublisher)
    {
        _invitacionRepository = invitacionRepository;
        _equipoRepository = equipoRepository;
        _eventsPublisher = eventsPublisher;
    }

    public async Task<AceptarInvitacionEquipoResponse> Handle(AceptarInvitacionEquipoCommand request, CancellationToken cancellationToken)
    {
        var invitacion = await _invitacionRepository.GetByIdAsync(request.InvitacionId, cancellationToken);
        if (invitacion is null)
            throw new InvitacionNoEncontradaException(request.InvitacionId);

        if (invitacion.InvitadoUserId != request.ActorUserId)
            throw new InvitacionNoEncontradaException(request.InvitacionId);

        if (invitacion.Estado != EstadoInvitacion.Pendiente)
            throw new InvitacionNoEncontradaException(request.InvitacionId);

        var invitadoYaEnEquipo = await _equipoRepository.ExistsActiveTeamByUserIdAsync(request.ActorUserId, cancellationToken);
        if (invitadoYaEnEquipo)
            throw new UsuarioYaEnEquipoException(request.ActorUserId);

        var equipo = await _equipoRepository.GetByIdAsync(invitacion.EquipoId, cancellationToken);
        if (equipo is null || equipo.Estado != EstadoEquipo.Activo)
            throw new InvitacionNoEncontradaException(request.InvitacionId);

        if (equipo.Participantes.Count >= 5)
            throw new EquipoLlenoException(equipo.EquipoId);

        invitacion.Aceptar();
        await _invitacionRepository.UpdateAsync(invitacion, cancellationToken);

        equipo.AgregarParticipante(invitacion.InvitadoUserId);
        await _equipoRepository.UpdateAsync(equipo, cancellationToken);

        var lider = equipo.Participantes.Single(p => p.EsLider);

        await _eventsPublisher.PublishInvitacionEquipoAceptadaAsync(
            new InvitacionEquipoAceptadaIntegrationEvent(
                invitacion.InvitacionEquipoId,
                equipo.EquipoId,
                invitacion.InvitadoUserId,
                lider.UsuarioId,
                DateTime.UtcNow),
            cancellationToken);

        return new AceptarInvitacionEquipoResponse(
            invitacion.InvitacionEquipoId,
            invitacion.EquipoId,
            invitacion.InvitadoUserId,
            invitacion.Estado.ToString());
    }
}
