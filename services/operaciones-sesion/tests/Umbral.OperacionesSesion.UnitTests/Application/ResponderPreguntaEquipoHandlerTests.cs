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
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class ResponderPreguntaEquipoHandlerTests
{
    private static readonly DateTime T0 = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    private static SesionPartida SesionEquipoIniciada(out Guid liderA, out Guid equipoA, out Guid correcta)
    {
        var liderALocal = Guid.NewGuid();
        var equipoALocal = Guid.NewGuid();
        var ok = new OpcionSnapshot(Guid.NewGuid(), "ok", true);
        var no = new OpcionSnapshot(Guid.NewGuid(), "no", false);
        var pregunta = new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 60, new[] { ok, no });
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[] { pregunta });
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Equipo, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        var sesion = SesionPartida.Publicar(Guid.NewGuid(), snap);

        var ins = sesion.PreinscribirEquipo(equipoALocal, true, liderALocal, new[] { liderALocal }, false, 0, T0);
        sesion.AceptarInscripcion(ins.Id.Valor, 0, T0); // HU-19: aceptar crea las convocatorias
        sesion.ResponderConvocatoria(ins.Convocatorias.Single(c => c.UsuarioId == liderALocal).Id.Valor, liderALocal, true, false, T0);
        sesion.Iniciar(T0);

        liderA = liderALocal;
        equipoA = equipoALocal;
        correcta = ok.OpcionId;
        return sesion;
    }

    [Fact]
    public async Task En_equipo_los_eventos_portan_el_equipo()
    {
        var sesion = SesionEquipoIniciada(out var liderA, out var equipoA, out var correcta);
        var repo = new FakeSesionPartidaRepository();
        repo.Add(sesion);
        var uow = new FakeOperacionesSesionUnitOfWork();
        var events = new FakeSesionEventsPublisher();
        var handler = new ResponderPreguntaCommandHandler(repo, uow, events, new FakeTimeProvider(T0.AddSeconds(5)));

        await handler.Handle(new ResponderPreguntaCommand(sesion.PartidaId, liderA, correcta), CancellationToken.None);

        var validada = events.RespuestasValidadas.Single();
        Assert.Equal(equipoA, validada.EquipoId);
        var puntaje = events.PuntajesIncrementados.Single();
        Assert.Equal(equipoA, puntaje.EquipoId);
        var cerrada = events.PreguntasCerradas.Single();
        Assert.Equal(equipoA, cerrada.GanadorEquipoId);
        Assert.Equal(liderA, cerrada.GanadorParticipanteId);
    }

    [Fact]
    public async Task En_individual_los_eventos_llevan_equipo_null()
    {
        var partidaId = Guid.NewGuid();
        var ok = new OpcionSnapshot(Guid.NewGuid(), "ok", true);
        var no = new OpcionSnapshot(Guid.NewGuid(), "no", false);
        var pregunta = new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 60, new[] { ok, no });
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[] { pregunta });
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        var sesion = SesionPartida.Publicar(partidaId, snap);
        var part = Guid.NewGuid();
        var inscP = sesion.Inscribir(part, false, 0, T0);
        sesion.AceptarInscripcion(inscP.Id.Valor, 0, T0); // HU-19: aceptar para que cuente en mínimos
        sesion.Iniciar(T0);

        var repo = new FakeSesionPartidaRepository();
        repo.Add(sesion);
        var events = new FakeSesionEventsPublisher();
        var handler = new ResponderPreguntaCommandHandler(repo, new FakeOperacionesSesionUnitOfWork(), events, new FakeTimeProvider(T0.AddSeconds(4)));

        await handler.Handle(new ResponderPreguntaCommand(partidaId, part, ok.OpcionId), CancellationToken.None);

        Assert.Null(events.RespuestasValidadas.Single().EquipoId);
        Assert.Null(events.PreguntasCerradas.Single().GanadorEquipoId);
    }
}
