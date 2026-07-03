using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Handlers.Commands;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class FinalizarJuegoActualCommandHandlerTests
{
    private static readonly DateTime T0 = new(2026, 6, 26, 12, 0, 0, DateTimeKind.Utc);

    private static SesionPartida Iniciada(Guid partidaId, int juegos)
    {
        var lista = Enumerable.Range(1, juegos).Select(o => new JuegoResumen(Guid.NewGuid(), o, TipoJuego.Trivia)).ToList();
        var snapshot = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 5, lista);
        var sesion = SesionPartida.Publicar(partidaId, snapshot);
        sesion.Inscribir(Guid.NewGuid(), false, 0, T0);
        sesion.Iniciar(T0);
        return sesion;
    }

    private static (FinalizarJuegoActualCommandHandler handler, FakeOperacionesSesionUnitOfWork uow, FakeSesionEventsPublisher events) Build(FakeSesionPartidaRepository repo)
    {
        var uow = new FakeOperacionesSesionUnitOfWork();
        var events = new FakeSesionEventsPublisher();
        var time = new FakeTimeProvider(T0);
        return (new FinalizarJuegoActualCommandHandler(repo, uow, events, time), uow, events);
    }

    [Fact]
    public async Task Advance_publishes_juego_activado()
    {
        var partidaId = Guid.NewGuid();
        var repo = new FakeSesionPartidaRepository();
        repo.Add(Iniciada(partidaId, juegos: 2));
        var (handler, uow, events) = Build(repo);

        var response = await handler.Handle(new FinalizarJuegoActualCommand(partidaId), CancellationToken.None);

        Assert.Equal("Iniciada", response.Estado);
        Assert.False(response.Terminada);
        Assert.Equal(1, response.JuegoFinalizadoOrden);
        Assert.Equal(2, response.JuegoActivadoOrden);
        Assert.Equal(1, uow.SaveCount);
        Assert.Single(events.JuegosActivados);
        Assert.Empty(events.PartidasFinalizadas);
    }

    [Fact]
    public async Task Finishing_last_game_publishes_partida_finalizada()
    {
        var partidaId = Guid.NewGuid();
        var repo = new FakeSesionPartidaRepository();
        repo.Add(Iniciada(partidaId, juegos: 1));
        var (handler, uow, events) = Build(repo);

        var response = await handler.Handle(new FinalizarJuegoActualCommand(partidaId), CancellationToken.None);

        Assert.Equal("Terminada", response.Estado);
        Assert.True(response.Terminada);
        Assert.Null(response.JuegoActivadoOrden);
        Assert.Single(events.PartidasFinalizadas);
        Assert.Empty(events.JuegosActivados);
    }

    [Fact]
    public async Task Unknown_partida_throws()
    {
        var (handler, _, _) = Build(new FakeSesionPartidaRepository());
        await Assert.ThrowsAsync<SesionNoEncontradaException>(
            () => handler.Handle(new FinalizarJuegoActualCommand(Guid.NewGuid()), CancellationToken.None));
    }
}
