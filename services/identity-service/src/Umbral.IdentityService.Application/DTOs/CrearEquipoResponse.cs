namespace Umbral.IdentityService.Application.DTOs;

public sealed record CrearEquipoResponse(
    Guid EquipoId,
    string NombreEquipo,
    string Estado,
    Guid LiderUserId,
    IReadOnlyCollection<CrearEquipoIntegranteResponse> Integrantes);

public sealed record CrearEquipoIntegranteResponse(Guid UserId, bool EsLider);
