namespace Umbral.TeamService.Application.Teams.JoinTeamByCode;

public sealed record UnirseAEquipoPorCodigoResponse(
    Guid EquipoId,
    string NombreEquipo,
    string CodigoAcceso,
    string Estado,
    Guid LiderUserId,
    IReadOnlyCollection<UnirseAEquipoIntegranteResponse> Integrantes);

public sealed record UnirseAEquipoIntegranteResponse(Guid UserId, bool EsLider);
