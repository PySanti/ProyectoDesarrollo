namespace Umbral.IdentityService.Application.DTOs;

public sealed record RechazarInvitacionEquipoResponse(
    Guid InvitacionEquipoId,
    Guid EquipoId,
    Guid InvitadoUserId,
    string EstadoInvitacion);
