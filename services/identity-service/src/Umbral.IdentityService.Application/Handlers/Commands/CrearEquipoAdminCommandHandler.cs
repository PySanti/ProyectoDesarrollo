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
        _ = await _usuarios.GetByIdAsync(request.LiderUserId, cancellationToken)
            ?? throw new UserNotFoundException(request.LiderUserId);

        if (await _equipos.ExistsActiveTeamByUserIdAsync(request.LiderUserId, cancellationToken))
        {
            throw new AlreadyBelongsToActiveTeamException(request.LiderUserId);
        }

        var equipo = Equipo.CrearPorAdmin(request.NombreEquipo, request.LiderUserId);
        await _equipos.AddAsync(equipo, cancellationToken);

        var ahora = _timeProvider.GetUtcNow().UtcDateTime;
        await _historial.AddRangeAsync(new[]
        {
            HistorialNombreEquipo.Registrar(request.LiderUserId, equipo.EquipoId, equipo.NombreEquipo, ahora)
        }, cancellationToken);

        await _events.PublishEquipoCreadoAsync(
            new EquipoCreadoIntegrationEvent(equipo.EquipoId, request.LiderUserId, ahora),
            cancellationToken);

        return GetEquiposAdminQueryHandler.MapToResponse(equipo);
    }
}
