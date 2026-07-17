namespace Umbral.IdentityService.Application.Interfaces;

public interface IIdentityEventsPublisher
{
    Task PublishEquipoCreadoAsync(EquipoCreadoIntegrationEvent integrationEvent, CancellationToken cancellationToken);
    Task PublishInvitacionEquipoCreadaAsync(InvitacionEquipoCreadaIntegrationEvent integrationEvent, CancellationToken cancellationToken);
    Task PublishInvitacionEquipoAceptadaAsync(InvitacionEquipoAceptadaIntegrationEvent integrationEvent, CancellationToken cancellationToken);
    Task PublishInvitacionEquipoRechazadaAsync(InvitacionEquipoRechazadaIntegrationEvent integrationEvent, CancellationToken cancellationToken);
    Task PublishRolUsuarioModificadoAsync(RolUsuarioModificadoIntegrationEvent integrationEvent, CancellationToken cancellationToken);
    Task PublishPermisosRolActualizadosAsync(PermisosRolActualizadosIntegrationEvent integrationEvent, CancellationToken cancellationToken);
    Task PublishEquipoEliminadoAsync(EquipoEliminadoIntegrationEvent integrationEvent, CancellationToken cancellationToken);
    Task PublishLiderazgoEquipoModificadoAsync(LiderazgoEquipoModificadoIntegrationEvent integrationEvent, CancellationToken cancellationToken);
    Task PublishEquipoDesactivadoAsync(EquipoDesactivadoIntegrationEvent integrationEvent, CancellationToken cancellationToken);
    Task PublishEquipoReactivadoAsync(EquipoReactivadoIntegrationEvent integrationEvent, CancellationToken cancellationToken);
    Task PublishCredencialTemporalEmitidaAsync(CredencialTemporalEmitidaIntegrationEvent integrationEvent, CancellationToken cancellationToken);
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

public sealed record EquipoEliminadoIntegrationEvent(
    Guid EquipoId,
    string NombreEquipo,
    string Origen,
    IReadOnlyList<Guid> Miembros,
    DateTime OccurredOnUtc);

public sealed record LiderazgoEquipoModificadoIntegrationEvent(
    Guid EquipoId,
    Guid LiderAnteriorUserId,
    Guid NuevoLiderUserId,
    string Origen,
    DateTime OccurredOnUtc);

public sealed record EquipoDesactivadoIntegrationEvent(
    Guid EquipoId,
    DateTime OccurredOnUtc);

public sealed record EquipoReactivadoIntegrationEvent(
    Guid EquipoId,
    DateTime OccurredOnUtc);

// La contraseña temporal viaja en el payload: exchange interno (umbral.identity), no expuesto a
// clientes; decisión de seguridad documentada en el plan del slice (7f, RNF-23).
public sealed record CredencialTemporalEmitidaIntegrationEvent(
    string Nombre,
    string Correo,
    string Rol,
    string PasswordTemporal,
    DateTime OccurredOnUtc);
