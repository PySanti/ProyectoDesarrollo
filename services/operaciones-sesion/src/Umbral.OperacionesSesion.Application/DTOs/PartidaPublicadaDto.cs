namespace Umbral.OperacionesSesion.Application.DTOs;

public sealed record PartidaPublicadaDto(
    Guid PartidaId,
    string Nombre,
    string Modalidad,
    string ModoInicioPartida,
    DateTime? TiempoInicio,
    int MinimosParticipacion,
    int MaximosParticipacion,
    int InscritosActivos);
