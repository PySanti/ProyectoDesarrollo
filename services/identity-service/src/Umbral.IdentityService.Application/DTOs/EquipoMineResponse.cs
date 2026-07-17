namespace Umbral.IdentityService.Application.DTOs;

public sealed record EquipoMineResponse(
    Guid EquipoId,
    string NombreEquipo,
    string Estado,
    IReadOnlyList<MiembroEquipoResponse> Participantes);

public sealed record MiembroEquipoResponse(Guid UsuarioId, string Nombre, bool EsLider);
