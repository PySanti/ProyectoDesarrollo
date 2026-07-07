using System.Text.Json;

namespace Umbral.Puntuaciones.Application.DTOs;

public sealed record EntradaHistorialDto(
    DateTime OccurredAt, string TipoEvento, Guid? JuegoId, Guid? ParticipanteId, Guid? EquipoId, JsonElement Detalle);

public sealed record HistorialPartidaResponse(
    Guid PartidaId, int Total, IReadOnlyList<EntradaHistorialDto> Entradas);
