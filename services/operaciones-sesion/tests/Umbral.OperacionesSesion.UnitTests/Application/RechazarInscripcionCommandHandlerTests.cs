using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.Handlers.Commands;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class RechazarInscripcionCommandHandlerTests
{
    private static readonly DateTime T0 = new(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Rechaza_equipo_publica_InscripcionRechazada_y_InscripcionEquipoCancelada()
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
        var equipoId = Guid.NewGuid();
        var insc = sesion.PreinscribirEquipo(equipoId, true, Guid.NewGuid(), new[] { Guid.NewGuid() }, false, 0, T0);
        var repo = new FakeSesionPartidaRepository();
        repo.Add(sesion);
        var events = new FakeSesionEventsPublisher();
        var handler = new RechazarInscripcionCommandHandler(
            repo, events, new FakeOperacionesSesionUnitOfWork(), new FakeTimeProvider(T0));

        await handler.Handle(new RechazarInscripcionCommand(partidaId, insc.Id.Valor), default);

        Assert.Equal(EstadoInscripcion.Rechazada, insc.Estado);
        var rech = Assert.Single(events.InscripcionesRechazadas);
        Assert.Equal(equipoId, rech.Evento.EquipoId);
        // En Equipo el push va al snapshot de miembros: el lider no es identificable.
        Assert.Equal(insc.MiembrosSnapshot, rech.Destinatarios);
        var cancel = Assert.Single(events.InscripcionesEquipoCanceladas);
        Assert.Equal(equipoId, cancel.EquipoId);
    }

    [Fact]
    public async Task Rechaza_individual_no_publica_InscripcionEquipoCancelada()
    {
        var partidaId = Guid.NewGuid();
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[]
        {
            new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 30,
                new[] { new OpcionSnapshot(Guid.NewGuid(), "ok", true) })
        });
        var snap = new ConfiguracionSnapshot("P", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 5,
            new List<JuegoResumen> { juego });
        var sesion = SesionPartida.Publicar(partidaId, snap);
        var insc = sesion.Inscribir(Guid.NewGuid(), false, 0, T0);
        var repo = new FakeSesionPartidaRepository();
        repo.Add(sesion);
        var events = new FakeSesionEventsPublisher();
        var handler = new RechazarInscripcionCommandHandler(
            repo, events, new FakeOperacionesSesionUnitOfWork(), new FakeTimeProvider(T0));

        await handler.Handle(new RechazarInscripcionCommand(partidaId, insc.Id.Valor), default);

        Assert.Single(events.InscripcionesRechazadas);
        Assert.Empty(events.InscripcionesEquipoCanceladas);
    }
}
