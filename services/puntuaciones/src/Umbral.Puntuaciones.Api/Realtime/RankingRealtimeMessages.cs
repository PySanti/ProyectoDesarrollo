namespace Umbral.Puntuaciones.Api.Realtime;

public static class RankingRealtimeMessages
{
    public const string RankingTriviaActualizado = nameof(RankingTriviaActualizado);
    public const string RankingBDTActualizado = nameof(RankingBDTActualizado);
    public const string RankingConsolidadoCalculado = nameof(RankingConsolidadoCalculado);

    public static string GrupoPartida(Guid partidaId) => $"puntuaciones-partida-{partidaId}";
}
