namespace Umbral.IdentityService.Application.DTOs;

public sealed record ParticipanteElegibleResponse(
    Guid UserId,
    string Nombre,
    string Correo);
