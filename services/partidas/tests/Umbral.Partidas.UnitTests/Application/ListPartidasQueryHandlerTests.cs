using System;
using System.Threading;
using System.Threading.Tasks;
using Umbral.Partidas.Application.Handlers.Queries;
using Umbral.Partidas.Application.Queries;
using Umbral.Partidas.Domain.Entities;
using Umbral.Partidas.Domain.Enums;
using Umbral.Partidas.Domain.ValueObjects;
using Umbral.Partidas.UnitTests.Application.Fakes;

namespace Umbral.Partidas.UnitTests.Application;

public class ListPartidasQueryHandlerTests
{
    private static readonly DateTime T0 = new(2026, 7, 16, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_returns_summaries_with_game_counts()
    {
        var partidas = new FakePartidaRepository();
        var partida = Partida.Crear(NombrePartida.Crear("Copa"), Modalidad.Equipo, ModoInicioPartida.Manual, null, 2, 8, T0);
        partida.AgregarJuego(JuegoId.New(), 1, TipoJuego.Trivia);
        partidas.Add(partida);

        var handler = new ListPartidasQueryHandler(partidas);
        var result = await handler.Handle(new ListPartidasQuery(), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("Copa", result[0].NombrePartida);
        Assert.Equal("Equipo", result[0].Modalidad);
        Assert.Equal(1, result[0].CantidadJuegos);
        Assert.Null(result[0].Estado);
    }

    [Fact]
    public async Task Handle_returns_empty_when_no_partidas()
    {
        var handler = new ListPartidasQueryHandler(new FakePartidaRepository());
        var result = await handler.Handle(new ListPartidasQuery(), CancellationToken.None);
        Assert.Empty(result);
    }
}
