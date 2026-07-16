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
    private static readonly DateTime T0 = new(2026, 7, 16, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_persists_partida_and_returns_id()
    {
        var repo = new FakePartidaRepository();
        var uow = new FakePartidasUnitOfWork();
        var handler = new CrearPartidaCommandHandler(repo, uow, new FakeTimeProvider(T0));
        var command = new CrearPartidaCommand("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10);

        var response = await handler.Handle(command, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, response.PartidaId);
        Assert.True(repo.Store.ContainsKey(response.PartidaId));
        Assert.Equal(1, uow.SaveCount);
        Assert.Null(repo.Store[response.PartidaId].Estado); // not published yet
    }

    [Fact]
    public async Task Handle_toma_la_fecha_de_creacion_del_reloj_inyectado()
    {
        var repo = new FakePartidaRepository();
        var handler = new CrearPartidaCommandHandler(repo, new FakePartidasUnitOfWork(), new FakeTimeProvider(T0));
        var command = new CrearPartidaCommand("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10);

        var response = await handler.Handle(command, CancellationToken.None);

        // Fija el instante exacto. Con DateTime.UtcNow ambiente esto solo podria aseverar
        // "cerca de ahora", que es la razon de que el reloj se inyecte.
        Assert.Equal(T0, repo.Store[response.PartidaId].FechaCreacion);
    }
}
