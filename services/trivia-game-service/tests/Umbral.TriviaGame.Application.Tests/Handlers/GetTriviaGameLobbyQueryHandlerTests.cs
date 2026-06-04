using Moq;
using Umbral.TriviaGame.Application.Handlers;
using Umbral.TriviaGame.Application.Ports;
using Umbral.TriviaGame.Application.Queries;
using Umbral.TriviaGame.Domain.Common.Exceptions;
using Umbral.TriviaGame.Domain.Entities;
using Umbral.TriviaGame.Domain.Enums;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Application.Tests.Handlers;

public class GetTriviaGameLobbyQueryHandlerTests
{
    private readonly Mock<IPartidaTriviaRepository> _partidaRepoMock = new();
    private readonly Mock<ITriviaInscripcionRepository> _inscripcionRepoMock = new();
    private readonly GetTriviaGameLobbyQueryHandler _handler;

    public GetTriviaGameLobbyQueryHandlerTests()
    {
        _handler = new GetTriviaGameLobbyQueryHandler(
            _partidaRepoMock.Object,
            _inscripcionRepoMock.Object);
    }

    [Fact]
    public async Task Handle_PartidaExisteYUsuarioInscrito_RetornaDto()
    {
        var partida = CrearPartidaEnLobby();
        var partidaId = partida.Id.Value;

        _partidaRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(partida);
        _inscripcionRepoMock
            .Setup(r => r.ExistsByPartidaYUsuarioAsync(It.IsAny<PartidaId>(), "user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _inscripcionRepoMock
            .Setup(r => r.ListByPartidaIdAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TriviaInscripcion>
            {
                TriviaInscripcion.Create(PartidaId.Create(partidaId), "user-1"),
                TriviaInscripcion.Create(PartidaId.Create(partidaId), "user-2"),
            });

        var query = new GetTriviaGameLobbyQuery(partidaId, "user-1");
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(partidaId, result.PartidaId);
        Assert.Equal(partida.Nombre.Value, result.Nombre);
        Assert.Equal("Lobby", result.Estado);
        Assert.Equal(2, result.ParticipantesActual);
        Assert.Equal(2, result.Participantes.Count);
    }

    [Fact]
    public async Task Handle_PartidaNoExiste_ThrowsPartidaTriviaNotFoundException()
    {
        _partidaRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PartidaTrivia?)null);

        var query = new GetTriviaGameLobbyQuery(Guid.NewGuid(), "user-1");

        await Assert.ThrowsAsync<PartidaTriviaNotFoundException>(
            () => _handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UsuarioNoInscrito_ThrowsUsuarioNoInscritoException()
    {
        var partida = CrearPartidaEnLobby();

        _partidaRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(partida);
        _inscripcionRepoMock
            .Setup(r => r.ExistsByPartidaYUsuarioAsync(It.IsAny<PartidaId>(), "user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var query = new GetTriviaGameLobbyQuery(partida.Id.Value, "user-1");

        await Assert.ThrowsAsync<UsuarioNoInscritoException>(
            () => _handler.Handle(query, CancellationToken.None));
    }

    private static PartidaTrivia CrearPartidaEnLobby()
    {
        var min = CantidadMinima.Create(1);
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
}
