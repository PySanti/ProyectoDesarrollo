using MediatR;
using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Application.DTOs;
using Umbral.IdentityService.Application.Exceptions;
using Umbral.IdentityService.Application.Handlers.Queries;
using Umbral.IdentityService.Application.Interfaces;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Domain.Exceptions;

namespace Umbral.IdentityService.Application.Handlers.Commands;

public sealed class ReasignarLiderazgoAdminCommandHandler : IRequestHandler<ReasignarLiderazgoAdminCommand, EquipoAdminResponse>
{
    private readonly IEquipoRepository _equipos;
    private readonly IIdentityEventsPublisher _events;
    private readonly ITeamLifecycleNotifier _notifier;
    private readonly TimeProvider _timeProvider;

    public ReasignarLiderazgoAdminCommandHandler(
        IEquipoRepository equipos,
        IIdentityEventsPublisher events,
        ITeamLifecycleNotifier notifier,
        TimeProvider timeProvider)
    {
        _equipos = equipos;
        _events = events;
        _notifier = notifier;
        _timeProvider = timeProvider;
    }

    public async Task<EquipoAdminResponse> Handle(ReasignarLiderazgoAdminCommand request, CancellationToken cancellationToken)
    {
        var equipo = await _equipos.GetByIdAsync(request.EquipoId, cancellationToken)
            ?? throw new EquipoNoEncontradoException(request.EquipoId);

        Guid liderAnterior;
        Guid nuevoLider;
        try
        {
            (liderAnterior, nuevoLider) = equipo.ReasignarLiderazgoPorAdmin(request.NuevoLiderUserId);
        }
        catch (NuevoLiderNoPerteneceAlEquipoException ex)
        {
            throw new TransferirLiderazgoConflictException(ex.Message);
        }
        catch (NuevoLiderDebeSerDiferenteException ex)
        {
            throw new TransferirLiderazgoConflictException(ex.Message);
        }
        catch (EquipoEliminadoInmutableException ex)
        {
            throw new TransferirLiderazgoConflictException(ex.Message);
        }

        await _equipos.UpdateAsync(equipo, cancellationToken);

        var ahora = _timeProvider.GetUtcNow().UtcDateTime;
        await _events.PublishLiderazgoEquipoModificadoAsync(
            new LiderazgoEquipoModificadoIntegrationEvent(equipo.EquipoId, liderAnterior, nuevoLider, "Admin", ahora),
            cancellationToken);
        await _notifier.NotificarLiderazgoModificadoAsync(liderAnterior, nuevoLider, cancellationToken);

        return GetEquiposAdminQueryHandler.MapToResponse(equipo);
    }
}
