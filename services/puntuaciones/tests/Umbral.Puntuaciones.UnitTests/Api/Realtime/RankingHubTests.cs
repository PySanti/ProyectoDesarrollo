using Microsoft.AspNetCore.SignalR;
using Umbral.Puntuaciones.Api.Realtime;
using Umbral.Puntuaciones.Domain.Entities;
using Umbral.Puntuaciones.Domain.Enums;
using Umbral.Puntuaciones.UnitTests.Application.Fakes;

namespace Umbral.Puntuaciones.UnitTests.Api.Realtime;

public class RankingHubTests
{
    private static (RankingHub Hub, FakeGroupManager Groups, FakeProyeccionesRepository Repo) Construir(string connId = "c1")
    {
        var repo = new FakeProyeccionesRepository();
        var groups = new FakeGroupManager();
        var hub = new RankingHub(repo)
        {
            Context = new FakeHubCallerContext(connId),
            Groups = groups
        };
        return (hub, groups, repo);
    }

    [Fact]
    public async Task Suscribir_con_partida_proyectada_une_al_grupo()
    {
        var (hub, groups, repo) = Construir();
        var partidaId = Guid.NewGuid();
        repo.AddPartida(PartidaProyectada.DesdePublicacion(partidaId, Guid.NewGuid(), Modalidad.Individual));

        await hub.SuscribirAPartida(partidaId);

        Assert.Contains(("c1", RankingRealtimeMessages.GrupoPartida(partidaId)), groups.Added);
    }

    [Fact]
    public async Task Suscribir_a_partida_desconocida_lanza_HubException_y_no_une()
    {
        var (hub, groups, _) = Construir();

        await Assert.ThrowsAsync<HubException>(() => hub.SuscribirAPartida(Guid.NewGuid()));

        Assert.Empty(groups.Added);
    }

    [Fact]
    public async Task Desuscribir_remueve_del_grupo()
    {
        var (hub, groups, _) = Construir();
        var partidaId = Guid.NewGuid();

        await hub.DesuscribirDePartida(partidaId);

        Assert.Contains(("c1", RankingRealtimeMessages.GrupoPartida(partidaId)), groups.Removed);
    }
}
