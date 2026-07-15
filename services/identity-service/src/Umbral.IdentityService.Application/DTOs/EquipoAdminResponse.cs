namespace Umbral.IdentityService.Application.DTOs;

public sealed record EquipoAdminResponse(
    Guid EquipoId,
    string NombreEquipo,
    string Estado,
    Guid? LiderUserId,
    IReadOnlyList<EquipoAdminIntegrante> Integrantes);

public sealed record EquipoAdminIntegrante(Guid UsuarioId, bool EsLider);
