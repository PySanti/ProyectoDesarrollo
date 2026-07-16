using MediatR;
using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Application.DTOs;
using Umbral.IdentityService.Application.Exceptions;
using Umbral.IdentityService.Application.Interfaces;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Domain.Exceptions;

namespace Umbral.IdentityService.Application.Handlers.Commands;

public sealed class EliminarMiEquipoCommandHandler : IRequestHandler<EliminarMiEquipoCommand, EliminarEquipoResponse>
{
    private readonly IEquipoRepository _equipos;
    private readonly IInvitacionEquipoRepository _invitaciones;
    private readonly IParticipacionActivaEquipoRepository _participaciones;
    private readonly IIdentityEventsPublisher _events;

    public EliminarMiEquipoCommandHandler(
        IEquipoRepository equipos,
        IInvitacionEquipoRepository invitaciones,
        IParticipacionActivaEquipoRepository participaciones,
        IIdentityEventsPublisher events)
    {
        _equipos = equipos;
        _invitaciones = invitaciones;
        _participaciones = participaciones;
        _events = events;
    }

    public async Task<EliminarEquipoResponse> Handle(EliminarMiEquipoCommand request, CancellationToken cancellationToken)
    {
        var equipo = await _equipos.GetActiveByMemberUserIdAsync(request.ActorUserId, cancellationToken)
            ?? throw new NoActiveTeamForParticipantException(request.ActorUserId);

        if (await _participaciones.ExistsByEquipoAsync(equipo.EquipoId, cancellationToken))
        {
            throw new EquipoConParticipacionActivaException(equipo.EquipoId);
        }

        var nombre = equipo.NombreEquipo;
        IReadOnlyList<Guid> miembros;
        try
        {
            miembros = equipo.EliminarPorLider(request.ActorUserId);
        }
        catch (ActorNoEsLiderEquipoException)
        {
            throw new NoEsLiderException(request.ActorUserId);
        }

        await _equipos.UpdateAsync(equipo, cancellationToken);
        await _invitaciones.DeletePendientesByEquipoAsync(equipo.EquipoId, cancellationToken);

        // El correo a los integrantes lo dispara este evento: Identity se autoconsume
        // EquipoEliminado y notifica fuera del request (ver CredencialesTemporalesConsumer).
        await _events.PublishEquipoEliminadoAsync(
            new EquipoEliminadoIntegrationEvent(equipo.EquipoId, nombre, "Lider", miembros, DateTime.UtcNow),
            cancellationToken);

        return new EliminarEquipoResponse(equipo.EquipoId, equipo.Estado.ToString());
    }
}
