using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Handlers.Commands;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class PublicarPartidaCommandHandlerTests
{
    private static ConfiguracionPartidaDto Config(string modalidad = "Individual", int juegos = 1) =>
        new("Copa", modalidad, "Manual", null, 1, 10,
            Enumerable.Range(1, juegos).Select(o => new JuegoResumenDto(Guid.NewGuid(), o, "Trivia")).ToList());

    private static PublicarPartidaCommandHandler Handler(
        FakeSesionPartidaRepository repo, FakeOperacionesSesionUnitOfWork uow,
        FakeConfiguracionPartidaClient client, FakeSesionEventsPublisher events)
        => new(repo, uow, client, events);

    [Fact]
    public async Task Publishes_session_in_lobby_and_emits_event()
    {
        var repo = new FakeSesionPartidaRepository();
        var uow = new FakeOperacionesSesionUnitOfWork();
        var client = new FakeConfiguracionPartidaClient(Config(juegos: 2));
        var events = new FakeSesionEventsPublisher();
        var partidaId = Guid.NewGuid();

        var lobby = await Handler(repo, uow, client, events)
            .Handle(new PublicarPartidaCommand(partidaId, "Bearer abc"), CancellationToken.None);

        Assert.Equal("Lobby", lobby.Estado);
        Assert.Equal(partidaId, lobby.PartidaId);
        Assert.Equal(0, lobby.InscritosActivos);
        Assert.True(repo.Store.ContainsKey(partidaId));
        Assert.Equal(1, uow.SaveCount);
        Assert.Equal(1, events.PublishCount);
        Assert.Equal(partidaId, events.LastEvent!.PartidaId);
        Assert.Equal("Bearer abc", client.LastBearerToken);
    }

    [Fact]
    public async Task Throws_when_config_not_found()
    {
        var handler = Handler(new FakeSesionPartidaRepository(), new FakeOperacionesSesionUnitOfWork(),
            new FakeConfiguracionPartidaClient(null), new FakeSesionEventsPublisher());

        await Assert.ThrowsAsync<PartidaConfigNoEncontradaException>(
            () => handler.Handle(new PublicarPartidaCommand(Guid.NewGuid(), null), CancellationToken.None));
    }

    [Fact]
    public async Task Throws_when_already_published()
    {
        var repo = new FakeSesionPartidaRepository();
        var uow = new FakeOperacionesSesionUnitOfWork();
        var client = new FakeConfiguracionPartidaClient(Config());
        var events = new FakeSesionEventsPublisher();
        var partidaId = Guid.NewGuid();
        await Handler(repo, uow, client, events).Handle(new PublicarPartidaCommand(partidaId, null), CancellationToken.None);

        await Assert.ThrowsAsync<SesionYaPublicadaException>(
            () => Handler(repo, uow, client, events).Handle(new PublicarPartidaCommand(partidaId, null), CancellationToken.None));
    }

    [Fact]
    public async Task Throws_when_config_has_no_games()
    {
        var client = new FakeConfiguracionPartidaClient(
            new ConfiguracionPartidaDto("Copa", "Individual", "Manual", null, 1, 10, new List<JuegoResumenDto>()));
        var handler = Handler(new FakeSesionPartidaRepository(), new FakeOperacionesSesionUnitOfWork(),
            client, new FakeSesionEventsPublisher());

        await Assert.ThrowsAsync<Umbral.OperacionesSesion.Domain.Exceptions.PartidaNoPublicableException>(
            () => handler.Handle(new PublicarPartidaCommand(Guid.NewGuid(), null), CancellationToken.None));
    }
}
