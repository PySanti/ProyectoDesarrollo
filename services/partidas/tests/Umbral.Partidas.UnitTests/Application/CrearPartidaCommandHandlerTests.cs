using System;
using System.Threading;
using System.Threading.Tasks;
using Umbral.Partidas.Application.Commands;
using Umbral.Partidas.Application.Handlers.Commands;
using Umbral.Partidas.Domain.Enums;
using Umbral.Partidas.UnitTests.Application.Fakes;

namespace Umbral.Partidas.UnitTests.Application;

public class CrearPartidaCommandHandlerTests
{
    [Fact]
    public async Task Handle_persists_partida_and_returns_id()
    {
        var repo = new FakePartidaRepository();
        var uow = new FakePartidasUnitOfWork();
        var handler = new CrearPartidaCommandHandler(repo, uow);
        var command = new CrearPartidaCommand("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10);

        var response = await handler.Handle(command, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, response.PartidaId);
        Assert.True(repo.Store.ContainsKey(response.PartidaId));
        Assert.Equal(1, uow.SaveCount);
        Assert.Null(repo.Store[response.PartidaId].Estado); // not published yet
    }
}
