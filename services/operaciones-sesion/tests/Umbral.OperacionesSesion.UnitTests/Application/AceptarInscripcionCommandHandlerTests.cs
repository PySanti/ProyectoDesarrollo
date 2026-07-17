using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Handlers.Commands;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.Exceptions;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class AceptarInscripcionCommandHandlerTests
{
    private static readonly DateTime T0 = new(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc);

    private static SesionPartida IndividualEnLobby(Guid partidaId)
    {
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[]
        {
            new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 30,
                new[] { new OpcionSnapshot(Guid.NewGuid(), "ok", true) })
        });
        var snap = new ConfiguracionSnapshot("P", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 5,
            new List<JuegoResumen> { juego });
        return SesionPartida.Publicar(partidaId, snap);
    }

    [Fact]
    public async Task Acepta_pendiente_y_publica_InscripcionAceptada()
    {
        var partidaId = Guid.NewGuid();
        var sesion = IndividualEnLobby(partidaId);
        var insc = sesion.Inscribir(Guid.NewGuid(), false, 0, T0); // Pendiente
        var repo = new FakeSesionPartidaRepository();
        repo.Add(sesion);
        var events = new FakeSesionEventsPublisher();
        var handler = new AceptarInscripcionCommandHandler(
            repo, events, new FakeOperacionesSesionUnitOfWork(), new FakeTimeProvider(T0));

        await handler.Handle(new AceptarInscripcionCommand(partidaId, insc.Id.Valor), default);

        Assert.True(insc.EsActiva);
        var e = Assert.Single(events.InscripcionesAceptadas);
        Assert.Equal(insc.Id.Valor, e.Evento.InscripcionId);
        Assert.Equal("Individual", e.Evento.Modalidad);
        // En Individual el destinatario del push es el propio solicitante.
        Assert.Equal(new[] { insc.ParticipanteId }, e.Destinatarios);
        Assert.Empty(events.ConvocatoriasCreadas); // individual no convoca
    }

    [Fact]
    public async Task Acepta_equipo_publica_una_ConvocatoriaCreada_por_miembro()
    {
        var partidaId = Guid.NewGuid();
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[]
        {
            new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 30,
                new[] { new OpcionSnapshot(Guid.NewGuid(), "ok", true) })
        });
        var snap = new ConfiguracionSnapshot("P", Modalidad.Equipo, ModoInicioPartida.Manual, null, 1, 5,
            new List<JuegoResumen> { juego });
        var sesion = SesionPartida.Publicar(partidaId, snap);
        var m1 = Guid.NewGuid();
        var m2 = Guid.NewGuid();
        var insc = sesion.PreinscribirEquipo(Guid.NewGuid(), true, m1, new[] { m1, m2 }, false, 0, T0);
        var repo = new FakeSesionPartidaRepository();
        repo.Add(sesion);
        var events = new FakeSesionEventsPublisher();
        var handler = new AceptarInscripcionCommandHandler(
            repo, events, new FakeOperacionesSesionUnitOfWork(), new FakeTimeProvider(T0));

        await handler.Handle(new AceptarInscripcionCommand(partidaId, insc.Id.Valor), default);

        Assert.Equal(2, events.ConvocatoriasCreadas.Count);
        Assert.Contains(events.ConvocatoriasCreadas, c => c.UsuarioId == m1);
        Assert.Single(events.InscripcionesAceptadas);
    }

    [Fact]
    public async Task Acepta_equipo_autoacepta_al_lider_y_anuncia_la_respuesta()
    {
        var partidaId = Guid.NewGuid();
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[]
        {
            new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 30,
                new[] { new OpcionSnapshot(Guid.NewGuid(), "ok", true) })
        });
        var snap = new ConfiguracionSnapshot("P", Modalidad.Equipo, ModoInicioPartida.Manual, null, 1, 5,
            new List<JuegoResumen> { juego });
        var sesion = SesionPartida.Publicar(partidaId, snap);
        var lider = Guid.NewGuid();
        var miembro = Guid.NewGuid();
        var insc = sesion.PreinscribirEquipo(Guid.NewGuid(), true, lider, new[] { lider, miembro }, false, 0, T0);
        var repo = new FakeSesionPartidaRepository { ParticipacionActivaEnOtra = false };
        repo.Add(sesion);
        var events = new FakeSesionEventsPublisher();
        var handler = new AceptarInscripcionCommandHandler(
            repo, events, new FakeOperacionesSesionUnitOfWork(), new FakeTimeProvider(T0));

        await handler.Handle(new AceptarInscripcionCommand(partidaId, insc.Id.Valor), default);

        Assert.True(insc.Convocatorias.Single(c => c.UsuarioId == lider).EstaAceptada);
        Assert.True(insc.Convocatorias.Single(c => c.UsuarioId == miembro).EstaPendiente);

        // Solo la del lider se anuncia como respondida: la del miembro sigue pendiente.
        var respondida = Assert.Single(events.ConvocatoriasRespondidas);
        Assert.Equal(lider, respondida.UsuarioId);
    }

    // BR-G09: si el lider ya participa en otra partida, el handler no le concede el auto-aceptado.
    [Fact]
    public async Task Acepta_equipo_no_autoacepta_al_lider_con_participacion_activa_en_otra()
    {
        var partidaId = Guid.NewGuid();
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[]
        {
            new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 30,
                new[] { new OpcionSnapshot(Guid.NewGuid(), "ok", true) })
        });
        var snap = new ConfiguracionSnapshot("P", Modalidad.Equipo, ModoInicioPartida.Manual, null, 1, 5,
            new List<JuegoResumen> { juego });
        var sesion = SesionPartida.Publicar(partidaId, snap);
        var lider = Guid.NewGuid();
        var insc = sesion.PreinscribirEquipo(Guid.NewGuid(), true, lider, new[] { lider }, false, 0, T0);
        var repo = new FakeSesionPartidaRepository { ParticipacionActivaEnOtra = true };
        repo.Add(sesion);
        var events = new FakeSesionEventsPublisher();
        var handler = new AceptarInscripcionCommandHandler(
            repo, events, new FakeOperacionesSesionUnitOfWork(), new FakeTimeProvider(T0));

        await handler.Handle(new AceptarInscripcionCommand(partidaId, insc.Id.Valor), default);

        Assert.True(insc.Convocatorias.Single(c => c.UsuarioId == lider).EstaPendiente);
        Assert.Empty(events.ConvocatoriasRespondidas);
    }

    [Fact]
    public async Task Sesion_inexistente_lanza()
    {
        var repo = new FakeSesionPartidaRepository();
        var handler = new AceptarInscripcionCommandHandler(
            repo, new FakeSesionEventsPublisher(), new FakeOperacionesSesionUnitOfWork(), new FakeTimeProvider(T0));

        await Assert.ThrowsAsync<SesionNoEncontradaException>(
            () => handler.Handle(new AceptarInscripcionCommand(Guid.NewGuid(), Guid.NewGuid()), default));
    }
}
