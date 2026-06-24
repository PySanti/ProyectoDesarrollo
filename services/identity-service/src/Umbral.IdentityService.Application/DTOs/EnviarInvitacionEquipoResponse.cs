namespace Umbral.IdentityService.Application.DTOs;

public sealed record EnviarInvitacionEquipoResponse(
    Guid InvitacionEquipoId,
    Guid EquipoId,
    Guid InvitadoUserId,
    Guid InvitadoPorUserId,
    string Estado,
    DateTime FechaCreacionUtc);
