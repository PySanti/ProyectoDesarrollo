namespace Umbral.TriviaGame.Application.Ports;

public interface ITriviaRankingNotifier
{
    Task NotifyRankingUpdated(Guid partidaId, CancellationToken cancellationToken = default);
}
