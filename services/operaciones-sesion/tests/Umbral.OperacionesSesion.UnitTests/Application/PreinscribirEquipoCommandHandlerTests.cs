using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.Commands;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Handlers.Commands;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.Exceptions;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class PreinscribirEquipoCommandHandlerTests
{
    private static readonly DateTime T0 = new(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc);

    private static SesionPartida PartidaEquipoEnLobby(Guid partidaId)
    {
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, new[]
        {
            new PreguntaSnapshot(Guid.NewGuid(), 1, "Q1", 10, 30,
                new[] { new OpcionSnapshot(Guid.NewGuid(), "ok", true) })
        });
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Equipo, ModoInicioPartida.Manual, null, 1, 5,
            new List<JuegoResumen> { juego });
        return SesionPartida.Publicar(partidaId, snap);
    }

    [Fact]
    public async Task Preinscribe_y_publica_una_convocatoria_creada_por_miembro()
    {
        var partidaId = Guid.NewGuid();
        var lider = Guid.NewGuid();
        var miembro = Guid.NewGuid();
        var equipoId = Guid.NewGuid();

        var repo = new FakeSesionPartidaRepository();
        repo.Add(PartidaEquipoEnLobby(partidaId));
        var directory = new FakeEquipoDirectoryClient
        {
            Equipo = new EquipoSnapshotDto(equipoId, "Halcones",
                new List<MiembroEquipoDto> { new(lider, true), new(miembro, false) })
        };
        var events = new FakeSesionEventsPublisher();
        var handler = new PreinscribirEquipoCommandHandler(
            repo, directory, events, new FakeOperacionesSesionUnitOfWork(), new FakeTimeProvider(T0));

        var resp = await handler.Handle(new PreinscribirEquipoCommand(partidaId, lider, "Bearer x"), default);

        Assert.Equal(equipoId, resp.EquipoId);
        Assert.Equal(2, resp.Convocados);
        Assert.Equal(2, events.ConvocatoriasCreadas.Count);
        Assert.All(events.ConvocatoriasCreadas, e => Assert.Equal(equipoId, e.EquipoId));
        Assert.Contains(events.ConvocatoriasCreadas, e => e.UsuarioId == miembro);
    }

    [Fact]
    public async Task Caller_no_es_lider_lanza_sin_publicar()
    {
        var partidaId = Guid.NewGuid();
        var lider = Guid.NewGuid();
        var otro = Guid.NewGuid();
        var repo = new FakeSesionPartidaRepository();
        repo.Add(PartidaEquipoEnLobby(partidaId));
        var directory = new FakeEquipoDirectoryClient
        {
            Equipo = new EquipoSnapshotDto(Guid.NewGuid(), "Halcones",
                new List<MiembroEquipoDto> { new(lider, true), new(otro, false) })
        };
        var events = new FakeSesionEventsPublisher();
        var handler = new PreinscribirEquipoCommandHandler(
            repo, directory, events, new FakeOperacionesSesionUnitOfWork(), new FakeTimeProvider(T0));

        // el caller es 'otro' (no líder según el snapshot)
        await Assert.ThrowsAsync<NoEsLiderEquipoException>(
            () => handler.Handle(new PreinscribirEquipoCommand(partidaId, otro, "Bearer x"), default));
        Assert.Empty(events.ConvocatoriasCreadas);
    }

    [Fact]
    public async Task Sin_equipo_activo_lanza()
    {
        var partidaId = Guid.NewGuid();
        var repo = new FakeSesionPartidaRepository();
        repo.Add(PartidaEquipoEnLobby(partidaId));
        var directory = new FakeEquipoDirectoryClient { Equipo = null };
        var handler = new PreinscribirEquipoCommandHandler(
            repo, directory, new FakeSesionEventsPublisher(), new FakeOperacionesSesionUnitOfWork(), new FakeTimeProvider(T0));

        await Assert.ThrowsAsync<SinEquipoActivoException>(
            () => handler.Handle(new PreinscribirEquipoCommand(partidaId, Guid.NewGuid(), "Bearer x"), default));
    }

    [Fact]
    public async Task Sesion_inexistente_lanza()
    {
        var repo = new FakeSesionPartidaRepository();
        var directory = new FakeEquipoDirectoryClient
        {
            Equipo = new EquipoSnapshotDto(Guid.NewGuid(), "H", new List<MiembroEquipoDto> { new(Guid.NewGuid(), true) })
        };
        var handler = new PreinscribirEquipoCommandHandler(
            repo, directory, new FakeSesionEventsPublisher(), new FakeOperacionesSesionUnitOfWork(), new FakeTimeProvider(T0));

        await Assert.ThrowsAsync<SesionNoEncontradaException>(
            () => handler.Handle(new PreinscribirEquipoCommand(Guid.NewGuid(), Guid.NewGuid(), "Bearer x"), default));
    }
}
