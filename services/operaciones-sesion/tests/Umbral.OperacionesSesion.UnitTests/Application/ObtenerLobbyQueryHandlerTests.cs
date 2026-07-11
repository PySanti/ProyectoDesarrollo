using System;
using System.Threading;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.Exceptions;
using Umbral.OperacionesSesion.Application.Handlers.Queries;
using Umbral.OperacionesSesion.Application.Queries;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;

namespace Umbral.OperacionesSesion.UnitTests.Application;

public class ObtenerLobbyQueryHandlerTests
{
    private static readonly DateTime T0 = new(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc);

    private static SesionPartida PublishedSession(Guid partidaId)
    {
        var snapshot = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10,
            new[] { new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia) });
        return SesionPartida.Publicar(partidaId, snapshot);
    }

    private static SesionPartida PublishedEquipoSession(Guid partidaId)
    {
        var snapshot = new ConfiguracionSnapshot("Copa", Modalidad.Equipo, ModoInicioPartida.Manual, null, 1, 10,
            new[] { new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia) });
        return SesionPartida.Publicar(partidaId, snapshot);
    }

    [Fact]
    public async Task Returns_lobby_with_active_inscriptions()
    {
        var partidaId = Guid.NewGuid();
        var repo = new FakeSesionPartidaRepository();
        var sesion = PublishedSession(partidaId);
        var participante = Guid.NewGuid();
        var insc = sesion.Inscribir(participante, false, 0, DateTime.UtcNow);
        sesion.AceptarInscripcion(insc.Id.Valor, 0, DateTime.UtcNow); // HU-19: aceptar para inscripción activa
        repo.Add(sesion);
        var handler = new ObtenerLobbyQueryHandler(repo);

        var lobby = await handler.Handle(new ObtenerLobbyQuery(partidaId), CancellationToken.None);

        Assert.Equal("Lobby", lobby.Estado);
        Assert.Equal("Individual", lobby.Modalidad);
        Assert.Equal(1, lobby.InscritosActivos);
        Assert.Contains(participante, lobby.Participantes);
    }

    [Fact]
    public async Task Throws_when_session_not_found()
    {
        var handler = new ObtenerLobbyQueryHandler(new FakeSesionPartidaRepository());
        await Assert.ThrowsAsync<SesionNoEncontradaException>(
            () => handler.Handle(new ObtenerLobbyQuery(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Lobby_separa_activos_de_solicitudes_pendientes()
    {
        var partidaId = Guid.NewGuid();
        var sesion = PublishedSession(partidaId);
        var pAct = sesion.Inscribir(Guid.NewGuid(), false, 0, T0);
        sesion.AceptarInscripcion(pAct.Id.Valor, 0, T0);            // Activa
        var pPend = sesion.Inscribir(Guid.NewGuid(), false, 1, T0); // Pendiente
        var repo = new FakeSesionPartidaRepository();
        repo.Add(sesion);
        var handler = new ObtenerLobbyQueryHandler(repo);

        var lobby = await handler.Handle(new ObtenerLobbyQuery(partidaId), default);

        Assert.Equal(1, lobby.InscritosActivos);
        var pendiente = Assert.Single(lobby.SolicitudesPendientesIndividual);
        Assert.Equal(pPend.Id.Valor, pendiente.InscripcionId);
        Assert.Empty(lobby.SolicitudesPendientesEquipo);
    }

    [Fact]
    public async Task Lobby_expone_preinscripcion_de_equipo_como_solicitud_pendiente()
    {
        var partidaId = Guid.NewGuid();
        var sesion = PublishedEquipoSession(partidaId);
        var equipoId = Guid.NewGuid();
        var insc = sesion.PreinscribirEquipo(equipoId, true, new[] { Guid.NewGuid(), Guid.NewGuid() }, false, 0, T0);
        var repo = new FakeSesionPartidaRepository();
        repo.Add(sesion);
        var handler = new ObtenerLobbyQueryHandler(repo);

        var lobby = await handler.Handle(new ObtenerLobbyQuery(partidaId), default);

        Assert.Equal(0, lobby.InscritosActivos);
        var solicitud = Assert.Single(lobby.SolicitudesPendientesEquipo);
        Assert.Equal(insc.Id.Valor, solicitud.InscripcionId);
        Assert.Equal(equipoId, solicitud.EquipoId);
        Assert.Equal(2, solicitud.Miembros);
        Assert.Empty(lobby.Equipos); // aún no activa → no aparece como equipo del lobby
    }
}
