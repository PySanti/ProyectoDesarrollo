using Microsoft.AspNetCore.SignalR;
using Umbral.Puntuaciones.Application.DTOs;
using Umbral.Puntuaciones.Application.Interfaces;

namespace Umbral.Puntuaciones.Api.Realtime;

public sealed class SignalRRankingRealtimePublisher : IRankingRealtimePublisher
{
    private readonly IHubContext<RankingHub> _hub;

    public SignalRRankingRealtimePublisher(IHubContext<RankingHub> hub) => _hub = hub;

    private Task Difundir(Guid partidaId, string mensaje, object payload, CancellationToken ct) =>
        _hub.Clients.Group(RankingRealtimeMessages.GrupoPartida(partidaId)).SendAsync(mensaje, payload, ct);

    public Task PublicarRankingTriviaActualizadoAsync(Guid partidaId, RankingJuegoResponse ranking, CancellationToken cancellationToken) =>
        Difundir(partidaId, RankingRealtimeMessages.RankingTriviaActualizado, ranking, cancellationToken);

    public Task PublicarRankingBdtActualizadoAsync(Guid partidaId, RankingJuegoResponse ranking, CancellationToken cancellationToken) =>
        Difundir(partidaId, RankingRealtimeMessages.RankingBDTActualizado, ranking, cancellationToken);

    public Task PublicarRankingConsolidadoCalculadoAsync(RankingConsolidadoResponse ranking, CancellationToken cancellationToken) =>
        Difundir(ranking.PartidaId, RankingRealtimeMessages.RankingConsolidadoCalculado, ranking, cancellationToken);
}
