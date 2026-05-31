namespace Umbral.TriviaGame.Application.Dtos;

public sealed record TriviaInscripcionDto(
    Guid InscripcionId,
    Guid PartidaId,
    DateTimeOffset FechaInscripcion
);
