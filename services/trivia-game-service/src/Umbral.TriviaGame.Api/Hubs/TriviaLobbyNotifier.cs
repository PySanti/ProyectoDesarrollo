using Microsoft.AspNetCore.SignalR;
using Umbral.TriviaGame.Application.Ports;

namespace Umbral.TriviaGame.Api.Hubs;

public sealed class TriviaLobbyNotifier : ITriviaLobbyNotifier
{
    private readonly IHubContext<TriviaLobbyHub> _hubContext;

    public TriviaLobbyNotifier(IHubContext<TriviaLobbyHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyParticipantJoined(Guid partidaId, string usuarioId, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"game-{partidaId}").SendAsync("ParticipantJoined", new
        {
            PartidaId = partidaId,
            UsuarioId = usuarioId
        }, cancellationToken);
    }

    public async Task NotifyGameStarted(Guid partidaId, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"game-{partidaId}").SendAsync("GameStarted", new
        {
            PartidaId = partidaId
        }, cancellationToken);
    }

    public async Task NotifyGameCancelled(Guid partidaId, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"game-{partidaId}").SendAsync("GameCancelled", new
        {
            PartidaId = partidaId
        }, cancellationToken);
    }
}
