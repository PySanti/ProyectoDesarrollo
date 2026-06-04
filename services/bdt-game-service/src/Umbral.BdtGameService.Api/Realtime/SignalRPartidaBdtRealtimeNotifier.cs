using Microsoft.AspNetCore.SignalR;
using Umbral.BdtGameService.Application.Abstractions.Realtime;
using Umbral.BdtGameService.Application.Games.Start;

namespace Umbral.BdtGameService.Api.Realtime;

public sealed class SignalRPartidaBdtRealtimeNotifier : IPartidaBdtRealtimeNotifier
{
    private readonly IHubContext<BdtPartidaHub> _hubContext;

    public SignalRPartidaBdtRealtimeNotifier(IHubContext<BdtPartidaHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyPartidaBdtIniciadaAsync(IniciarPartidaBdtResponse response, CancellationToken cancellationToken)
    {
        var payload = PartidaBdtIniciadaRealtimeMessage.FromResponse(response, DateTime.UtcNow);

        await _hubContext.Clients
            .Group(BdtPartidaHub.PartidaGroupName(response.PartidaId))
            .SendAsync(BdtPartidaHub.PartidaBdtIniciadaMethod, payload, cancellationToken);
    }
}

public sealed record PartidaBdtIniciadaRealtimeMessage(
    string Type,
    int Version,
    Guid PartidaId,
    string Estado,
    string Modalidad,
    PartidaBdtIniciadaEtapaActivaMessage EtapaActiva,
    DateTime OccurredOnUtc)
{
    public static PartidaBdtIniciadaRealtimeMessage FromResponse(IniciarPartidaBdtResponse response, DateTime occurredOnUtc)
    {
        return new PartidaBdtIniciadaRealtimeMessage(
            BdtPartidaHub.PartidaBdtIniciadaMethod,
            1,
            response.PartidaId,
            response.Estado,
            response.Modalidad,
            new PartidaBdtIniciadaEtapaActivaMessage(
                response.EtapaActiva.EtapaId,
                response.EtapaActiva.Orden,
                response.EtapaActiva.TiempoLimiteSegundos,
                response.EtapaActiva.IniciadaEnUtc,
                response.EtapaActiva.CierraEnUtc),
            occurredOnUtc);
    }
}

public sealed record PartidaBdtIniciadaEtapaActivaMessage(
    Guid EtapaId,
    int Orden,
    int TiempoLimiteSegundos,
    DateTime IniciadaEnUtc,
    DateTime CierraEnUtc);
