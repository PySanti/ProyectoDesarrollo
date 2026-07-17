namespace Umbral.Partidas.Application.DTOs;

// FechaCreacion va al final: el record es posicional y meterla en medio romperia toda
// construccion existente sin que el compilador senale el sitio correcto.
public sealed record PartidaSummaryDto(
    Guid PartidaId,
    string NombrePartida,
    string Modalidad,
    string ModoInicioPartida,
    DateTime? TiempoInicio,
    int MinimosParticipacion,
    int MaximosParticipacion,
    string? Estado,
    int CantidadJuegos,
    DateTime FechaCreacion);
