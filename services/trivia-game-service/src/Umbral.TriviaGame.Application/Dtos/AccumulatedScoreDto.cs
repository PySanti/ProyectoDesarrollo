namespace Umbral.TriviaGame.Application.Dtos;

public sealed record AccumulatedScoreDto(
    Guid PartidaId,
    int PuntajeAcumulado,
    double TiempoAcumuladoSegundos,
    int RespuestasCorrectas,
    int TotalRespuestas
);
