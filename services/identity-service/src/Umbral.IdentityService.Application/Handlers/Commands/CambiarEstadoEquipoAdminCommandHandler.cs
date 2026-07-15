using MediatR;
using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Application.DTOs;
using Umbral.IdentityService.Application.Exceptions;
using Umbral.IdentityService.Application.Handlers.Queries;
using Umbral.IdentityService.Application.Interfaces;
using Umbral.IdentityService.Domain.Abstractions.Persistence;

namespace Umbral.IdentityService.Application.Handlers.Commands;

public sealed class CambiarEstadoEquipoAdminCommandHandler : IRequestHandler<CambiarEstadoEquipoAdminCommand, EquipoAdminResponse>
{
    private readonly IEquipoRepository _equipos;
    private readonly IIdentityEventsPublisher _events;
    private readonly TimeProvider _timeProvider;

    public CambiarEstadoEquipoAdminCommandHandler(
        IEquipoRepository equipos,
        IIdentityEventsPublisher events,
        TimeProvider timeProvider)
    {
        _equipos = equipos;
        _events = events;
        _timeProvider = timeProvider;
    }

    public async Task<EquipoAdminResponse> Handle(CambiarEstadoEquipoAdminCommand request, CancellationToken cancellationToken)
    {
        var equipo = await _equipos.GetByIdAsync(request.EquipoId, cancellationToken)
            ?? throw new EquipoNoEncontradoException(request.EquipoId);

        var ahora = _timeProvider.GetUtcNow().UtcDateTime;

        if (request.Estado == "Desactivado")
        {
            equipo.Desactivar();
            await _equipos.UpdateAsync(equipo, cancellationToken);
            await _events.PublishEquipoDesactivadoAsync(
                new EquipoDesactivadoIntegrationEvent(equipo.EquipoId, ahora), cancellationToken);
        }
        else
        {
            equipo.Reactivar();
            await _equipos.UpdateAsync(equipo, cancellationToken);
            await _events.PublishEquipoReactivadoAsync(
                new EquipoReactivadoIntegrationEvent(equipo.EquipoId, ahora), cancellationToken);
        }

        return GetEquiposAdminQueryHandler.MapToResponse(equipo);
    }
}
