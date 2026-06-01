namespace Umbral.TeamService.Application.Teams.CreateTeam;

public sealed record CrearEquipoResponse(
    Guid EquipoId,
    string NombreEquipo,
    string CodigoAcceso,
    string Estado,
    Guid LiderUserId,
    IReadOnlyCollection<CrearEquipoIntegranteResponse> Integrantes);

public sealed record CrearEquipoIntegranteResponse(Guid UserId, bool EsLider);
