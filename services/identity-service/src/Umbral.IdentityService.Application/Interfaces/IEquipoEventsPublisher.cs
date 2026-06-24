namespace Umbral.IdentityService.Application.Interfaces;

public interface IEquipoEventsPublisher
{
    Task PublishEquipoCreadoAsync(EquipoCreadoIntegrationEvent integrationEvent, CancellationToken cancellationToken);
    Task PublishInvitacionEquipoCreadaAsync(InvitacionEquipoCreadaIntegrationEvent integrationEvent, CancellationToken cancellationToken);
    Task PublishInvitacionEquipoAceptadaAsync(InvitacionEquipoAceptadaIntegrationEvent integrationEvent, CancellationToken cancellationToken);
    Task PublishInvitacionEquipoRechazadaAsync(InvitacionEquipoRechazadaIntegrationEvent integrationEvent, CancellationToken cancellationToken);
}

public sealed record EquipoCreadoIntegrationEvent(
    Guid EquipoId,
    Guid LiderUserId,
    DateTime OccurredOnUtc);

public sealed record InvitacionEquipoCreadaIntegrationEvent(
    Guid InvitacionEquipoId,
    Guid EquipoId,
    Guid InvitadoUserId,
    Guid InvitadoPorUserId,
    DateTime OccurredOnUtc);

public sealed record InvitacionEquipoAceptadaIntegrationEvent(
    Guid InvitacionEquipoId,
    Guid EquipoId,
    Guid InvitadoUserId,
    Guid LiderUserId,
    DateTime OccurredOnUtc);

public sealed record InvitacionEquipoRechazadaIntegrationEvent(
    Guid InvitacionEquipoId,
    Guid EquipoId,
    Guid InvitadoUserId,
    DateTime OccurredOnUtc);
