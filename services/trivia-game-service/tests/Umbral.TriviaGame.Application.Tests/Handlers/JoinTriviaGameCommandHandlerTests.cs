using Moq;
using Umbral.TriviaGame.Application.Commands;
using Umbral.TriviaGame.Application.Handlers;
using Umbral.TriviaGame.Application.Ports;
using Umbral.TriviaGame.Domain.Common.Exceptions;
using Umbral.TriviaGame.Domain.Entities;
using Umbral.TriviaGame.Domain.Enums;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Application.Tests.Handlers;

public class JoinTriviaGameCommandHandlerTests
{
    private readonly Mock<IPartidaTriviaRepository> _partidaRepoMock = new();
    private readonly Mock<ITriviaInscripcionRepository> _inscripcionRepoMock = new();
    private readonly JoinTriviaGameCommandHandler _handler;

    public JoinTriviaGameCommandHandlerTests()
    {
        _handler = new JoinTriviaGameCommandHandler(_partidaRepoMock.Object, _inscripcionRepoMock.Object);
    }

    private static PartidaTrivia CreatePartidaIndividualEnLobby(int maxJugadores = 10)
    {
        var min = CantidadMinima.Create(1);
        return PartidaTrivia.Create(
            NombrePartida.Create("Test Individual"),
            Modalidad.Individual,
            ModoInicio.Manual,
            TriviaFormId.New(),
            OperatorId.Create("op-1"),
            TiempoInicio.Create(DateTimeOffset.UtcNow.AddDays(1)),
            min,
            maximoJugadores: CantidadMaximaJugadores.Create(maxJugadores, min),
            maximoEquipos: null,
            minJugadoresPorEquipo: null,
            maxJugadoresPorEquipo: null);
    }

    private static PartidaTrivia CreatePartidaEquipoEnLobby()
    {
        var min = CantidadMinima.Create(1);
        var maxEquipos = CantidadMaximaEquipos.Create(5);
        var minPorEquipo = JugadoresPorEquipoMin.Create(1);
        var maxPorEquipo = JugadoresPorEquipoMax.Create(5, minPorEquipo);
        return PartidaTrivia.Create(
            NombrePartida.Create("Test Equipo"),
            Modalidad.Equipo,
            ModoInicio.Manual,
            TriviaFormId.New(),
            OperatorId.Create("op-1"),
            TiempoInicio.Create(DateTimeOffset.UtcNow.AddDays(1)),
            min,
            maximoJugadores: null,
            maximoEquipos: maxEquipos,
            minJugadoresPorEquipo: minPorEquipo,
            maxJugadoresPorEquipo: maxPorEquipo);
    }

    private JoinTriviaGameCommand CreateCommand(PartidaTrivia partida, string usuarioId = "user-1")
    {
        return new JoinTriviaGameCommand(partida.Id.Value, usuarioId);
    }

    [Fact]
    public async Task Handle_PartidaIndividualEnLobbyConCupo_InscribeExitosamente()
    {
        var partida = CreatePartidaIndividualEnLobby();
        var cmd = CreateCommand(partida);

        _partidaRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(partida);
        _inscripcionRepoMock
            .Setup(r => r.CountByPartidaIdAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _inscripcionRepoMock
            .Setup(r => r.ExistsByPartidaYUsuarioAsync(It.IsAny<PartidaId>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _handler.Handle(cmd, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(partida.Id.Value, result.PartidaId);
        Assert.NotEqual(Guid.Empty, result.InscripcionId);
        Assert.NotEqual(default, result.FechaInscripcion);

        _inscripcionRepoMock.Verify(r => r.AddAsync(It.IsAny<TriviaInscripcion>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PartidaNoExiste_ThrowsPartidaTriviaNotFoundException()
    {
        var cmd = new JoinTriviaGameCommand(Guid.NewGuid(), "user-1");

        _partidaRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PartidaTrivia?)null);

        await Assert.ThrowsAsync<PartidaTriviaNotFoundException>(
            () => _handler.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_PartidaNoEnLobby_ThrowsInvalidStateTransitionException()
    {
        var partida = CreatePartidaIndividualEnLobby();
        partida.Iniciar(cantidadInscriptos: 1);
        var cmd = CreateCommand(partida);

        _partidaRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(partida);

        await Assert.ThrowsAsync<InvalidStateTransitionException>(
            () => _handler.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_PartidaModalidadEquipo_ThrowsModalidadInvalidaException()
    {
        var partida = CreatePartidaEquipoEnLobby();
        var cmd = CreateCommand(partida);

        _partidaRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(partida);

        await Assert.ThrowsAsync<ModalidadInvalidaException>(
            () => _handler.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_CupoLleno_ThrowsCupoLlenoException()
    {
        var partida = CreatePartidaIndividualEnLobby(maxJugadores: 2);
        var cmd = CreateCommand(partida);

        _partidaRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(partida);
        _inscripcionRepoMock
            .Setup(r => r.CountByPartidaIdAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        await Assert.ThrowsAsync<CupoLlenoException>(
            () => _handler.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UsuarioYaInscrito_ThrowsJugadorYaInscritoException()
    {
        var partida = CreatePartidaIndividualEnLobby();
        var cmd = CreateCommand(partida);

        _partidaRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(partida);
        _inscripcionRepoMock
            .Setup(r => r.CountByPartidaIdAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _inscripcionRepoMock
            .Setup(r => r.ExistsByPartidaYUsuarioAsync(It.IsAny<PartidaId>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<JugadorYaInscritoException>(
            () => _handler.Handle(cmd, CancellationToken.None));
    }
}
