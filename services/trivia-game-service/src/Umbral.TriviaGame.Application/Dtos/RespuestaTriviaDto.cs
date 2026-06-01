namespace Umbral.TriviaGame.Application.Dtos;

public sealed record RespuestaTriviaDto(
    Guid RespuestaId,
    Guid PartidaId,
    Guid PreguntaId,
    bool EsCorrecta,
    int PuntajeObtenido,
    DateTimeOffset FechaRespuesta
);
