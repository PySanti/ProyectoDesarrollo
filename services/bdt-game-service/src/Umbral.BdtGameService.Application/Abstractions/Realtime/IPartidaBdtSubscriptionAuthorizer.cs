namespace Umbral.BdtGameService.Application.Abstractions.Realtime;

public interface IPartidaBdtSubscriptionAuthorizer
{
    Task<bool> CanSubscribeAsync(
        Guid partidaId,
        Guid userId,
        bool isOperator,
        bool isParticipant,
        CancellationToken cancellationToken);
}
