namespace Umbral.TriviaGame.Application.Dtos;

public sealed record TriviaGameLobbyDto(
    Guid PartidaId,
    string Nombre,
    string Estado,
    string Modalidad,
    DateTimeOffset TiempoInicio,
    int MinimoParticipantes,
    int MaximoJugadores,
    int ParticipantesActual,
    IReadOnlyList<TriviaInscripcionLobbyDto> Participantes);

public sealed record TriviaInscripcionLobbyDto(
    Guid InscripcionId,
    string UsuarioId,
    DateTimeOffset FechaInscripcion);
