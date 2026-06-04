namespace Umbral.TriviaGame.Application.Dtos;

public sealed record TriviaGameListItemDto(
    Guid Id,
    string Nombre,
    string Modalidad,
    string Estado,
    DateTimeOffset TiempoInicio,
    int MinimoParticipantes,
    int? MaximoJugadores,
    int? MaximoEquipos);
