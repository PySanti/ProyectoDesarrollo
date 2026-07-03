using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.Handlers.Commands;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class IntentarInicioAutomaticoCommandHandlerTests
{
    private static readonly DateTime TDue = new(2026, 6, 26, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime TBefore = new(2026, 6, 26, 11, 0, 0, DateTimeKind.Utc);

    private static SesionPartida Sesion(Guid partidaId, ModoInicioPartida modo, DateTime? tiempoInicio, int inscritos)
    {
        var lista = Enumerable.Range(1, 2).Select(o => new JuegoResumen(Guid.NewGuid(), o, TipoJuego.Trivia)).ToList();
        var snapshot = new ConfiguracionSnapshot("Copa", Modalidad.Individual, modo, tiempoInicio, 1, 5, lista);
        var sesion = SesionPartida.Publicar(partidaId, snapshot);
        for (var i = 0; i < inscritos; i++) sesion.Inscribir(Guid.NewGuid(), false, i, TBefore);
        return sesion;
    }

    private static (IntentarInicioAutomaticoCommandHandler handler, FakeOperacionesSesionUnitOfWork uow, FakeSesionEventsPublisher events) Build(
        FakeSesionPartidaRepository repo, DateTime now)
    {
        var uow = new FakeOperacionesSesionUnitOfWork();
        var events = new FakeSesionEventsPublisher();
        var time = new FakeTimeProvider(now);
        return (new IntentarInicioAutomaticoCommandHandler(repo, uow, events, time), uow, events);
    }

    [Fact]
    public async Task When_due_and_minimums_met_starts_and_publishes()
    {
        var partidaId = Guid.NewGuid();
        var repo = new FakeSesionPartidaRepository();
        repo.Add(Sesion(partidaId, ModoInicioPartida.Automatico, TDue, inscritos: 1));
        var (handler, uow, events) = Build(repo, TDue);

        var response = await handler.Handle(new IntentarInicioAutomaticoCommand(partidaId), CancellationToken.None);

        Assert.Equal("Iniciada", response.Estado);
        Assert.Equal(1, uow.SaveCount);
        Assert.Single(events.PartidasIniciadas);
        Assert.Single(events.JuegosActivados);
    }

    [Fact]
    public async Task When_not_due_is_noop_no_save_no_event()
    {
        var partidaId = Guid.NewGuid();
        var repo = new FakeSesionPartidaRepository();
        repo.Add(Sesion(partidaId, ModoInicioPartida.Automatico, TDue, inscritos: 1));
        var (handler, uow, events) = Build(repo, TBefore); // before due

        var response = await handler.Handle(new IntentarInicioAutomaticoCommand(partidaId), CancellationToken.None);

        Assert.Equal("Lobby", response.Estado);
        Assert.Equal(0, uow.SaveCount);
        Assert.Empty(events.PartidasIniciadas);
        Assert.Empty(events.JuegosActivados);
        Assert.Empty(events.PartidasCanceladas);
    }
}
