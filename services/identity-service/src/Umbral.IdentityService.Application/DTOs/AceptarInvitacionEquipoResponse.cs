namespace Umbral.IdentityService.Application.DTOs;

public sealed record AceptarInvitacionEquipoResponse(
    Guid InvitacionEquipoId,
    Guid EquipoId,
    Guid InvitadoUserId,
    string EstadoInvitacion);
