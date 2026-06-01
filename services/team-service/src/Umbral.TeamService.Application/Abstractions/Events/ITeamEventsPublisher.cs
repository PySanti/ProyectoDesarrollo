namespace Umbral.TeamService.Application.Abstractions.Events;

public interface ITeamEventsPublisher
{
    Task PublishEquipoCreadoAsync(EquipoCreadoIntegrationEvent integrationEvent, CancellationToken cancellationToken);
}

public sealed record EquipoCreadoIntegrationEvent(
    Guid EquipoId,
    Guid LiderUserId,
    string CodigoAcceso,
    DateTime OccurredOnUtc);
