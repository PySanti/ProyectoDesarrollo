namespace Umbral.Partidas.Application.DTOs;

public sealed record PartidaSummaryDto(
    Guid PartidaId,
    string NombrePartida,
    string Modalidad,
    string ModoInicioPartida,
    DateTime? TiempoInicio,
    int MinimosParticipacion,
    int MaximosParticipacion,
    string? Estado,
    int CantidadJuegos);
