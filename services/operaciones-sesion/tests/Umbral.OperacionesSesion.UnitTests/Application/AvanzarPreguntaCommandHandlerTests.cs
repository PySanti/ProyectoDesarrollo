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

public class AvanzarPreguntaCommandHandlerTests
{
    private static readonly DateTime T0 = new(2026, 6, 27, 12, 0, 0, DateTimeKind.Utc);

    private static PreguntaSnapshot P(int orden) =>
        new(Guid.NewGuid(), orden, $"Q{orden}", 10, 30,
            new[] { new OpcionSnapshot(Guid.NewGuid(), "ok", true), new OpcionSnapshot(Guid.NewGuid(), "no", false) });

    private static SesionPartida Iniciada(Guid partidaId, params PreguntaSnapshot[] preguntas)
    {
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, preguntas);
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        var sesion = SesionPartida.Publicar(partidaId, snap);
        var insc = sesion.Inscribir(Guid.NewGuid(), false, 0, T0);
        sesion.AceptarInscripcion(insc.Id.Valor, 0, T0); // HU-19: aceptar para que cuente en mínimos
        sesion.Iniciar(T0);
        return sesion;
    }

    [Fact]
    public async Task Advance_to_next_publishes_cerrada_and_activada()
    {
        var partidaId = Guid.NewGuid();
        var repo = new FakeSesionPartidaRepository();
        repo.Add(Iniciada(partidaId, P(1), P(2)));
        var events = new FakeSesionEventsPublisher();
        var uow = new FakeOperacionesSesionUnitOfWork();
        var handler = new AvanzarPreguntaCommandHandler(repo, uow, events, new FakeTimeProvider(T0.AddSeconds(5)));

        var resp = await handler.Handle(new AvanzarPreguntaCommand(partidaId), CancellationToken.None);

        Assert.Equal(1, resp.PreguntaCerradaOrden);
        Assert.Equal(2, resp.PreguntaActivadaOrden);
        Assert.False(resp.SinMasPreguntas);
        Assert.Equal(1, uow.SaveCount);
        Assert.Single(events.PreguntasCerradas);
        Assert.Single(events.PreguntasActivadas);
    }

    [Fact]
    public async Task Advance_on_last_publishes_only_cerrada()
    {
        var partidaId = Guid.NewGuid();
        var repo = new FakeSesionPartidaRepository();
        repo.Add(Iniciada(partidaId, P(1)));
        var events = new FakeSesionEventsPublisher();
        var handler = new AvanzarPreguntaCommandHandler(repo, new FakeOperacionesSesionUnitOfWork(), events, new FakeTimeProvider(T0.AddSeconds(5)));

        var resp = await handler.Handle(new AvanzarPreguntaCommand(partidaId), CancellationToken.None);

        Assert.True(resp.SinMasPreguntas);
        Assert.Null(resp.PreguntaActivadaOrden);
        Assert.Single(events.PreguntasCerradas);
        Assert.Empty(events.PreguntasActivadas);
    }
}
