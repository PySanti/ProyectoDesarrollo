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

public class GetRankingQueryHandlerTests
{
    private readonly Mock<IPartidaTriviaRepository> _partidaRepoMock = new();
    private readonly Mock<ITriviaInscripcionRepository> _inscripcionRepoMock = new();
    private readonly GetRankingQueryHandler _handler;

    public GetRankingQueryHandlerTests()
    {
        _handler = new GetRankingQueryHandler(_partidaRepoMock.Object, _inscripcionRepoMock.Object);
    }

    private static (PartidaTrivia partida, List<TriviaInscripcion> inscripciones) CrearPartidaConDosParticipantes()
    {
        var min = CantidadMinima.Create(1);
        var partida = PartidaTrivia.Create(
            NombrePartida.Create("Ranking Test"),
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

        var partidaId = partida.Id;

        var p1 = QuestionId.New();
        var p2 = QuestionId.New();
        var p3 = QuestionId.New();

        partida.Iniciar(cantidadInscriptos: 2, esInicioManual: true, primeraPreguntaId: p1);

        // user-1: correcta (100pts), correcta (200pts)
        partida.RegistrarRespuestaDefinitiva(p1, "user-1", 0, esCorrecta: true, assignedScore: 100, timeLimitSeconds: 300);
        partida.AbrirPregunta(p2);
        partida.RegistrarRespuestaDefinitiva(p2, "user-1", 1, esCorrecta: true, assignedScore: 200, timeLimitSeconds: 300);
        partida.AbrirPregunta(p3);

        // user-2: correcta (150pts), incorrecta (0pts)
        partida.RegistrarRespuestaDefinitiva(p3, "user-2", 0, esCorrecta: true, assignedScore: 150, timeLimitSeconds: 300);

        var inscripciones = new List<TriviaInscripcion>
        {
            TriviaInscripcion.Create(partidaId, "user-1"),
            TriviaInscripcion.Create(partidaId, "user-2"),
        };

        return (partida, inscripciones);
    }

    [Fact]
    public async Task Handle_ConParticipantes_RetornaRankingOrdenadoPorPuntajeDescendente()
    {
        var (partida, inscripciones) = CrearPartidaConDosParticipantes();

        _partidaRepoMock
            .Setup(r => r.GetByIdWithRespuestasAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(partida);

        _inscripcionRepoMock
            .Setup(r => r.ListByPartidaIdAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(inscripciones);

        var query = new GetRankingQuery(partida.Id.Value);
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("user-1", result[0].UsuarioId);
        Assert.Equal(300, result[0].PuntajeAcumulado);
        Assert.Equal("user-2", result[1].UsuarioId);
        Assert.Equal(150, result[1].PuntajeAcumulado);
        Assert.Equal(1, result[0].Posicion);
        Assert.Equal(2, result[1].Posicion);
    }

    [Fact]
    public async Task Handle_EmpatePorPuntaje_DesempataPorMenorTiempo()
    {
        var min = CantidadMinima.Create(1);
        var partida = PartidaTrivia.Create(
            NombrePartida.Create("Tie Test"),
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

        var partidaId = partida.Id;
        var q1 = QuestionId.New();
        var q2 = QuestionId.New();

        partida.Iniciar(cantidadInscriptos: 2, esInicioManual: true, primeraPreguntaId: q1);

        // user-1 answers q1 correctly (100pts), question closes
        partida.RegistrarRespuestaDefinitiva(q1, "user-1", 0, esCorrecta: true, assignedScore: 100, timeLimitSeconds: 300);
        // Open q2
        partida.AbrirPregunta(q2);
        // user-2 answers q2 correctly (100pts), question closes
        partida.RegistrarRespuestaDefinitiva(q2, "user-2", 0, esCorrecta: true, assignedScore: 100, timeLimitSeconds: 300);

        var inscripciones = new List<TriviaInscripcion>
        {
            TriviaInscripcion.Create(partidaId, "user-1"),
            TriviaInscripcion.Create(partidaId, "user-2"),
        };

        _partidaRepoMock
            .Setup(r => r.GetByIdWithRespuestasAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(partida);

        _inscripcionRepoMock
            .Setup(r => r.ListByPartidaIdAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(inscripciones);

        var query = new GetRankingQuery(partidaId.Value);
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Equal(2, result.Count);
        // Both have 100pts; ordered by accumulated time ASC
        Assert.Equal(100, result[0].PuntajeAcumulado);
        Assert.Equal(100, result[1].PuntajeAcumulado);
        Assert.True(result[0].TiempoAcumuladoSegundos <= result[1].TiempoAcumuladoSegundos);
    }

    [Fact]
    public async Task Handle_SinInscripciones_RetornaListaVacia()
    {
        var min = CantidadMinima.Create(1);
        var partida = PartidaTrivia.Create(
            NombrePartida.Create("Empty"),
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

        _inscripcionRepoMock
            .Setup(r => r.ListByPartidaIdAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TriviaInscripcion>());

        var query = new GetRankingQuery(partida.Id.Value);
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_PartidaNotFound_ThrowsPartidaTriviaNotFoundException()
    {
        _partidaRepoMock
            .Setup(r => r.GetByIdWithRespuestasAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PartidaTrivia?)null);

        var query = new GetRankingQuery(Guid.NewGuid());
        await Assert.ThrowsAsync<PartidaTriviaNotFoundException>(
            () => _handler.Handle(query, CancellationToken.None));
    }
}
