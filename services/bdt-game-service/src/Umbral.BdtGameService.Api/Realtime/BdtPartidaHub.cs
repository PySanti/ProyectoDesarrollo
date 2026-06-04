using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Umbral.BdtGameService.Application.Abstractions.Realtime;

namespace Umbral.BdtGameService.Api.Realtime;

[Authorize]
public sealed class BdtPartidaHub : Hub
{
    public const string PartidaBdtIniciadaMethod = "PartidaBDTIniciada";

    private readonly IPartidaBdtSubscriptionAuthorizer _subscriptionAuthorizer;

    public BdtPartidaHub(IPartidaBdtSubscriptionAuthorizer subscriptionAuthorizer)
    {
        _subscriptionAuthorizer = subscriptionAuthorizer;
    }

    public static string PartidaGroupName(Guid partidaId)
    {
        return $"bdt-partida-{partidaId:N}";
    }

    public async Task SubscribeToPartida(Guid partidaId)
    {
        if (partidaId == Guid.Empty)
        {
            throw new HubException("PartidaId invalido.");
        }

        var userIdClaim = Context.User?.FindFirst("sub")?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            throw new HubException("Usuario invalido.");
        }

        var canSubscribe = await _subscriptionAuthorizer.CanSubscribeAsync(
            partidaId,
            userId,
            Context.User?.IsInRole("Operador") == true,
            Context.User?.IsInRole("Participante") == true,
            Context.ConnectionAborted);

        if (!canSubscribe)
        {
            throw new HubException("No autorizado para suscribirse a esta partida BDT.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, PartidaGroupName(partidaId));
    }

    public async Task UnsubscribeFromPartida(Guid partidaId)
    {
        if (partidaId == Guid.Empty)
        {
            throw new HubException("PartidaId invalido.");
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, PartidaGroupName(partidaId));
    }
}
