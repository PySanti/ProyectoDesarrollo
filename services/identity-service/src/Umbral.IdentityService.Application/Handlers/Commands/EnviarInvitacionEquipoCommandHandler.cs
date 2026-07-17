using MediatR;
using Umbral.IdentityService.Application.Exceptions;
using Umbral.IdentityService.Application.Interfaces;

using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Domain.Entities;
using Umbral.IdentityService.Domain.Exceptions;

using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Application.DTOs;
namespace Umbral.IdentityService.Application.Handlers.Commands;

public sealed class EnviarInvitacionEquipoCommandHandler : IRequestHandler<EnviarInvitacionEquipoCommand, EnviarInvitacionEquipoResponse>
{
    private readonly IEquipoRepository _equipoRepository;
    private readonly IInvitacionEquipoRepository _invitacionRepository;
    private readonly IIdentityEventsPublisher _eventsPublisher;

    public EnviarInvitacionEquipoCommandHandler(
        IEquipoRepository equipoRepository,
        IInvitacionEquipoRepository invitacionRepository,
        IIdentityEventsPublisher eventsPublisher)
    {
        _equipoRepository = equipoRepository;
        _invitacionRepository = invitacionRepository;
        _eventsPublisher = eventsPublisher;
    }

    public async Task<EnviarInvitacionEquipoResponse> Handle(EnviarInvitacionEquipoCommand request, CancellationToken cancellationToken)
    {
        var equipo = await _equipoRepository.GetActiveByMemberUserIdAsync(request.ActorUserId, cancellationToken);

        if (equipo is null)
            throw new NoEsLiderException(request.ActorUserId);

        var lider = equipo.Participantes.SingleOrDefault(p => p.EsLider);
        if (lider is null || lider.SubjectId != request.ActorUserId)
            throw new NoEsLiderException(request.ActorUserId);

        if (equipo.Participantes.Count >= 5)
            throw new EquipoLlenoException(equipo.EquipoId);

        var invitadoYaEnEquipo = await _equipoRepository.ExistsActiveTeamByUserIdAsync(request.InvitadoUserId, cancellationToken);
        if (invitadoYaEnEquipo)
            throw new UsuarioYaEnEquipoException(request.InvitadoUserId);

        var yaExistePendiente = await _invitacionRepository.ExistsPendienteAsync(equipo.EquipoId, request.InvitadoUserId, cancellationToken);
        if (yaExistePendiente)
            throw new InvitacionPendienteYaExisteException(equipo.EquipoId, request.InvitadoUserId);

        var invitacion = InvitacionEquipo.Crear(equipo.EquipoId, request.InvitadoUserId, request.ActorUserId);
        await _invitacionRepository.AddAsync(invitacion, cancellationToken);

        await _eventsPublisher.PublishInvitacionEquipoCreadaAsync(
            new InvitacionEquipoCreadaIntegrationEvent(
                invitacion.InvitacionEquipoId,
                equipo.EquipoId,
                request.InvitadoUserId,
                request.ActorUserId,
                DateTime.UtcNow),
            cancellationToken);

        return new EnviarInvitacionEquipoResponse(
            invitacion.InvitacionEquipoId,
            invitacion.EquipoId,
            invitacion.InvitadoSubjectId,
            invitacion.InvitadoPorSubjectId,
            invitacion.Estado.ToString(),
            invitacion.FechaCreacionUtc);
    }
}
