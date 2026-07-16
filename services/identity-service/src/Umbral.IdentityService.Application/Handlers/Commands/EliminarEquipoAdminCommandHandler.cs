using MediatR;
using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Application.DTOs;
using Umbral.IdentityService.Application.Exceptions;
using Umbral.IdentityService.Application.Interfaces;
using Umbral.IdentityService.Domain.Abstractions.Persistence;

namespace Umbral.IdentityService.Application.Handlers.Commands;

public sealed class EliminarEquipoAdminCommandHandler : IRequestHandler<EliminarEquipoAdminCommand, EliminarEquipoAdminResponse>
{
    private readonly IEquipoRepository _equipos;
    private readonly IInvitacionEquipoRepository _invitaciones;
    private readonly IParticipacionActivaEquipoRepository _participaciones;
    private readonly IIdentityEventsPublisher _events;

    public EliminarEquipoAdminCommandHandler(
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

    public async Task<EliminarEquipoAdminResponse> Handle(EliminarEquipoAdminCommand request, CancellationToken cancellationToken)
    {
        var equipo = await _equipos.GetByIdAsync(request.EquipoId, cancellationToken)
            ?? throw new EquipoNoEncontradoException(request.EquipoId);

        if (await _participaciones.ExistsByEquipoAsync(equipo.EquipoId, cancellationToken))
        {
            throw new EquipoConParticipacionActivaException(equipo.EquipoId);
        }

        var nombre = equipo.NombreEquipo;
        var miembros = equipo.EliminarPorAdmin();

        await _equipos.UpdateAsync(equipo, cancellationToken);
        await _invitaciones.DeletePendientesByEquipoAsync(equipo.EquipoId, cancellationToken);

        // El correo a los integrantes lo dispara este evento: Identity se autoconsume
        // EquipoEliminado y notifica fuera del request (ver CredencialesTemporalesConsumer).
        await _events.PublishEquipoEliminadoAsync(
            new EquipoEliminadoIntegrationEvent(equipo.EquipoId, nombre, "Admin", miembros, DateTime.UtcNow),
            cancellationToken);

        return new EliminarEquipoAdminResponse(equipo.EquipoId, nombre);
    }
}
