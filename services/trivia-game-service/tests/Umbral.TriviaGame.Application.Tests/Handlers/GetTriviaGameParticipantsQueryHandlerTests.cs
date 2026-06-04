using Moq;
using Umbral.TriviaGame.Application.Handlers;
using Umbral.TriviaGame.Application.Ports;
using Umbral.TriviaGame.Application.Queries;
using Umbral.TriviaGame.Domain.Common.Exceptions;
using Umbral.TriviaGame.Domain.Entities;
using Umbral.TriviaGame.Domain.Enums;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Application.Tests.Handlers;

public class GetTriviaGameParticipantsQueryHandlerTests
{
    private readonly Mock<IPartidaTriviaRepository> _partidaRepoMock = new();
    private readonly Mock<ITriviaInscripcionRepository> _inscripcionRepoMock = new();
    private readonly GetTriviaGameParticipantsQueryHandler _handler;

    public GetTriviaGameParticipantsQueryHandlerTests()
    {
        _handler = new GetTriviaGameParticipantsQueryHandler(
            _partidaRepoMock.Object,
            _inscripcionRepoMock.Object);
    }

    [Fact]
    public async Task Handle_PartidaExiste_RetornaDtoConParticipantes()
    {
        var partida = CrearPartidaEnLobby();
        var partidaId = partida.Id.Value;

        _partidaRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(partida);
        _inscripcionRepoMock
            .Setup(r => r.ListByPartidaIdAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TriviaInscripcion>
            {
                TriviaInscripcion.Create(PartidaId.Create(partidaId), "user-1"),
                TriviaInscripcion.Create(PartidaId.Create(partidaId), "user-2"),
                TriviaInscripcion.Create(PartidaId.Create(partidaId), "user-3"),
            });

        var query = new GetTriviaGameParticipantsQuery(partidaId);
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(partidaId, result.PartidaId);
        Assert.Equal(partida.Nombre.Value, result.Nombre);
        Assert.Equal("Lobby", result.Estado);
        Assert.Equal(3, result.ParticipantesActual);
        Assert.Equal(3, result.Participantes.Count);
    }

    [Fact]
    public async Task Handle_PartidaNoExiste_ThrowsPartidaTriviaNotFoundException()
    {
        _partidaRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PartidaTrivia?)null);

        var query = new GetTriviaGameParticipantsQuery(Guid.NewGuid());

        await Assert.ThrowsAsync<PartidaTriviaNotFoundException>(
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
