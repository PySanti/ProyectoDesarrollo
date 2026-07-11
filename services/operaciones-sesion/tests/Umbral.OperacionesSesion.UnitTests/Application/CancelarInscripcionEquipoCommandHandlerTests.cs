using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Handlers.Commands;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.Exceptions;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class CancelarInscripcionEquipoCommandHandlerTests
{
    private static readonly DateTime T0 = new(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Lider_cancela_inscripcion_de_equipo()
    {
        var partidaId = Guid.NewGuid();
        var lider = Guid.NewGuid();
        var equipoId = Guid.NewGuid();
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[]
        {
            new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 30,
                new[] { new OpcionSnapshot(Guid.NewGuid(), "ok", true) })
        });
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Equipo, ModoInicioPartida.Manual, null, 1, 5,
            new List<JuegoResumen> { juego });
        var sesion = SesionPartida.Publicar(partidaId, snap);
        var inscPre = sesion.PreinscribirEquipo(equipoId, true, new[] { lider }, false, 0, T0);
        sesion.AceptarInscripcion(inscPre.Id.Valor, 0, T0); // HU-19: aceptar para inscripción activa
        var repo = new FakeSesionPartidaRepository();
        repo.Add(sesion);
        var directory = new FakeEquipoDirectoryClient
        {
            Equipo = new EquipoSnapshotDto(equipoId, "H", new List<MiembroEquipoDto> { new(lider, true) })
        };
        var inscripcionId = sesion.Inscripciones.Single(i => i.EquipoId == equipoId).Id.Valor;
        var events = new FakeSesionEventsPublisher();
        var handler = new CancelarInscripcionEquipoCommandHandler(
            repo, directory, events, new FakeOperacionesSesionUnitOfWork(), new FakeTimeProvider(T0));

        await handler.Handle(new CancelarInscripcionEquipoCommand(partidaId, lider, "Bearer x"), default);

        Assert.DoesNotContain(sesion.Inscripciones, i => i.EsActiva);

        var inscripcionCancelada = Assert.Single(events.InscripcionesEquipoCanceladas);
        Assert.Equal(partidaId, inscripcionCancelada.PartidaId);
        Assert.Equal(equipoId, inscripcionCancelada.EquipoId);
        Assert.Equal(inscripcionId, inscripcionCancelada.InscripcionId);
        Assert.Equal(T0, inscripcionCancelada.Instante);
    }
}
