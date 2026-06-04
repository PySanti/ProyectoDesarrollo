namespace Umbral.TriviaGame.Application.Ports;

public interface ITriviaLobbyNotifier
{
    Task NotifyParticipantJoined(Guid partidaId, string usuarioId, CancellationToken cancellationToken = default);

    Task NotifyGameStarted(Guid partidaId, CancellationToken cancellationToken = default);

    Task NotifyGameCancelled(Guid partidaId, CancellationToken cancellationToken = default);
}
