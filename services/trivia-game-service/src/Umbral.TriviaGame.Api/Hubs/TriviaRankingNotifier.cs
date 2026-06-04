using Microsoft.AspNetCore.SignalR;
using Umbral.TriviaGame.Application.Ports;

namespace Umbral.TriviaGame.Api.Hubs;

internal sealed class TriviaRankingNotifier : ITriviaRankingNotifier
{
    private readonly IHubContext<TriviaRankingHub> _hubContext;

    public TriviaRankingNotifier(IHubContext<TriviaRankingHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyRankingUpdated(Guid partidaId, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients
            .Group($"game-{partidaId}")
            .SendAsync("RankingUpdated", new { partidaId }, cancellationToken);
    }
}
