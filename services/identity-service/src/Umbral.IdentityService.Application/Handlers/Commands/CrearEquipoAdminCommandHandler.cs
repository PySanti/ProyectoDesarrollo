using Umbral.IdentityService.Domain.ValueObjects;
using MediatR;
using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Application.DTOs;
using Umbral.IdentityService.Application.Exceptions;
using Umbral.IdentityService.Application.Handlers.Queries;
using Umbral.IdentityService.Application.Interfaces;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Domain.Entities;

namespace Umbral.IdentityService.Application.Handlers.Commands;

public sealed class CrearEquipoAdminCommandHandler : IRequestHandler<CrearEquipoAdminCommand, EquipoAdminResponse>
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IEquipoRepository _equipos;
    private readonly IHistorialNombreEquipoRepository _historial;
    private readonly IIdentityEventsPublisher _events;
    private readonly TimeProvider _timeProvider;

    public CrearEquipoAdminCommandHandler(
        IUsuarioRepository usuarios,
        IEquipoRepository equipos,
        IHistorialNombreEquipoRepository historial,
        IIdentityEventsPublisher events,
        TimeProvider timeProvider)
    {
        _usuarios = usuarios;
        _equipos = equipos;
        _historial = historial;
        _events = events;
        _timeProvider = timeProvider;
    }

    public async Task<EquipoAdminResponse> Handle(CrearEquipoAdminCommand request, CancellationToken cancellationToken)
    {
        var usuario = await _usuarios.GetByIdAsync(UsuarioLocalId.From(request.LiderUserId), cancellationToken)
            ?? throw new UserNotFoundException(request.LiderUserId);

        // La membresía de equipo (y toda referencia que deba coincidir con el `sub` del JWT del
        // futuro líder) se indexa por KeycloakId, no por el UsuarioId local recibido del admin.
        var liderMembershipKey = Guid.Parse(usuario.KeycloakId);

        if (await _equipos.ExistsActiveTeamByUserIdAsync(liderMembershipKey, cancellationToken))
        {
            throw new AlreadyBelongsToActiveTeamException(liderMembershipKey);
        }

        var equipo = Equipo.CrearPorAdmin(request.NombreEquipo, liderMembershipKey);
        await _equipos.AddAsync(equipo, cancellationToken);

        var ahora = _timeProvider.GetUtcNow().UtcDateTime;
        await _historial.AddRangeAsync(new[]
        {
            HistorialNombreEquipo.Registrar(liderMembershipKey, equipo.EquipoId, equipo.NombreEquipo, ahora)
        }, cancellationToken);

        await _events.PublishEquipoCreadoAsync(
            new EquipoCreadoIntegrationEvent(equipo.EquipoId, liderMembershipKey, ahora),
            cancellationToken);

        return GetEquiposAdminQueryHandler.MapToResponse(equipo);
    }
}
