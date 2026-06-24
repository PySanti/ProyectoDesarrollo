namespace Umbral.IdentityService.Application.DTOs;

public sealed record TransferirLiderazgoResponse(
    Guid EquipoId,
    Guid LiderAnteriorUserId,
    Guid NuevoLiderUserId,
    string EquipoEstado);
