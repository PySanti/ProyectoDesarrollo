using Umbral.Puntuaciones.Application.DTOs;

namespace Umbral.Puntuaciones.Application.Interfaces;

// Port de difusión de rankings en vivo (SP-4c). La implementación vive en Api (SignalR).
public interface IRankingRealtimePublisher
{
    Task PublicarRankingTriviaActualizadoAsync(Guid partidaId, RankingJuegoResponse ranking, CancellationToken cancellationToken);
    Task PublicarRankingBdtActualizadoAsync(Guid partidaId, RankingJuegoResponse ranking, CancellationToken cancellationToken);
    Task PublicarRankingConsolidadoCalculadoAsync(RankingConsolidadoResponse ranking, CancellationToken cancellationToken);
}
