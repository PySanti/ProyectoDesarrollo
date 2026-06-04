using Umbral.BdtGameService.Application.Games.Start;

namespace Umbral.BdtGameService.Application.Abstractions.Realtime;

public interface IPartidaBdtRealtimeNotifier
{
    Task NotifyPartidaBdtIniciadaAsync(IniciarPartidaBdtResponse response, CancellationToken cancellationToken);
}
