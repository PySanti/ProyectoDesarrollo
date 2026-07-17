using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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

public class BarrerTimeoutsCommandHandlerTests
{
    private static readonly DateTime T0 = new(2026, 6, 29, 12, 0, 0, DateTimeKind.Utc);

    private static PreguntaSnapshot P(int orden, int limite) =>
        new(Guid.NewGuid(), orden, $"Q{orden}", 10, limite,
            new[] { new OpcionSnapshot(Guid.NewGuid(), "ok", true), new OpcionSnapshot(Guid.NewGuid(), "no", false) });

    private static SesionPartida TriviaIniciada(int limite)
    {
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[] { P(1, limite), P(2, limite) });
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        var s = SesionPartida.Publicar(Guid.NewGuid(), snap);
        var insc = s.Inscribir(Guid.NewGuid(), false, 0, T0);
        s.AceptarInscripcion(insc.Id.Valor, 0, T0); // HU-19: aceptar para que cuente en mínimos
        s.Iniciar(T0);
        return s;
    }

    private static BarrerTimeoutsCommandHandler Build(
        ISesionPartidaRepository repo, IOperacionesSesionUnitOfWork uow, FakeSesionEventsPublisher events, DateTime now)
        => new(repo, uow, events, new FakeTimeProvider(now), NullLogger<BarrerTimeoutsCommandHandler>.Instance);

    [Fact]
    public async Task Cierra_vencida_emite_eventos_y_cuenta()
    {
        var repo = new FakeSesionPartidaRepository();
        repo.Add(TriviaIniciada(30));
        var uow = new FakeOperacionesSesionUnitOfWork();
        var events = new FakeSesionEventsPublisher();
        var handler = Build(repo, uow, events, T0.AddSeconds(31));

        var n = await handler.Handle(new BarrerTimeoutsCommand(), CancellationToken.None);

        Assert.Equal(1, n);
        Assert.Equal(1, uow.SaveCount);
        Assert.Single(events.PreguntasCerradas);
        Assert.Single(events.PreguntasActivadas); // se activó Q2
    }

    [Fact]
    public async Task Cierra_vencida_publica_texto_opcion_correcta_en_cierre()
    {
        var sesion = TriviaIniciada(30);
        var correcta = sesion.Juegos.Single().Preguntas.Single(p => p.Orden == 1).Opciones.First(o => o.EsCorrecta);
        var repo = new FakeSesionPartidaRepository();
        repo.Add(sesion);
        var uow = new FakeOperacionesSesionUnitOfWork();
        var events = new FakeSesionEventsPublisher();
        var handler = Build(repo, uow, events, T0.AddSeconds(31));

        await handler.Handle(new BarrerTimeoutsCommand(), CancellationToken.None);

        var cerrada = Assert.Single(events.PreguntasCerradas);
        Assert.Equal(correcta.OpcionId, cerrada.OpcionCorrectaId);
        Assert.Equal("ok", cerrada.TextoOpcionCorrecta);
    }

    [Fact]
    public async Task No_vencida_no_guarda_ni_emite()
    {
        var repo = new FakeSesionPartidaRepository();
        repo.Add(TriviaIniciada(30));
        var uow = new FakeOperacionesSesionUnitOfWork();
        var events = new FakeSesionEventsPublisher();
        var handler = Build(repo, uow, events, T0.AddSeconds(10)); // dentro de ventana

        var n = await handler.Handle(new BarrerTimeoutsCommand(), CancellationToken.None);

        Assert.Equal(0, n);
        Assert.Equal(0, uow.SaveCount);
        Assert.Empty(events.PreguntasCerradas);
    }

    [Fact]
    public async Task Conflicto_en_un_candidato_no_aborta_el_resto()
    {
        var repo = new FakeSesionPartidaRepository();
        repo.Add(TriviaIniciada(30));
        repo.Add(TriviaIniciada(30));
        // Prueba la intención loop-continue del handler contra fakes. La semántica real del
        // change-tracker de EF (entidad fallida queda Modified → re-batch) NO se reproduce aquí:
        // resiliencia efectiva = por-tick, auto-sana al próximo tick (ver caveat I1, design §4.2).
        var uow = new ThrowOnceUnitOfWork(); // 1er SaveChanges lanza DbUpdateConcurrencyException
        var events = new FakeSesionEventsPublisher();
        var handler = Build(repo, uow, events, T0.AddSeconds(31));

        var n = await handler.Handle(new BarrerTimeoutsCommand(), CancellationToken.None);

        Assert.Equal(1, n); // el segundo candidato sí avanzó
        Assert.Single(events.PreguntasCerradas);   // el candidato en conflicto NO emitió (save lanzó antes de publicar)
        Assert.Single(events.PreguntasActivadas);
    }

    private sealed class ThrowOnceUnitOfWork : IOperacionesSesionUnitOfWork
    {
        private bool _thrown;
        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            if (!_thrown) { _thrown = true; throw new DbUpdateConcurrencyException("conflict"); }
            return Task.CompletedTask;
        }
    }
}
