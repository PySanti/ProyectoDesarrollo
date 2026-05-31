namespace Umbral.TriviaGame.Application.Dtos;

public sealed record TriviaGameDetailDto(
    Guid Id,
    string Nombre,
    string Estado,
    string Modalidad,
    string ModoInicio,
    Guid FormularioId,
    DateTimeOffset TiempoInicio,
    int MinimoParticipantes,
    int? MaximoJugadores,
    int? MaximoEquipos,
    int? MinimoJugadoresPorEquipo,
    int? MaximoJugadoresPorEquipo,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? StartedAtUtc);
