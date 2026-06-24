using Umbral.IdentityService.Application.Interfaces;

namespace Umbral.IdentityService.Infrastructure.Services.Events;

public sealed class NoOpEquipoEventsPublisher : IEquipoEventsPublisher
{
    public Task PublishEquipoCreadoAsync(EquipoCreadoIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task PublishInvitacionEquipoCreadaAsync(InvitacionEquipoCreadaIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task PublishInvitacionEquipoAceptadaAsync(InvitacionEquipoAceptadaIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task PublishInvitacionEquipoRechazadaAsync(InvitacionEquipoRechazadaIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
