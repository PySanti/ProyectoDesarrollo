namespace Umbral.TeamService.Application.Teams.LeaveTeam;

public sealed record SalirDeEquipoResponse(
    Guid UserId,
    Guid EquipoId,
    string Resultado,
    string EquipoEstado);
