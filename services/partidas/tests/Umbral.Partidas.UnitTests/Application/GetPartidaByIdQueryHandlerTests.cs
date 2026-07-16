using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbral.Partidas.Application.Exceptions;
using Umbral.Partidas.Application.Handlers.Queries;
using Umbral.Partidas.Application.Queries;
using Umbral.Partidas.Domain.Entities;
using Umbral.Partidas.Domain.Enums;
using Umbral.Partidas.Domain.ValueObjects;
using Umbral.Partidas.UnitTests.Application.Fakes;

namespace Umbral.Partidas.UnitTests.Application;

public class GetPartidaByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_returns_detail_with_ordered_games_and_content()
    {
        var partidas = new FakePartidaRepository();
        var trivias = new FakeJuegoTriviaRepository();
        var bdts = new FakeJuegoBDTRepository();

        var partida = Partida.Crear(
            NombrePartida.Crear("Copa"), Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10,
            new DateTime(2026, 7, 16, 12, 0, 0, DateTimeKind.Utc));
        var trivia = JuegoTrivia.Crear(partida.PartidaId, 1, new[]
        {
            new PreguntaSpec("Q", new List<OpcionSpec> { new("A", true), new("B", false) }, 10, 30)
        });
        var bdt = JuegoBDT.Crear(partida.PartidaId, 2, "Plaza", new[] { new EtapaSpec(1, Guid.NewGuid().ToString(), 50, 120) });
        partida.AgregarJuego(trivia.JuegoId, 1, TipoJuego.Trivia);
        partida.AgregarJuego(bdt.JuegoId, 2, TipoJuego.BusquedaDelTesoro);
        partidas.Add(partida);
        trivias.Add(trivia);
        bdts.Add(bdt);

        var handler = new GetPartidaByIdQueryHandler(partidas, trivias, bdts);
        var detail = await handler.Handle(new GetPartidaByIdQuery(partida.PartidaId.Valor), CancellationToken.None);

        Assert.Equal("Copa", detail.NombrePartida);
        Assert.Null(detail.Estado);
        Assert.Equal(2, detail.Juegos.Count);
        Assert.Equal(1, detail.Juegos[0].Orden);
        Assert.Equal("Trivia", detail.Juegos[0].TipoJuego);
        Assert.NotNull(detail.Juegos[0].Trivia);
        Assert.Single(detail.Juegos[0].Trivia!.Preguntas);
        Assert.Equal("BusquedaDelTesoro", detail.Juegos[1].TipoJuego);
        Assert.NotNull(detail.Juegos[1].BDT);
        Assert.Equal("Plaza", detail.Juegos[1].BDT!.AreaBusqueda);
        Assert.Equal("Pendiente", detail.Juegos[0].Estado);
        Assert.Equal("Pendiente", detail.Juegos[1].Estado);
    }

    [Fact]
    public async Task Handle_throws_when_partida_not_found()
    {
        var handler = new GetPartidaByIdQueryHandler(
            new FakePartidaRepository(), new FakeJuegoTriviaRepository(), new FakeJuegoBDTRepository());

        await Assert.ThrowsAsync<PartidaNoEncontradaException>(
            () => handler.Handle(new GetPartidaByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }
}
