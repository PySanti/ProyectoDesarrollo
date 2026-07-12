namespace Umbral.IdentityService.Application.DTOs;

public sealed record EquipoAdminItemResponse(
    Guid EquipoId,
    string NombreEquipo,
    string Estado,
    IReadOnlyList<MiembroEquipoAdminResponse> Participantes);

public sealed record MiembroEquipoAdminResponse(Guid UsuarioId, string Nombre, bool EsLider);
