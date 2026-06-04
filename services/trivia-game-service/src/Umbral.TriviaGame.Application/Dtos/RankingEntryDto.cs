namespace Umbral.TriviaGame.Application.Dtos;

public sealed record RankingEntryDto(
    string UsuarioId,
    int PuntajeAcumulado,
    int TiempoAcumuladoSegundos,
    int RespuestasCorrectas,
    int TotalRespuestas,
    int Posicion
);
