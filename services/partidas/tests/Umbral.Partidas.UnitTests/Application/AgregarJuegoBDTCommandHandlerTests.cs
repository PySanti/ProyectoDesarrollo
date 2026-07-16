using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Umbral.Partidas.Application.Commands;
using Umbral.Partidas.Application.DTOs;
using Umbral.Partidas.Application.Exceptions;
using Umbral.Partidas.Application.Handlers.Commands;
using Umbral.Partidas.Domain.Entities;
using Umbral.Partidas.Domain.Enums;
using Umbral.Partidas.Domain.ValueObjects;
using Umbral.Partidas.UnitTests.Application.Fakes;

namespace Umbral.Partidas.UnitTests.Application;

public class AgregarJuegoBDTCommandHandlerTests
{
    private static Partida NewPartida()
        => Partida.Crear(NombrePartida.Crear("Copa"), Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10,
            new DateTime(2026, 7, 16, 12, 0, 0, DateTimeKind.Utc));

    private static AgregarJuegoBDTCommand Command(Guid partidaId, int orden = 1) =>
        new(partidaId, orden, "Plaza central", new List<EtapaRequest>
        {
            new(1, Guid.NewGuid().ToString(), 50, 120)
        });

    [Fact]
    public async Task Handle_adds_bdt_game_to_both_aggregates_and_saves_once()
    {
        var partidas = new FakePartidaRepository();
        var juegos = new FakeJuegoBDTRepository();
        var uow = new FakePartidasUnitOfWork();
        var partida = NewPartida();
        partidas.Add(partida);

        var handler = new AgregarJuegoBDTCommandHandler(partidas, juegos, uow);
        var response = await handler.Handle(Command(partida.PartidaId.Valor), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, response.JuegoId);
        Assert.Single(juegos.Store);
        Assert.Single(partida.Juegos);
        Assert.Equal(TipoJuego.BusquedaDelTesoro, partida.Juegos[0].TipoJuego);
        Assert.Equal(1, uow.SaveCount);
    }

    [Fact]
    public async Task Handle_throws_when_partida_not_found()
    {
        var handler = new AgregarJuegoBDTCommandHandler(
            new FakePartidaRepository(), new FakeJuegoBDTRepository(), new FakePartidasUnitOfWork());

        await Assert.ThrowsAsync<PartidaNoEncontradaException>(
            () => handler.Handle(Command(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_does_not_save_when_orden_collides()
    {
        var partidas = new FakePartidaRepository();
        var juegos = new FakeJuegoBDTRepository();
        var uow = new FakePartidasUnitOfWork();
        var partida = NewPartida();
        partida.AgregarJuego(JuegoId.New(), 1, TipoJuego.Trivia); // orden 1 already taken
        partidas.Add(partida);

        var handler = new AgregarJuegoBDTCommandHandler(partidas, juegos, uow);

        await Assert.ThrowsAsync<Umbral.Partidas.Domain.Exceptions.OrdenJuegoDuplicadoException>(
            () => handler.Handle(Command(partida.PartidaId.Valor, orden: 1), CancellationToken.None));
        Assert.Empty(juegos.Store);
        Assert.Equal(0, uow.SaveCount);
    }
}
