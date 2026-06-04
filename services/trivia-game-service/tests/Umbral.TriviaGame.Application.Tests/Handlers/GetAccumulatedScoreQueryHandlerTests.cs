using Moq;
using Umbral.TriviaGame.Application.Handlers;
using Umbral.TriviaGame.Application.Ports;
using Umbral.TriviaGame.Application.Queries;
using Umbral.TriviaGame.Domain.Common;
using Umbral.TriviaGame.Domain.Common.Exceptions;
using Umbral.TriviaGame.Domain.Entities;
using Umbral.TriviaGame.Domain.Enums;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Application.Tests.Handlers;

public class GetAccumulatedScoreQueryHandlerTests
{
    private readonly Mock<IPartidaTriviaRepository> _partidaRepoMock = new();
    private readonly GetAccumulatedScoreQueryHandler _handler;

    public GetAccumulatedScoreQueryHandlerTests()
    {
        _handler = new GetAccumulatedScoreQueryHandler(_partidaRepoMock.Object);
    }

    private static PartidaTrivia CrearPartidaConRespuestas()
    {
        var min = CantidadMinima.Create(1);
        var partida = PartidaTrivia.Create(
            NombrePartida.Create("Score Test"),
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

        var preguntaId1 = QuestionId.New();
        var preguntaId2 = QuestionId.New();
        var preguntaId3 = QuestionId.New();

        partida.Iniciar(cantidadInscriptos: 2, esInicioManual: true, primeraPreguntaId: preguntaId1);

        partida.RegistrarRespuestaDefinitiva(
            preguntaId1, "user-1", 0, esCorrecta: true,
            assignedScore: 100, timeLimitSeconds: 300);

        partida.AbrirPregunta(preguntaId2);

        partida.RegistrarRespuestaDefinitiva(
            preguntaId2, "user-1", 1, esCorrecta: false,
            assignedScore: 50, timeLimitSeconds: 300);

        partida.AbrirPregunta(preguntaId3);

        partida.RegistrarRespuestaDefinitiva(
            preguntaId3, "user-1", 2, esCorrecta: true,
            assignedScore: 200, timeLimitSeconds: 300);

        return partida;
    }

    [Fact]
    public async Task Handle_ConRespuestas_RetornaScoreAcumuladoCorrecto()
    {
        var partida = CrearPartidaConRespuestas();

        _partidaRepoMock
            .Setup(r => r.GetByIdWithRespuestasAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(partida);

        var query = new GetAccumulatedScoreQuery(partida.Id.Value, "user-1");
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(partida.Id.Value, result.PartidaId);
        Assert.Equal(300, result.PuntajeAcumulado);
        Assert.True(result.TiempoAcumuladoSegundos >= 0);
        Assert.Equal(2, result.RespuestasCorrectas);
        Assert.Equal(3, result.TotalRespuestas);
    }

    [Fact]
    public async Task Handle_SinRespuestas_RetornaCeros()
    {
        var min = CantidadMinima.Create(1);
        var partida = PartidaTrivia.Create(
            NombrePartida.Create("Empty Score"),
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

        _partidaRepoMock
            .Setup(r => r.GetByIdWithRespuestasAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(partida);

        var query = new GetAccumulatedScoreQuery(partida.Id.Value, "user-1");
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Equal(0, result.PuntajeAcumulado);
        Assert.Equal(0, result.RespuestasCorrectas);
        Assert.Equal(0, result.TotalRespuestas);
    }

    [Fact]
    public async Task Handle_PartidaNotFound_ThrowsPartidaTriviaNotFoundException()
    {
        _partidaRepoMock
            .Setup(r => r.GetByIdWithRespuestasAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PartidaTrivia?)null);

        var query = new GetAccumulatedScoreQuery(Guid.NewGuid(), "user-1");

        await Assert.ThrowsAsync<PartidaTriviaNotFoundException>(
            () => _handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_RespuestasDeOtroUsuario_RetornaCeros()
    {
        var partida = CrearPartidaConRespuestas();

        _partidaRepoMock
            .Setup(r => r.GetByIdWithRespuestasAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(partida);

        var query = new GetAccumulatedScoreQuery(partida.Id.Value, "other-user");
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Equal(0, result.PuntajeAcumulado);
        Assert.Equal(0, result.RespuestasCorrectas);
        Assert.Equal(0, result.TotalRespuestas);
    }
}
