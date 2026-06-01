using Moq;
using Umbral.TriviaGame.Application.Commands;
using Umbral.TriviaGame.Application.Handlers;
using Umbral.TriviaGame.Application.Ports;
using Umbral.TriviaGame.Domain.Common;
using Umbral.TriviaGame.Domain.Common.Exceptions;
using Umbral.TriviaGame.Domain.Drafts;
using Umbral.TriviaGame.Domain.Entities;
using Umbral.TriviaGame.Domain.Enums;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Application.Tests.Handlers;

public class AnswerTriviaQuestionCommandHandlerTests
{
    private readonly Mock<IPartidaTriviaRepository> _partidaRepoMock = new();
    private readonly Mock<ITriviaFormRepository> _formRepoMock = new();
    private readonly Mock<IDomainEventDispatcher> _eventMock = new();
    private readonly Mock<ITriviaRankingNotifier> _rankingNotifierMock = new();
    private readonly AnswerTriviaQuestionCommandHandler _handler;

    public AnswerTriviaQuestionCommandHandlerTests()
    {
        _handler = new AnswerTriviaQuestionCommandHandler(
            _partidaRepoMock.Object,
            _formRepoMock.Object,
            _eventMock.Object,
            _rankingNotifierMock.Object);
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
                        AnswerOptionDraft.Create("A", false),
                        AnswerOptionDraft.Create("B", true),
                        AnswerOptionDraft.Create("C", false),
                        AnswerOptionDraft.Create("D", false),
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

    private AnswerTriviaQuestionCommand CrearComando(PartidaTrivia partida, QuestionId preguntaId, string usuarioId, int opcionIndex)
    {
        return new AnswerTriviaQuestionCommand(partida.Id.Value, preguntaId.Value, usuarioId, opcionIndex);
    }

    // =====================================================================
    // Happy paths
    // =====================================================================

    [Fact]
    public async Task Handle_RespuestaCorrecta_ReturnsDtoConEsCorrectaTrue()
    {
        var form = CreateFormWithOneQuestion();
        var partida = CrearPartidaIniciadaConFormulario(form, out var preguntaId);
        var cmd = CrearComando(partida, preguntaId, "user-1", opcionIndex: 0);

        _partidaRepoMock
            .Setup(r => r.GetByIdWithRespuestasAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(partida);
        _formRepoMock
            .Setup(r => r.GetByIdAsync(form.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(form);

        var result = await _handler.Handle(cmd, CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.EsCorrecta);
        Assert.Equal(100, result.PuntajeObtenido);
        Assert.Equal(partida.Id.Value, result.PartidaId);
        Assert.Equal(preguntaId.Value, result.PreguntaId);
        Assert.NotEqual(Guid.Empty, result.RespuestaId);
        Assert.NotEqual(default, result.FechaRespuesta);

        _partidaRepoMock.Verify(r => r.UpdateAsync(partida, It.IsAny<CancellationToken>()), Times.Once);
        _eventMock.Verify(e => e.DispatchAsync(It.IsAny<IReadOnlyList<DomainEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RespuestaIncorrecta_ReturnsDtoConEsCorrectaFalse()
    {
        var form = CreateFormWithOneQuestion();
        var partida = CrearPartidaIniciadaConFormulario(form, out var preguntaId);
        var cmd = CrearComando(partida, preguntaId, "user-1", opcionIndex: 1);

        _partidaRepoMock
            .Setup(r => r.GetByIdWithRespuestasAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(partida);
        _formRepoMock
            .Setup(r => r.GetByIdAsync(form.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(form);

        var result = await _handler.Handle(cmd, CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(result.EsCorrecta);
        Assert.Equal(0, result.PuntajeObtenido);
    }

    [Fact]
    public async Task Handle_PreguntaIndexCorrecto_RespuestaIncorrecta()
    {
        var form = CreateFormWithOneQuestion();
        var partida = CrearPartidaIniciadaConFormulario(form, out var preguntaId);
        var cmd = CrearComando(partida, preguntaId, "user-1", opcionIndex: 2);

        _partidaRepoMock
            .Setup(r => r.GetByIdWithRespuestasAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(partida);
        _formRepoMock
            .Setup(r => r.GetByIdAsync(form.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(form);

        var result = await _handler.Handle(cmd, CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(result.EsCorrecta);
        Assert.Equal(0, result.PuntajeObtenido);
    }

    // =====================================================================
    // Error cases
    // =====================================================================

    [Fact]
    public async Task Handle_PartidaNotFound_ThrowsPartidaTriviaNotFoundException()
    {
        _partidaRepoMock
            .Setup(r => r.GetByIdWithRespuestasAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PartidaTrivia?)null);

        var cmd = new AnswerTriviaQuestionCommand(Guid.NewGuid(), Guid.NewGuid(), "user-1", 0);

        await Assert.ThrowsAsync<PartidaTriviaNotFoundException>(
            () => _handler.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_FormNotFound_ThrowsDomainValidationException()
    {
        var form = CreateFormWithOneQuestion();
        var partida = CrearPartidaIniciadaConFormulario(form, out var preguntaId);

        _partidaRepoMock
            .Setup(r => r.GetByIdWithRespuestasAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(partida);
        _formRepoMock
            .Setup(r => r.GetByIdAsync(form.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TriviaForm?)null);

        var cmd = CrearComando(partida, preguntaId, "user-1", 0);

        await Assert.ThrowsAsync<DomainValidationException>(
            () => _handler.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_PreguntaNoPerteneceAlFormulario_ThrowsDomainValidationException()
    {
        var form = CreateFormWithOneQuestion();
        var partida = CrearPartidaIniciadaConFormulario(form, out var _);
        var preguntaIdFuera = QuestionId.New();

        _partidaRepoMock
            .Setup(r => r.GetByIdWithRespuestasAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(partida);
        _formRepoMock
            .Setup(r => r.GetByIdAsync(form.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(form);

        var cmd = CrearComando(partida, preguntaIdFuera, "user-1", 0);

        var ex = await Assert.ThrowsAsync<DomainValidationException>(
            () => _handler.Handle(cmd, CancellationToken.None));
        Assert.Contains("no pertenece al formulario", ex.Message);
    }

    [Fact]
    public async Task Handle_OpcionIndexInvalido_ThrowsDomainValidationException()
    {
        var form = CreateFormWithOneQuestion();
        var partida = CrearPartidaIniciadaConFormulario(form, out var preguntaId);

        _partidaRepoMock
            .Setup(r => r.GetByIdWithRespuestasAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(partida);
        _formRepoMock
            .Setup(r => r.GetByIdAsync(form.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(form);

        var cmd = CrearComando(partida, preguntaId, "user-1", 99);

        var ex = await Assert.ThrowsAsync<DomainValidationException>(
            () => _handler.Handle(cmd, CancellationToken.None));
        Assert.Contains("no existe", ex.Message);
    }

    [Fact]
    public async Task Handle_PartidaEnLobby_ThrowsEstadoPartidaInvalido()
    {
        var form = CreateFormWithOneQuestion();
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

        _partidaRepoMock
            .Setup(r => r.GetByIdWithRespuestasAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(partida);
        _formRepoMock
            .Setup(r => r.GetByIdAsync(form.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(form);

        var preguntaId = form.Questions.First().Id;
        var cmd = CrearComando(partida, preguntaId, "user-1", 0);

        await Assert.ThrowsAsync<EstadoPartidaInvalidoException>(
            () => _handler.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_RespuestaDuplicadaEnPartida_ThrowsRespuestaDuplicadaException()
    {
        var form = CreateFormWithOneQuestion();
        var partida = CrearPartidaIniciadaConFormulario(form, out var preguntaId);

        partida.RegistrarRespuestaDefinitiva(
            preguntaId, "user-1", 0, esCorrecta: false,
            assignedScore: 100, timeLimitSeconds: 300);

        _partidaRepoMock
            .Setup(r => r.GetByIdWithRespuestasAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(partida);
        _formRepoMock
            .Setup(r => r.GetByIdAsync(form.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(form);

        var cmd = CrearComando(partida, preguntaId, "user-1", 1);

        await Assert.ThrowsAsync<RespuestaDuplicadaException>(
            () => _handler.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NoSaveWhenExceptionThrown()
    {
        _partidaRepoMock
            .Setup(r => r.GetByIdWithRespuestasAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PartidaTrivia?)null);

        var cmd = new AnswerTriviaQuestionCommand(Guid.NewGuid(), Guid.NewGuid(), "user-1", 0);

        await Assert.ThrowsAsync<PartidaTriviaNotFoundException>(
            () => _handler.Handle(cmd, CancellationToken.None));

        _partidaRepoMock.Verify(r => r.UpdateAsync(It.IsAny<PartidaTrivia>(), It.IsAny<CancellationToken>()), Times.Never);
        _eventMock.Verify(e => e.DispatchAsync(It.IsAny<IReadOnlyList<DomainEvent>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
