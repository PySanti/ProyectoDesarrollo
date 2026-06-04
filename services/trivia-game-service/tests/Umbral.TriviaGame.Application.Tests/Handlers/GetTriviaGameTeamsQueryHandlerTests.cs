using Moq;
using Umbral.TriviaGame.Application.Handlers;
using Umbral.TriviaGame.Application.Ports;
using Umbral.TriviaGame.Application.Queries;
using Umbral.TriviaGame.Domain.Common.Exceptions;
using Umbral.TriviaGame.Domain.Entities;
using Umbral.TriviaGame.Domain.Enums;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Application.Tests.Handlers;

public class GetTriviaGameTeamsQueryHandlerTests
{
    private readonly Mock<IPartidaTriviaRepository> _partidaRepoMock = new();
    private readonly Mock<ITriviaInscripcionRepository> _inscripcionRepoMock = new();
    private readonly GetTriviaGameTeamsQueryHandler _handler;

    public GetTriviaGameTeamsQueryHandlerTests()
    {
        _handler = new GetTriviaGameTeamsQueryHandler(
            _partidaRepoMock.Object,
            _inscripcionRepoMock.Object);
    }

    [Fact]
    public async Task Handle_PartidaConEquipos_RetornaEquiposUnicos()
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
                TriviaInscripcion.Create(PartidaId.Create(partidaId), "user-1", equipoId: "equipo-1"),
                TriviaInscripcion.Create(PartidaId.Create(partidaId), "user-2", equipoId: "equipo-1"),
                TriviaInscripcion.Create(PartidaId.Create(partidaId), "user-3", equipoId: "equipo-2"),
            });

        var query = new GetTriviaGameTeamsQuery(partidaId);
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, e => e.EquipoId == "equipo-1");
        Assert.Contains(result, e => e.EquipoId == "equipo-2");
    }

    [Fact]
    public async Task Handle_PartidaSinEquipos_RetornaListaVacia()
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
            });

        var query = new GetTriviaGameTeamsQuery(partidaId);
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_PartidaNoExiste_ThrowsPartidaTriviaNotFoundException()
    {
        _partidaRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PartidaTrivia?)null);

        var query = new GetTriviaGameTeamsQuery(Guid.NewGuid());

        await Assert.ThrowsAsync<PartidaTriviaNotFoundException>(
            () => _handler.Handle(query, CancellationToken.None));
    }

    private static PartidaTrivia CrearPartidaEnLobby()
    {
        var min = CantidadMinima.Create(1);
        return PartidaTrivia.Create(
            NombrePartida.Create("Test"),
            Modalidad.Equipo,
            ModoInicio.Manual,
            TriviaFormId.New(),
            OperatorId.Create("op-1"),
            TiempoInicio.Create(DateTimeOffset.UtcNow.AddDays(1)),
            min,
            maximoJugadores: null,
            maximoEquipos: CantidadMaximaEquipos.Create(5),
            minJugadoresPorEquipo: JugadoresPorEquipoMin.Create(1),
            maxJugadoresPorEquipo: JugadoresPorEquipoMax.Create(5));
    }
}
