using Moq;
using Umbral.TriviaGame.Application.Handlers;
using Umbral.TriviaGame.Application.Ports;
using Umbral.TriviaGame.Application.Queries;
using Umbral.TriviaGame.Domain.Entities;
using Umbral.TriviaGame.Domain.Enums;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Application.Tests.Handlers;

public class GetTriviaGameByIdQueryHandlerTests
{
    private readonly Mock<IPartidaTriviaRepository> _repoMock = new();
    private readonly GetTriviaGameByIdQueryHandler _handler;

    public GetTriviaGameByIdQueryHandlerTests()
    {
        _handler = new GetTriviaGameByIdQueryHandler(_repoMock.Object);
    }

    private static PartidaTrivia CreatePartida()
    {
        var min = CantidadMinima.Create(2);
        return PartidaTrivia.Create(
            NombrePartida.Create("Test Game"),
            Modalidad.Individual,
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
    public async Task Handle_PartidaExists_ReturnsDto()
    {
        var partida = CreatePartida();
        var partidaId = partida.Id.Value;

        _repoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(partida);

        var query = new GetTriviaGameByIdQuery(partidaId);
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(partidaId, result.Id);
        Assert.Equal("Test Game", result.Nombre);
        Assert.Equal("Lobby", result.Estado);
        Assert.Equal("Individual", result.Modalidad);
    }

    [Fact]
    public async Task Handle_PartidaNotFound_ReturnsNull()
    {
        var partidaId = Guid.NewGuid();

        _repoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PartidaTrivia?)null);

        var query = new GetTriviaGameByIdQuery(partidaId);
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Null(result);
    }
}
