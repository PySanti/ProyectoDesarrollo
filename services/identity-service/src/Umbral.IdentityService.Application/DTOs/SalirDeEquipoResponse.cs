namespace Umbral.IdentityService.Application.DTOs;

public sealed record SalirDeEquipoResponse(
    Guid UserId,
    Guid EquipoId,
    string Resultado,
    string EquipoEstado);
