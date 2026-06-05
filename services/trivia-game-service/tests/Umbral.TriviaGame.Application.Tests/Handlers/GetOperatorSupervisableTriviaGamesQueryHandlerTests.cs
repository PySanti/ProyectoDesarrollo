using Moq;
using Umbral.TriviaGame.Application.Handlers;
using Umbral.TriviaGame.Application.Ports;
using Umbral.TriviaGame.Application.Queries;
using Umbral.TriviaGame.Domain.Entities;
using Umbral.TriviaGame.Domain.Enums;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Application.Tests.Handlers;

public sealed class GetOperatorSupervisableTriviaGamesQueryHandlerTests
{
    private readonly Mock<IPartidaTriviaRepository> _repoMock = new();
    private readonly GetOperatorSupervisableTriviaGamesQueryHandler _handler;

    public GetOperatorSupervisableTriviaGamesQueryHandlerTests()
    {
        _handler = new GetOperatorSupervisableTriviaGamesQueryHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsRepositorySupervisableGames()
    {
        var lobby = CreatePartida("Trivia Lobby");
        var iniciada = CreatePartida("Trivia Iniciada");
        iniciada.Iniciar(1, esInicioManual: false, QuestionId.New());

        _repoMock
            .Setup(r => r.GetSupervisableForOperatorAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PartidaTrivia> { lobby, iniciada });

        var result = await _handler.Handle(new GetOperatorSupervisableTriviaGamesQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, game => game.Estado == "Lobby");
        Assert.Contains(result, game => game.Estado == "Iniciada");
    }

    [Fact]
    public async Task Handle_NoSupervisableGames_ReturnsEmptyList()
    {
        _repoMock
            .Setup(r => r.GetSupervisableForOperatorAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PartidaTrivia>());

        var result = await _handler.Handle(new GetOperatorSupervisableTriviaGamesQuery(), CancellationToken.None);

        Assert.Empty(result);
    }

    private static PartidaTrivia CreatePartida(string nombre)
    {
        var minimo = CantidadMinima.Create(1);

        return PartidaTrivia.Create(
            NombrePartida.Create(nombre),
            Modalidad.Individual,
            ModoInicio.Manual,
            TriviaFormId.New(),
            OperatorId.Create("op-1"),
            TiempoInicio.Create(DateTimeOffset.UtcNow.AddHours(1)),
            minimo,
            CantidadMaximaJugadores.Create(10, minimo),
            maximoEquipos: null,
            minJugadoresPorEquipo: null,
            maxJugadoresPorEquipo: null);
    }
}
