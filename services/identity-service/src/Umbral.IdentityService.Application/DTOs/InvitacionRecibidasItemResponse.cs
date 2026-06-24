namespace Umbral.IdentityService.Application.DTOs;

public sealed record InvitacionRecibidasItemResponse(
    Guid InvitacionId,
    Guid EquipoId,
    string NombreEquipo,
    Guid InvitadoPorUserId,
    DateTime FechaCreacionUtc);
