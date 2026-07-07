namespace Umbral.Puntuaciones.Application.DTOs;

public sealed record RendimientoPartidaDto(Guid PartidaId, DateTime? FechaFin, int Posicion, bool Gano);

public sealed record RendimientoEquipoResponse(Guid EquipoId, IReadOnlyList<RendimientoPartidaDto> Partidas);
