using Microsoft.AspNetCore.SignalR;
using Umbral.Puntuaciones.Api.Realtime;
using Umbral.Puntuaciones.Application.DTOs;
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.UnitTests.Api.Realtime;

public sealed class FakeClientProxy : IClientProxy
{
    public List<(string Method, object?[] Args)> Sent { get; } = new();

    public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
    {
        Sent.Add((method, args));
        return Task.CompletedTask;
    }
}

public sealed class FakeHubClients : IHubClients
{
    public FakeClientProxy Proxy { get; } = new();
    public string? GrupoSolicitado { get; private set; }

    public IClientProxy Group(string groupName)
    {
        GrupoSolicitado = groupName;
        return Proxy;
    }

    public IClientProxy All => Proxy;
    public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => Proxy;
    public IClientProxy Client(string connectionId) => Proxy;
    public IClientProxy Clients(IReadOnlyList<string> connectionIds) => Proxy;
    public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => Proxy;
    public IClientProxy Groups(IReadOnlyList<string> groupNames) => Proxy;
    public IClientProxy User(string userId) => Proxy;
    public IClientProxy Users(IReadOnlyList<string> userIds) => Proxy;
}

public sealed class FakeHubContext : IHubContext<RankingHub>
{
    public FakeHubClients FakeClients { get; } = new();
    public IHubClients Clients => FakeClients;
    public IGroupManager Groups { get; } = new FakeGroupManager();
}

public class SignalRRankingRealtimePublisherTests
{
    private static RankingJuegoResponse Ranking(Guid juegoId) =>
        new(juegoId, TipoJuego.Trivia, DateTime.UtcNow,
            new[] { new EntradaRankingDto(1, Guid.NewGuid(), TipoCompetidor.Participante, 10, 1500, 1) });

    [Fact]
    public async Task Trivia_envia_al_grupo_de_la_partida_con_mensaje_y_payload()
    {
        var hub = new FakeHubContext();
        var publisher = new SignalRRankingRealtimePublisher(hub);
        var partidaId = Guid.NewGuid();
        var ranking = Ranking(Guid.NewGuid());

        await publisher.PublicarRankingTriviaActualizadoAsync(partidaId, ranking, CancellationToken.None);

        Assert.Equal(RankingRealtimeMessages.GrupoPartida(partidaId), hub.FakeClients.GrupoSolicitado);
        var (method, args) = Assert.Single(hub.FakeClients.Proxy.Sent);
        Assert.Equal(RankingRealtimeMessages.RankingTriviaActualizado, method);
        Assert.Same(ranking, Assert.Single(args));
    }

    [Fact]
    public async Task Bdt_envia_el_mensaje_RankingBDTActualizado()
    {
        var hub = new FakeHubContext();
        var publisher = new SignalRRankingRealtimePublisher(hub);
        var partidaId = Guid.NewGuid();

        await publisher.PublicarRankingBdtActualizadoAsync(partidaId, Ranking(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(RankingRealtimeMessages.GrupoPartida(partidaId), hub.FakeClients.GrupoSolicitado);
        Assert.Equal(RankingRealtimeMessages.RankingBDTActualizado, hub.FakeClients.Proxy.Sent.Single().Method);
    }

    [Fact]
    public async Task Consolidado_usa_el_partidaId_del_payload()
    {
        var hub = new FakeHubContext();
        var publisher = new SignalRRankingRealtimePublisher(hub);
        var partidaId = Guid.NewGuid();
        var consolidado = new RankingConsolidadoResponse(partidaId, DateTime.UtcNow,
            Array.Empty<EntradaRankingConsolidadoDto>());

        await publisher.PublicarRankingConsolidadoCalculadoAsync(consolidado, CancellationToken.None);

        Assert.Equal(RankingRealtimeMessages.GrupoPartida(partidaId), hub.FakeClients.GrupoSolicitado);
        Assert.Equal(RankingRealtimeMessages.RankingConsolidadoCalculado, hub.FakeClients.Proxy.Sent.Single().Method);
    }
}
