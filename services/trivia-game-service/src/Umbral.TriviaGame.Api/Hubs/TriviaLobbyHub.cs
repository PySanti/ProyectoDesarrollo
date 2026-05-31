using Microsoft.AspNetCore.SignalR;

namespace Umbral.TriviaGame.Api.Hubs;

public sealed class TriviaLobbyHub : Hub
{
    public async Task JoinGameGroup(string gameId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"game-{gameId}");
    }

    public async Task LeaveGameGroup(string gameId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"game-{gameId}");
    }
}
