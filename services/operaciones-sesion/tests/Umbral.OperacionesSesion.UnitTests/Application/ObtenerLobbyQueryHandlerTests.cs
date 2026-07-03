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
    private static SesionPartida PublishedSession(Guid partidaId)
    {
        var snapshot = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 10,
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
        sesion.Inscribir(participante, false, 0, DateTime.UtcNow);
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
}
