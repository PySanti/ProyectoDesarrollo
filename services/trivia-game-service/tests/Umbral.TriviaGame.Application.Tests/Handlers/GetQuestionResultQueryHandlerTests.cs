using Moq;
using Umbral.TriviaGame.Application.Handlers;
using Umbral.TriviaGame.Application.Ports;
using Umbral.TriviaGame.Application.Queries;
using Umbral.TriviaGame.Domain.Common;
using Umbral.TriviaGame.Domain.Common.Exceptions;
using Umbral.TriviaGame.Domain.Drafts;
using Umbral.TriviaGame.Domain.Entities;
using Umbral.TriviaGame.Domain.Enums;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Application.Tests.Handlers;

public class GetQuestionResultQueryHandlerTests
{
    private readonly Mock<IPartidaTriviaRepository> _partidaRepoMock = new();
    private readonly Mock<ITriviaFormRepository> _formRepoMock = new();
    private readonly GetQuestionResultQueryHandler _handler;

    public GetQuestionResultQueryHandlerTests()
    {
        _handler = new GetQuestionResultQueryHandler(
            _partidaRepoMock.Object,
            _formRepoMock.Object);
    }

    private static TriviaForm CreateFormWithOneQuestion()
    {
        return TriviaForm.Create(
            FormTitle.Create("Test Form"),
            OperatorId.Create("op-1"),
            new List<QuestionDraft>
            {
                QuestionDraft.Create("Q1", 100, 30, 1,
                    new List<AnswerOptionDraft>
                    {
                        AnswerOptionDraft.Create("A", true),
                        AnswerOptionDraft.Create("B", false),
                        AnswerOptionDraft.Create("C", false),
                        AnswerOptionDraft.Create("D", false),
                    }),
            });
    }

    private static TriviaForm CreateFormWithTwoQuestions()
    {
        return TriviaForm.Create(
            FormTitle.Create("Test Form"),
            OperatorId.Create("op-1"),
            new List<QuestionDraft>
            {
                QuestionDraft.Create("Q1", 100, 30, 1,
                    new List<AnswerOptionDraft>
                    {
                        AnswerOptionDraft.Create("A", true),
                        AnswerOptionDraft.Create("B", false),
                        AnswerOptionDraft.Create("C", false),
                        AnswerOptionDraft.Create("D", false),
                    }),
                QuestionDraft.Create("Q2", 50, 30, 2,
                    new List<AnswerOptionDraft>
                    {
                        AnswerOptionDraft.Create("W", false),
                        AnswerOptionDraft.Create("X", true),
                        AnswerOptionDraft.Create("Y", false),
                        AnswerOptionDraft.Create("Z", false),
                    }),
            });
    }

    private static PartidaTrivia CrearPartidaIniciadaConFormulario(TriviaForm form, out QuestionId preguntaId)
    {
        var min = CantidadMinima.Create(1);
        var partida = PartidaTrivia.Create(
            NombrePartida.Create("Test"),
            Modalidad.Individual,
            ModoInicio.Manual,
            form.Id,
            OperatorId.Create("op-1"),
            TiempoInicio.Create(DateTimeOffset.UtcNow.AddDays(1)),
            min,
            maximoJugadores: CantidadMaximaJugadores.Create(10, min),
            maximoEquipos: null,
            minJugadoresPorEquipo: null,
            maxJugadoresPorEquipo: null);

        preguntaId = form.Questions.First().Id;
        partida.Iniciar(cantidadInscriptos: 2, esInicioManual: true, primeraPreguntaId: preguntaId);
        return partida;
    }

    [Fact]
    public async Task Handle_PreguntaCerradaPorTiempo_ReturnsResultadoCorrecto()
    {
        var form = CreateFormWithOneQuestion();
        var partida = CrearPartidaIniciadaConFormulario(form, out var preguntaId);
        partida.CerrarPreguntaActual(MotivoCierre.TimeExpired, "A");
        partida.FlushDomainEvents();

        _partidaRepoMock
            .Setup(r => r.GetByIdWithRespuestasAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(partida);
        _formRepoMock
            .Setup(r => r.GetByIdAsync(form.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(form);

        var query = new GetQuestionResultQuery(partida.Id.Value, preguntaId.Value, "user-1");
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(preguntaId.Value, result.PreguntaId);
        Assert.Equal("Q1", result.TextoPregunta);
        Assert.Equal(0, result.OpcionCorrectaIndex);
        Assert.Equal("A", result.OpcionCorrectaText);
        Assert.Null(result.MiOpcionIndex);
        Assert.Null(result.MiOpcionText);
        Assert.Null(result.EsCorrecta);
        Assert.Equal(0, result.PuntajeObtenido);
        Assert.Equal("TiempoAgotado", result.MotivoCierre);
    }

    [Fact]
    public async Task Handle_PreguntaCerradaPorRespuestaCorrectaDevuelta_ReturnsMotivoCorrectAnswer()
    {
        var form = CreateFormWithOneQuestion();
        var partida = CrearPartidaIniciadaConFormulario(form, out var preguntaId);

        partida.RegistrarRespuestaDefinitiva(
            preguntaId, "user-1", 0, esCorrecta: true,
            assignedScore: 100, timeLimitSeconds: 300,
            respuestaCorrecta: "A");
        partida.FlushDomainEvents();

        _partidaRepoMock
            .Setup(r => r.GetByIdWithRespuestasAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(partida);
        _formRepoMock
            .Setup(r => r.GetByIdAsync(form.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(form);

        var query = new GetQuestionResultQuery(partida.Id.Value, preguntaId.Value, "user-1");
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(0, result.MiOpcionIndex);
        Assert.Equal("A", result.MiOpcionText);
        Assert.True(result.EsCorrecta);
        Assert.Equal(100, result.PuntajeObtenido);
        Assert.Equal("RespuestaCorrecta", result.MotivoCierre);
    }

    [Fact]
    public async Task Handle_PreguntaActiva_ThrowsDomainValidationException()
    {
        var form = CreateFormWithOneQuestion();
        var partida = CrearPartidaIniciadaConFormulario(form, out var preguntaId);

        _partidaRepoMock
            .Setup(r => r.GetByIdWithRespuestasAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(partida);
        _formRepoMock
            .Setup(r => r.GetByIdAsync(form.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(form);

        var query = new GetQuestionResultQuery(partida.Id.Value, preguntaId.Value, "user-1");

        await Assert.ThrowsAsync<DomainValidationException>(
            () => _handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_PartidaNotFound_ThrowsPartidaTriviaNotFoundException()
    {
        _partidaRepoMock
            .Setup(r => r.GetByIdWithRespuestasAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PartidaTrivia?)null);

        var query = new GetQuestionResultQuery(Guid.NewGuid(), Guid.NewGuid(), "user-1");

        await Assert.ThrowsAsync<PartidaTriviaNotFoundException>(
            () => _handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_FormNotFound_ThrowsDomainValidationException()
    {
        var form = CreateFormWithOneQuestion();
        var partida = CrearPartidaIniciadaConFormulario(form, out var preguntaId);
        partida.CerrarPreguntaActual(MotivoCierre.TimeExpired, "A");
        partida.FlushDomainEvents();

        _partidaRepoMock
            .Setup(r => r.GetByIdWithRespuestasAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(partida);
        _formRepoMock
            .Setup(r => r.GetByIdAsync(form.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TriviaForm?)null);

        var query = new GetQuestionResultQuery(partida.Id.Value, preguntaId.Value, "user-1");

        await Assert.ThrowsAsync<DomainValidationException>(
            () => _handler.Handle(query, CancellationToken.None));
    }
}
