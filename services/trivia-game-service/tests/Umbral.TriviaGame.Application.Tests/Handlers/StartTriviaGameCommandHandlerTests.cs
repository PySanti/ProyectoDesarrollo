using Moq;
using Umbral.TriviaGame.Application.Commands;
using Umbral.TriviaGame.Application.Handlers;
using Umbral.TriviaGame.Application.Ports;
using Umbral.TriviaGame.Domain.Common;
using Umbral.TriviaGame.Domain.Common.Exceptions;
using Umbral.TriviaGame.Domain.Entities;
using Umbral.TriviaGame.Domain.Enums;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Application.Tests.Handlers;

public class StartTriviaGameCommandHandlerTests
{
    private readonly Mock<IPartidaTriviaRepository> _repoMock = new();
    private readonly Mock<IDomainEventDispatcher> _eventMock = new();
    private readonly StartTriviaGameCommandHandler _handler;

    public StartTriviaGameCommandHandlerTests()
    {
        _handler = new StartTriviaGameCommandHandler(_repoMock.Object, _eventMock.Object);
    }

    private static PartidaTrivia CreatePartidaEnLobby(CantidadMinima? minimo = null)
    {
        var min = minimo ?? CantidadMinima.Create(2);
        return PartidaTrivia.Create(
            NombrePartida.Create("Test"),
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
    public async Task Handle_PartidaExistsAndMinimosCumplidos_ReturnsDtoConEstadoIniciada()
    {
        var partida = CreatePartidaEnLobby();
        var partidaId = partida.Id.Value;

        _repoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(partida);
        _repoMock
            .Setup(r => r.CountInscripcionesAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var cmd = new StartTriviaGameCommand(partidaId);
        var result = await _handler.Handle(cmd, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Iniciada", result.Estado);
        Assert.NotNull(result.StartedAtUtc);

        _repoMock.Verify(r => r.UpdateAsync(partida, It.IsAny<CancellationToken>()), Times.Once);
        _eventMock.Verify(e => e.DispatchAsync(It.IsAny<IReadOnlyList<DomainEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PartidaNotFound_ThrowsPartidaTriviaNotFoundException()
    {
        _repoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PartidaTrivia?)null);

        var cmd = new StartTriviaGameCommand(Guid.NewGuid());

        await Assert.ThrowsAsync<PartidaTriviaNotFoundException>(
            () => _handler.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_MinimosNoCumplidos_ThrowsMinimosNoCumplidosException()
    {
        var partida = CreatePartidaEnLobby(minimo: CantidadMinima.Create(5));
        var partidaId = partida.Id.Value;

        _repoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(partida);
        _repoMock
            .Setup(r => r.CountInscripcionesAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var cmd = new StartTriviaGameCommand(partidaId);

        await Assert.ThrowsAsync<MinimosNoCumplidosException>(
            () => _handler.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_PartidaYaIniciada_ThrowsInvalidStateTransitionException()
    {
        var partida = CreatePartidaEnLobby();
        partida.Iniciar(cantidadInscriptos: 2);
        var partidaId = partida.Id.Value;

        _repoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(partida);
        _repoMock
            .Setup(r => r.CountInscripcionesAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var cmd = new StartTriviaGameCommand(partidaId);

        await Assert.ThrowsAsync<InvalidStateTransitionException>(
            () => _handler.Handle(cmd, CancellationToken.None));
    }
}
