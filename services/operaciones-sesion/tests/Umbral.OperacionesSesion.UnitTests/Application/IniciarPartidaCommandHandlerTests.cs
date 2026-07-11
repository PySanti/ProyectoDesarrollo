using System;
using System.Collections.Generic;
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

public class IniciarPartidaCommandHandlerTests
{
    private static readonly DateTime T0 = new(2026, 6, 26, 12, 0, 0, DateTimeKind.Utc);

    private static SesionPartida Sesion(Guid partidaId, int min, int max, int juegos, int inscritos)
    {
        var lista = Enumerable.Range(1, juegos).Select(o => new JuegoResumen(Guid.NewGuid(), o, TipoJuego.Trivia)).ToList();
        var snapshot = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, min, max, lista);
        var sesion = SesionPartida.Publicar(partidaId, snapshot);
        for (var i = 0; i < inscritos; i++)
        {
            var ins = sesion.Inscribir(Guid.NewGuid(), false, i, T0);
            sesion.AceptarInscripcion(ins.Id.Valor, i, T0); // HU-19: aceptar para que cuente en mínimos
        }
        return sesion;
    }

    private static (IniciarPartidaCommandHandler handler, FakeSesionPartidaRepository repo, FakeOperacionesSesionUnitOfWork uow, FakeSesionEventsPublisher events) Build()
    {
        var repo = new FakeSesionPartidaRepository();
        var uow = new FakeOperacionesSesionUnitOfWork();
        var events = new FakeSesionEventsPublisher();
        var time = new FakeTimeProvider(T0);
        return (new IniciarPartidaCommandHandler(repo, uow, events, time), repo, uow, events);
    }

    [Fact]
    public async Task Iniciar_minimums_met_starts_saves_and_publishes_iniciada_and_juego_activado()
    {
        var partidaId = Guid.NewGuid();
        var (handler, repo, uow, events) = Build();
        repo.Add(Sesion(partidaId, min: 1, max: 5, juegos: 2, inscritos: 1));

        var response = await handler.Handle(new IniciarPartidaCommand(partidaId), CancellationToken.None);

        Assert.Equal("Iniciada", response.Estado);
        Assert.Equal(1, response.JuegoActivadoOrden);
        Assert.Equal(1, uow.SaveCount);
        Assert.Single(events.PartidasIniciadas);
        Assert.Single(events.JuegosActivados);
        Assert.Empty(events.PartidasCanceladas);
        Assert.Equal(1, events.JuegosActivados[0].Orden);
    }

    [Fact]
    public async Task Iniciar_minimums_not_met_cancels_saves_and_publishes_cancelada()
    {
        var partidaId = Guid.NewGuid();
        var (handler, repo, uow, events) = Build();
        repo.Add(Sesion(partidaId, min: 2, max: 5, juegos: 1, inscritos: 1));

        var response = await handler.Handle(new IniciarPartidaCommand(partidaId), CancellationToken.None);

        Assert.Equal("Cancelada", response.Estado);
        Assert.Null(response.JuegoActivadoOrden);
        Assert.Equal(1, uow.SaveCount);
        Assert.Single(events.PartidasCanceladas);
        Assert.Equal("MinimosNoAlcanzados", events.PartidasCanceladas[0].Motivo);
        Assert.Empty(events.PartidasIniciadas);
    }

    [Fact]
    public async Task Iniciar_unknown_partida_throws()
    {
        var (handler, _, _, _) = Build();
        await Assert.ThrowsAsync<SesionNoEncontradaException>(
            () => handler.Handle(new IniciarPartidaCommand(Guid.NewGuid()), CancellationToken.None));
    }
}
