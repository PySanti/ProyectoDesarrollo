namespace Umbral.TriviaGame.Application.Ports;

public sealed class StubTriviaLobbyNotifier : ITriviaLobbyNotifier
{
    public Task NotifyParticipantJoined(Guid partidaId, string usuarioId, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task NotifyGameStarted(Guid partidaId, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task NotifyGameCancelled(Guid partidaId, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
