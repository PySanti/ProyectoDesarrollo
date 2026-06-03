namespace Umbral.TeamService.Application.Teams.TransferLeadership;

public sealed record TransferirLiderazgoResponse(
    Guid EquipoId,
    Guid LiderAnteriorUserId,
    Guid NuevoLiderUserId,
    string EquipoEstado);
