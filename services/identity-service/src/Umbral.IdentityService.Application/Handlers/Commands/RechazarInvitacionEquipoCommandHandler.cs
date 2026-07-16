using MediatR;
using Umbral.IdentityService.Application.Exceptions;
using Umbral.IdentityService.Application.Interfaces;

using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Domain.Enums;

using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Application.DTOs;
namespace Umbral.IdentityService.Application.Handlers.Commands;

public sealed class RechazarInvitacionEquipoCommandHandler : IRequestHandler<RechazarInvitacionEquipoCommand, RechazarInvitacionEquipoResponse>
{
    private readonly IInvitacionEquipoRepository _invitacionRepository;
    private readonly IIdentityEventsPublisher _eventsPublisher;

    public RechazarInvitacionEquipoCommandHandler(
        IInvitacionEquipoRepository invitacionRepository,
        IIdentityEventsPublisher eventsPublisher)
    {
        _invitacionRepository = invitacionRepository;
        _eventsPublisher = eventsPublisher;
    }

    public async Task<RechazarInvitacionEquipoResponse> Handle(RechazarInvitacionEquipoCommand request, CancellationToken cancellationToken)
    {
        var invitacion = await _invitacionRepository.GetByIdAsync(request.InvitacionId, cancellationToken);
        if (invitacion is null)
            throw new InvitacionNoEncontradaException(request.InvitacionId);

        if (invitacion.InvitadoSubjectId != request.ActorUserId)
            throw new InvitacionNoEncontradaException(request.InvitacionId);

        if (invitacion.Estado != EstadoInvitacion.Pendiente)
            throw new InvitacionNoEncontradaException(request.InvitacionId);

        invitacion.Rechazar();

        await _invitacionRepository.UpdateAsync(invitacion, cancellationToken);

        await _eventsPublisher.PublishInvitacionEquipoRechazadaAsync(
            new InvitacionEquipoRechazadaIntegrationEvent(
                invitacion.InvitacionEquipoId,
                invitacion.EquipoId,
                invitacion.InvitadoSubjectId,
                DateTime.UtcNow),
            cancellationToken);

        return new RechazarInvitacionEquipoResponse(
            invitacion.InvitacionEquipoId,
            invitacion.EquipoId,
            invitacion.InvitadoSubjectId,
            invitacion.Estado.ToString());
    }
}
