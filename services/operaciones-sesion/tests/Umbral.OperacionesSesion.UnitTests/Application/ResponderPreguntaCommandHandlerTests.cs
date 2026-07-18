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

public class ResponderPreguntaCommandHandlerTests
{
    private static readonly DateTime T0 = new(2026, 6, 27, 12, 0, 0, DateTimeKind.Utc);

    private static (SesionPartida sesion, Guid participante, Guid correcta) Iniciada(Guid partidaId)
    {
        var ok = new OpcionSnapshot(Guid.NewGuid(), "ok", true);
        var no = new OpcionSnapshot(Guid.NewGuid(), "no", false);
        var pregunta = new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 30, new[] { ok, no });
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[] { pregunta });
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        var sesion = SesionPartida.Publicar(partidaId, snap);
        var part = Guid.NewGuid();
        var insc = sesion.Inscribir(part, false, 0, T0);
        sesion.AceptarInscripcion(insc.Id.Valor, 0, T0); // HU-19: aceptar para que cuente en mínimos
        sesion.Iniciar(T0);
        return (sesion, part, ok.OpcionId);
    }

    // 2 participantes individuales: permite probar el cierre "todos respondieron mal" (una sola
    // respuesta incorrecta NO cierra; hacen falta ambas).
    private static (SesionPartida sesion, Guid pa, Guid pb, Guid correcta) IniciadaConDos(Guid partidaId)
    {
        var ok = new OpcionSnapshot(Guid.NewGuid(), "ok", true);
        var no = new OpcionSnapshot(Guid.NewGuid(), "no", false);
        var pregunta = new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 30, new[] { ok, no });
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[] { pregunta });
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        var sesion = SesionPartida.Publicar(partidaId, snap);
        var pa = Guid.NewGuid();
        var pb = Guid.NewGuid();
        var ia = sesion.Inscribir(pa, false, 0, T0); sesion.AceptarInscripcion(ia.Id.Valor, 0, T0);
        var ib = sesion.Inscribir(pb, false, 1, T0); sesion.AceptarInscripcion(ib.Id.Valor, 1, T0);
        sesion.Iniciar(T0);
        return (sesion, pa, pb, ok.OpcionId);
    }

    [Fact]
    public async Task Correct_answer_saves_and_publishes_three_events()
    {
        var partidaId = Guid.NewGuid();
        var (sesion, part, correcta) = Iniciada(partidaId);
        var repo = new FakeSesionPartidaRepository();
        repo.Add(sesion);
        var uow = new FakeOperacionesSesionUnitOfWork();
        var events = new FakeSesionEventsPublisher();
        var handler = new ResponderPreguntaCommandHandler(repo, uow, events, new FakeTimeProvider(T0.AddSeconds(4)));

        var resp = await handler.Handle(new ResponderPreguntaCommand(partidaId, part, correcta), CancellationToken.None);

        Assert.True(resp.EsCorrecta);
        Assert.True(resp.CerroPregunta);
        Assert.Equal(10, resp.Puntaje);
        Assert.Equal(1, uow.SaveCount);
        Assert.Single(events.RespuestasValidadas);
        Assert.Single(events.PuntajesIncrementados);
        Assert.Single(events.PreguntasCerradas);
    }

    [Fact]
    public async Task Correct_answer_publishes_texto_opcion_correcta_en_cierre()
    {
        var partidaId = Guid.NewGuid();
        var (sesion, part, correcta) = Iniciada(partidaId);
        var repo = new FakeSesionPartidaRepository();
        repo.Add(sesion);
        var events = new FakeSesionEventsPublisher();
        var handler = new ResponderPreguntaCommandHandler(repo, new FakeOperacionesSesionUnitOfWork(), events, new FakeTimeProvider(T0.AddSeconds(4)));

        await handler.Handle(new ResponderPreguntaCommand(partidaId, part, correcta), CancellationToken.None);

        var cerrada = Assert.Single(events.PreguntasCerradas);
        Assert.Equal(correcta, cerrada.OpcionCorrectaId);
        Assert.Equal("ok", cerrada.TextoOpcionCorrecta);
    }

    [Fact]
    public async Task Correct_answer_on_last_question_publishes_partida_finalizada()
    {
        var partidaId = Guid.NewGuid();
        var (sesion, part, correcta) = Iniciada(partidaId); // 1 juego, 1 pregunta
        var repo = new FakeSesionPartidaRepository();
        repo.Add(sesion);
        var events = new FakeSesionEventsPublisher();
        var handler = new ResponderPreguntaCommandHandler(repo, new FakeOperacionesSesionUnitOfWork(), events, new FakeTimeProvider(T0.AddSeconds(4)));

        await handler.Handle(new ResponderPreguntaCommand(partidaId, part, correcta), CancellationToken.None);

        // Acertar la última pregunta finaliza el juego y termina la partida.
        Assert.Single(events.PartidasFinalizadas);
        Assert.Empty(events.JuegosActivados);
    }

    [Fact]
    public async Task Wrong_answer_publishes_only_validada()
    {
        var partidaId = Guid.NewGuid();
        // 2 participantes: que solo uno falle NO cierra la pregunta (falta el otro).
        var (sesion, pa, _, correcta) = IniciadaConDos(partidaId);
        var incorrecta = sesion.Juegos.Single().Preguntas.Single().Opciones.First(o => o.OpcionId != correcta).OpcionId;
        var repo = new FakeSesionPartidaRepository();
        repo.Add(sesion);
        var events = new FakeSesionEventsPublisher();
        var handler = new ResponderPreguntaCommandHandler(repo, new FakeOperacionesSesionUnitOfWork(), events, new FakeTimeProvider(T0.AddSeconds(2)));

        var resp = await handler.Handle(new ResponderPreguntaCommand(partidaId, pa, incorrecta), CancellationToken.None);

        Assert.False(resp.EsCorrecta);
        Assert.False(resp.CerroPregunta);
        Assert.Single(events.RespuestasValidadas);
        Assert.Empty(events.PuntajesIncrementados);
        Assert.Empty(events.PreguntasCerradas);
    }

    [Fact]
    public async Task Todos_fallan_cierra_con_motivo_TodosRespondieron_y_sin_puntaje()
    {
        var partidaId = Guid.NewGuid();
        var (sesion, pa, pb, correcta) = IniciadaConDos(partidaId);
        var incorrecta = sesion.Juegos.Single().Preguntas.Single().Opciones.First(o => o.OpcionId != correcta).OpcionId;
        var repo = new FakeSesionPartidaRepository();
        repo.Add(sesion);
        var events = new FakeSesionEventsPublisher();
        var handler = new ResponderPreguntaCommandHandler(repo, new FakeOperacionesSesionUnitOfWork(), events, new FakeTimeProvider(T0.AddSeconds(2)));

        await handler.Handle(new ResponderPreguntaCommand(partidaId, pa, incorrecta), CancellationToken.None); // 1 de 2
        var respB = await handler.Handle(new ResponderPreguntaCommand(partidaId, pb, incorrecta), CancellationToken.None); // cierra

        Assert.False(respB.EsCorrecta);
        Assert.True(respB.CerroPregunta);
        var cerrada = Assert.Single(events.PreguntasCerradas);
        Assert.Equal("TodosRespondieron", cerrada.Motivo);
        Assert.Null(cerrada.GanadorParticipanteId);
        Assert.Null(cerrada.GanadorEquipoId);
        Assert.Equal(correcta, cerrada.OpcionCorrectaId); // revela la correcta
        Assert.Equal("ok", cerrada.TextoOpcionCorrecta);
        Assert.Empty(events.PuntajesIncrementados); // nadie acertó
    }

    [Fact]
    public async Task Correct_answer_with_following_question_also_publishes_pregunta_activada()
    {
        var partidaId = Guid.NewGuid();
        var ok = new OpcionSnapshot(Guid.NewGuid(), "ok", true);
        var no = new OpcionSnapshot(Guid.NewGuid(), "no", false);
        var p1 = new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 30, new[] { ok, no });
        var p2 = new PreguntaSnapshot(Guid.NewGuid(), 2, "Q2", 10, 30,
            new[] { new OpcionSnapshot(Guid.NewGuid(), "a", true), new OpcionSnapshot(Guid.NewGuid(), "b", false) });
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[] { p1, p2 });
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        var sesion = SesionPartida.Publicar(partidaId, snap);
        var part = Guid.NewGuid();
        var insc = sesion.Inscribir(part, false, 0, T0);
        sesion.AceptarInscripcion(insc.Id.Valor, 0, T0); // HU-19: aceptar para que cuente en mínimos
        sesion.Iniciar(T0);

        var repo = new FakeSesionPartidaRepository();
        repo.Add(sesion);
        var events = new FakeSesionEventsPublisher();
        var handler = new ResponderPreguntaCommandHandler(repo, new FakeOperacionesSesionUnitOfWork(), events, new FakeTimeProvider(T0.AddSeconds(4)));

        await handler.Handle(new ResponderPreguntaCommand(partidaId, part, ok.OpcionId), CancellationToken.None);

        Assert.Single(events.RespuestasValidadas);
        Assert.Single(events.PuntajesIncrementados);
        Assert.Single(events.PreguntasCerradas);
        Assert.Single(events.PreguntasActivadas);
        Assert.Equal(2, events.PreguntasActivadas[0].Orden);
    }
}
