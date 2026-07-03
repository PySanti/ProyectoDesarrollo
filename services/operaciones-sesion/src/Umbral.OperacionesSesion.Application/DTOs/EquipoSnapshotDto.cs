namespace Umbral.OperacionesSesion.Application.DTOs;

public sealed record EquipoSnapshotDto(
    Guid EquipoId, string NombreEquipo, IReadOnlyList<MiembroEquipoDto> Miembros);

public sealed record MiembroEquipoDto(Guid UsuarioId, bool EsLider);
