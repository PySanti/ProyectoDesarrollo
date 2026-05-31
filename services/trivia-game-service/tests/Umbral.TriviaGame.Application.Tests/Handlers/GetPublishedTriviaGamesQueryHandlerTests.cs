using Moq;
using Umbral.TriviaGame.Application.Handlers;
using Umbral.TriviaGame.Application.Ports;
using Umbral.TriviaGame.Application.Queries;
using Umbral.TriviaGame.Domain.Entities;
using Umbral.TriviaGame.Domain.Enums;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Application.Tests.Handlers;

public class GetPublishedTriviaGamesQueryHandlerTests
{
    private readonly Mock<IPartidaTriviaRepository> _repoMock = new();
    private readonly GetPublishedTriviaGamesQueryHandler _handler;

    public GetPublishedTriviaGamesQueryHandlerTests()
    {
        _handler = new GetPublishedTriviaGamesQueryHandler(_repoMock.Object);
    }

    private static PartidaTrivia CreatePartida(PartidaEstado estado, Modalidad modalidad = Modalidad.Individual)
    {
        var min = CantidadMinima.Create(2);
        if (modalidad == Modalidad.Equipo)
        {
            var equipoMin = JugadoresPorEquipoMin.Create(1);
            return PartidaTrivia.Create(
                NombrePartida.Create("Test Game"),
                modalidad,
                ModoInicio.Manual,
                TriviaFormId.New(),
                OperatorId.Create("op-1"),
                TiempoInicio.Create(DateTimeOffset.UtcNow.AddDays(1)),
                min,
                maximoJugadores: null,
                maximoEquipos: CantidadMaximaEquipos.Create(10, min),
                minJugadoresPorEquipo: equipoMin,
                maxJugadoresPorEquipo: JugadoresPorEquipoMax.Create(3, equipoMin));
        }

        return PartidaTrivia.Create(
            NombrePartida.Create("Test Game"),
            modalidad,
            ModoInicio.Manual,
            TriviaFormId.New(),
            OperatorId.Create("op-1"),
            TiempoInicio.Create(DateTimeOffset.UtcNow.AddDays(1)),
            min,
            maximoJugadores: CantidadMaximaJugadores.Create(10, min),
            maximoEquipos: null,
            minJugadoresPorEquipo: null,
            maxJugadoresPorEquipo: null);
    }

    [Fact]
    public async Task Handle_ReturnsOnlyLobbyGames()
    {
        var lobbyGame = CreatePartida(PartidaEstado.Lobby);

        _repoMock
            .Setup(r => r.GetPublishedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PartidaTrivia> { lobbyGame });

        var query = new GetPublishedTriviaGamesQuery();
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("Lobby", result[0].Estado);
    }

    [Fact]
    public async Task Handle_NoPublishedGames_ReturnsEmptyList()
    {
        _repoMock
            .Setup(r => r.GetPublishedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PartidaTrivia>());

        var query = new GetPublishedTriviaGamesQuery();
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_FilterIndividual_ReturnsOnlyIndividualGames()
    {
        var individual = CreatePartida(PartidaEstado.Lobby, Modalidad.Individual);
        var equipo = CreatePartida(PartidaEstado.Lobby, Modalidad.Equipo);

        _repoMock
            .Setup(r => r.GetPublishedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PartidaTrivia> { individual, equipo });

        var query = new GetPublishedTriviaGamesQuery("Individual");
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Single(result);
        Assert.All(result, g => Assert.Equal("Individual", g.Modalidad));
    }

    [Fact]
    public async Task Handle_FilterEquipo_ReturnsOnlyEquipoGames()
    {
        var individual = CreatePartida(PartidaEstado.Lobby, Modalidad.Individual);
        var equipo = CreatePartida(PartidaEstado.Lobby, Modalidad.Equipo);

        _repoMock
            .Setup(r => r.GetPublishedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PartidaTrivia> { individual, equipo });

        var query = new GetPublishedTriviaGamesQuery("Equipo");
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Single(result);
        Assert.All(result, g => Assert.Equal("Equipo", g.Modalidad));
    }

    [Fact]
    public async Task Handle_NoFilter_ReturnsAllGames()
    {
        var individual = CreatePartida(PartidaEstado.Lobby, Modalidad.Individual);
        var equipo = CreatePartida(PartidaEstado.Lobby, Modalidad.Equipo);

        _repoMock
            .Setup(r => r.GetPublishedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PartidaTrivia> { individual, equipo });

        var query = new GetPublishedTriviaGamesQuery();
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task Handle_FilterNoMatches_ReturnsEmptyList()
    {
        var individual = CreatePartida(PartidaEstado.Lobby, Modalidad.Individual);

        _repoMock
            .Setup(r => r.GetPublishedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PartidaTrivia> { individual });

        var query = new GetPublishedTriviaGamesQuery("Equipo");
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_InvalidModalidad_ThrowsArgumentOutOfRangeException()
    {
        _repoMock
            .Setup(r => r.GetPublishedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PartidaTrivia>());

        var query = new GetPublishedTriviaGamesQuery("Invalido");

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_FilterIndividualCaseInsensitive_ReturnsIndividualGame()
    {
        var individual = CreatePartida(PartidaEstado.Lobby, Modalidad.Individual);
        var equipo = CreatePartida(PartidaEstado.Lobby, Modalidad.Equipo);

        _repoMock
            .Setup(r => r.GetPublishedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PartidaTrivia> { individual, equipo });

        var query = new GetPublishedTriviaGamesQuery("individual");
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Single(result);
        Assert.All(result, g => Assert.Equal("Individual", g.Modalidad));
    }
}
