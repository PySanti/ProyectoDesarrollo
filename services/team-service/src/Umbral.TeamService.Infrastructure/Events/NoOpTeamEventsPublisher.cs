using Umbral.TeamService.Application.Abstractions.Events;

namespace Umbral.TeamService.Infrastructure.Events;

public sealed class NoOpTeamEventsPublisher : ITeamEventsPublisher
{
    public Task PublishEquipoCreadoAsync(EquipoCreadoIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
