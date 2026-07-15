using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.Handlers.Commands;
using Umbral.OperacionesSesion.Domain.Abstractions.Persistence;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class BarrerIniciosAutomaticosCommandHandlerTests
{
    private static readonly DateTime TDue = new(2026, 6, 29, 12, 0, 0, DateTimeKind.Utc);

    private static SesionPartida AutoEnLobby(DateTime tiempoInicio, int inscritos, int minimos)
    {
        // Cada juego lleva una pregunta: al iniciar, AplicarInicio debe activar el primer paso
        // (PreguntaActivada). Sin la pregunta el inicio "pasaría" sin paso activo — exactamente el
        // bug C1 que el grafo del scan de auto-inicio debe prevenir en Npgsql.
        var juegos = Enumerable.Range(1, 2).Select(o => new JuegoResumen(
            Guid.NewGuid(), o, TipoJuego.Trivia,
            new[]
            {
                new PreguntaSnapshot(Guid.NewGuid(), 1, $"Q{o}", 10, 30,
                    new[] { new OpcionSnapshot(Guid.NewGuid(), "ok", true), new OpcionSnapshot(Guid.NewGuid(), "no", false) })
            })).ToList();
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Automatico, tiempoInicio, minimos, 5, juegos);
        var s = SesionPartida.Publicar(Guid.NewGuid(), snap);
        for (var i = 0; i < inscritos; i++)
        {
            var ins = s.Inscribir(Guid.NewGuid(), false, i, tiempoInicio.AddSeconds(-1));
            s.AceptarInscripcion(ins.Id.Valor, i, tiempoInicio.AddSeconds(-1)); // HU-19: aceptar para que cuente en mínimos
        }
        return s;
    }

    private static BarrerIniciosAutomaticosCommandHandler Build(
        ISesionPartidaRepository repo, IOperacionesSesionUnitOfWork uow, FakeSesionEventsPublisher events, DateTime now)
        => new(repo, uow, events, new FakeTimeProvider(now), NullLogger<BarrerIniciosAutomaticosCommandHandler>.Instance);

    [Fact]
    public async Task Inicia_los_due_con_minimos_cumplidos()
    {
        var repo = new FakeSesionPartidaRepository();
        repo.Add(AutoEnLobby(TDue, inscritos: 1, minimos: 1));
        var uow = new FakeOperacionesSesionUnitOfWork();
        var events = new FakeSesionEventsPublisher();
        var handler = Build(repo, uow, events, TDue);

        var n = await handler.Handle(new BarrerIniciosAutomaticosCommand(), CancellationToken.None);

        Assert.Equal(1, n);
        Assert.Equal(1, uow.SaveCount);            // probó el path de save (estado in-memory no lo implica)
        Assert.Single(events.PartidasIniciadas);
        Assert.Single(events.JuegosActivados);
        Assert.Single(events.PreguntasActivadas);  // el primer paso se activó (guard del bug C1)
        Assert.Empty(events.PartidasCanceladas);   // iniciada NO emite cancelada
        Assert.Equal(EstadoSesion.Iniciada, repo.Store.Values.Single().Estado);
    }

    [Fact]
    public async Task Auto_cancela_los_due_bajo_minimos()
    {
        var repo = new FakeSesionPartidaRepository();
        repo.Add(AutoEnLobby(TDue, inscritos: 0, minimos: 2)); // 0 < 2 → cancela
        var uow = new FakeOperacionesSesionUnitOfWork();
        var events = new FakeSesionEventsPublisher();
        var handler = Build(repo, uow, events, TDue);

        var n = await handler.Handle(new BarrerIniciosAutomaticosCommand(), CancellationToken.None);

        Assert.Equal(1, n);
        Assert.Equal(1, uow.SaveCount);            // probó el path de save
        Assert.Single(events.PartidasCanceladas);
        Assert.Empty(events.PartidasIniciadas);    // cancelada NO emite iniciada/juego
        Assert.Empty(events.JuegosActivados);
        Assert.Equal(EstadoSesion.Cancelada, repo.Store.Values.Single().Estado);
    }

    [Fact]
    public async Task No_due_no_es_candidato_y_no_hace_nada()
    {
        var repo = new FakeSesionPartidaRepository();
        repo.Add(AutoEnLobby(TDue.AddHours(1), inscritos: 1, minimos: 1)); // futura
        var uow = new FakeOperacionesSesionUnitOfWork();
        var events = new FakeSesionEventsPublisher();
        var handler = Build(repo, uow, events, TDue);

        var n = await handler.Handle(new BarrerIniciosAutomaticosCommand(), CancellationToken.None);

        Assert.Equal(0, n);
        Assert.Equal(0, uow.SaveCount);
        Assert.Empty(events.PartidasIniciadas);
    }
}
