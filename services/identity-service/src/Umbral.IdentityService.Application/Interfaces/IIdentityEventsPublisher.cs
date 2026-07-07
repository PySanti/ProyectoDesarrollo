namespace Umbral.IdentityService.Application.Interfaces;

public interface IIdentityEventsPublisher
{
    Task PublishEquipoCreadoAsync(EquipoCreadoIntegrationEvent integrationEvent, CancellationToken cancellationToken);
    Task PublishInvitacionEquipoCreadaAsync(InvitacionEquipoCreadaIntegrationEvent integrationEvent, CancellationToken cancellationToken);
    Task PublishInvitacionEquipoAceptadaAsync(InvitacionEquipoAceptadaIntegrationEvent integrationEvent, CancellationToken cancellationToken);
    Task PublishInvitacionEquipoRechazadaAsync(InvitacionEquipoRechazadaIntegrationEvent integrationEvent, CancellationToken cancellationToken);
    Task PublishRolUsuarioModificadoAsync(RolUsuarioModificadoIntegrationEvent integrationEvent, CancellationToken cancellationToken);
    Task PublishPermisosRolActualizadosAsync(PermisosRolActualizadosIntegrationEvent integrationEvent, CancellationToken cancellationToken);
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

public sealed record RolUsuarioModificadoIntegrationEvent(
    Guid UsuarioId,
    string RolAnterior,
    string RolNuevo,
    DateTime OccurredOnUtc);

public sealed record PermisosRolActualizadosIntegrationEvent(
    string Rol,
    IReadOnlyList<string> Permisos,
    DateTime OccurredOnUtc);
