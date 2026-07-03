using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;

namespace Umbral.OperacionesSesion.Domain.ValueObjects;

public sealed record ConfiguracionSnapshot(
    string Nombre,
    Modalidad Modalidad,
    ModoInicioPartida ModoInicioPartida,
    DateTime? TiempoInicio,
    int MinimosParticipacion,
    int MaximosParticipacion,
    IReadOnlyList<JuegoResumen> Juegos);
