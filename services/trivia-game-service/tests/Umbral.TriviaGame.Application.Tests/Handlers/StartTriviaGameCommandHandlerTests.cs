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

public class StartTriviaGameCommandHandlerTests
{
    private readonly Mock<IPartidaTriviaRepository> _partidaRepoMock = new();
    private readonly Mock<ITriviaFormRepository> _formRepoMock = new();
    private readonly Mock<IDomainEventDispatcher> _eventMock = new();
    private readonly Mock<ITriviaLobbyNotifier> _notifierMock = new();
    private readonly StartTriviaGameCommandHandler _handler;

    public StartTriviaGameCommandHandlerTests()
    {
        _handler = new StartTriviaGameCommandHandler(
            _partidaRepoMock.Object,
            _formRepoMock.Object,
            _eventMock.Object,
            _notifierMock.Object);
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

    private static PartidaTrivia CreatePartidaEnLobby(CantidadMinima? minimo = null, ModoInicio modoInicio = ModoInicio.Manual)
    {
        var min = minimo ?? CantidadMinima.Create(2);
        return PartidaTrivia.Create(
            NombrePartida.Create("Test"),
            Modalidad.Individual,
            modoInicio,
            TriviaFormId.New(),
            OperatorId.Create("op-1"),
            TiempoInicio.Create(DateTimeOffset.UtcNow.AddDays(1)),
            min,
            maximoJugadores: CantidadMaximaJugadores.Create(10, min),
            maximoEquipos: null,
            minJugadoresPorEquipo: null,
            maxJugadoresPorEquipo: null);
    }

    private TriviaForm SetupForm()
    {
        var form = CreateFormWithOneQuestion();
        _formRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<TriviaFormId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(form);
        return form;
    }

    [Fact]
    public async Task Handle_PartidaExistsAndMinimosCumplidos_ReturnsDtoConEstadoIniciada()
    {
        var partida = CreatePartidaEnLobby();
        var partidaId = partida.Id.Value;
        SetupForm();

        _partidaRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(partida);
        _partidaRepoMock
            .Setup(r => r.CountInscripcionesAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var cmd = new StartTriviaGameCommand(partidaId);
        var result = await _handler.Handle(cmd, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Iniciada", result.Estado);
        Assert.NotNull(result.StartedAtUtc);

        _partidaRepoMock.Verify(r => r.UpdateAsync(partida, It.IsAny<CancellationToken>()), Times.Once);
        _eventMock.Verify(e => e.DispatchAsync(It.IsAny<IReadOnlyList<DomainEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
        _notifierMock.Verify(n => n.NotifyGameStarted(partidaId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PartidaNotFound_ThrowsPartidaTriviaNotFoundException()
    {
        _partidaRepoMock
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
        SetupForm();

        _partidaRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(partida);
        _partidaRepoMock
            .Setup(r => r.CountInscripcionesAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var cmd = new StartTriviaGameCommand(partidaId);

        await Assert.ThrowsAsync<MinimosNoCumplidosException>(
            () => _handler.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_PartidaYaIniciada_ThrowsInvalidStateTransitionException()
    {
        var form = CreateFormWithOneQuestion();
        var min = CantidadMinima.Create(2);
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
        var questionId = form.Questions.First().Id;
        partida.Iniciar(cantidadInscriptos: 2, esInicioManual: true, primeraPreguntaId: questionId);
        var partidaId = partida.Id.Value;

        _formRepoMock
            .Setup(r => r.GetByIdAsync(form.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(form);
        _partidaRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(partida);
        _partidaRepoMock
            .Setup(r => r.CountInscripcionesAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var cmd = new StartTriviaGameCommand(partidaId);

        await Assert.ThrowsAsync<InvalidStateTransitionException>(
            () => _handler.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ModoInicioAutomatico_ThrowsModoInicioAutomaticoException()
    {
        var partida = CreatePartidaEnLobby(modoInicio: ModoInicio.Automatico);
        var partidaId = partida.Id.Value;
        SetupForm();

        _partidaRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(partida);
        _partidaRepoMock
            .Setup(r => r.CountInscripcionesAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var cmd = new StartTriviaGameCommand(partidaId);

        await Assert.ThrowsAsync<ModoInicioAutomaticoException>(
            () => _handler.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ManualYAutomatico_AllowsStart()
    {
        var partida = CreatePartidaEnLobby(minimo: CantidadMinima.Create(2), modoInicio: ModoInicio.ManualYAutomatico);
        var partidaId = partida.Id.Value;
        SetupForm();

        _partidaRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(partida);
        _partidaRepoMock
            .Setup(r => r.CountInscripcionesAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var cmd = new StartTriviaGameCommand(partidaId);
        var result = await _handler.Handle(cmd, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Iniciada", result.Estado);
        Assert.NotNull(result.StartedAtUtc);

        _partidaRepoMock.Verify(r => r.UpdateAsync(partida, It.IsAny<CancellationToken>()), Times.Once);
        _eventMock.Verify(e => e.DispatchAsync(It.IsAny<IReadOnlyList<DomainEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
        _notifierMock.Verify(n => n.NotifyGameStarted(partidaId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_FormNotFound_ThrowsDomainValidationException()
    {
        var partida = CreatePartidaEnLobby();
        var partidaId = partida.Id.Value;

        _partidaRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(partida);
        _partidaRepoMock
            .Setup(r => r.CountInscripcionesAsync(It.IsAny<PartidaId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        _formRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<TriviaFormId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TriviaForm?)null);

        var cmd = new StartTriviaGameCommand(partidaId);

        await Assert.ThrowsAsync<DomainValidationException>(
            () => _handler.Handle(cmd, CancellationToken.None));
    }
}
