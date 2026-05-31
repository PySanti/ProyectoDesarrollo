using Moq;
using Umbral.TriviaGame.Application.Commands;
using Umbral.TriviaGame.Application.Dtos;
using Umbral.TriviaGame.Application.Handlers;
using Umbral.TriviaGame.Application.Ports;
using Umbral.TriviaGame.Domain.Common;
using Umbral.TriviaGame.Domain.Common.Exceptions;
using Umbral.TriviaGame.Domain.Drafts;
using Umbral.TriviaGame.Domain.Entities;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Application.Tests.Handlers;

public class CreateTriviaGameCommandHandlerTests
{
    private readonly Mock<IPartidaTriviaRepository> _partidaRepoMock = new();
    private readonly Mock<ITriviaFormRepository> _formRepoMock = new();
    private readonly Mock<ICurrentUserService> _userMock = new();
    private readonly Mock<IDomainEventDispatcher> _eventMock = new();
    private readonly CreateTriviaGameCommandHandler _handler;

    public CreateTriviaGameCommandHandlerTests()
    {
        _userMock.Setup(u => u.OperatorId).Returns("operator-123");
        _handler = new CreateTriviaGameCommandHandler(
            _partidaRepoMock.Object, _formRepoMock.Object, _userMock.Object, _eventMock.Object);
    }

    private static TriviaForm CreateCompleteForm()
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

    [Fact]
    public async Task Handle_ValidIndividualCommand_CreatesAndPersists()
    {
        var form = CreateCompleteForm();
        _formRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<TriviaFormId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(form);

        var cmd = new CreateTriviaGameCommand(
            Nombre: "Trivia Demo",
            Modalidad: "Individual",
            ModoInicio: "Manual",
            FormularioId: form.Id.Value,
            TiempoInicio: DateTimeOffset.UtcNow.AddDays(1),
            MinimoParticipantes: 2,
            MaximoJugadores: 10,
            MaximoEquipos: null,
            MinimoJugadoresPorEquipo: null,
            MaximoJugadoresPorEquipo: null);

        PartidaTrivia? captured = null;
        _partidaRepoMock
            .Setup(r => r.AddAsync(It.IsAny<PartidaTrivia>(), It.IsAny<CancellationToken>()))
            .Callback<PartidaTrivia, CancellationToken>((p, _) => captured = p);

        var result = await _handler.Handle(cmd, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Trivia Demo", result.Nombre);
        Assert.Equal("Lobby", result.Estado);
        Assert.Equal("Individual", result.Modalidad);
        Assert.Equal(10, result.MaximoJugadores);
        Assert.Null(result.MaximoEquipos);

        _partidaRepoMock.Verify(r => r.AddAsync(It.IsAny<PartidaTrivia>(), It.IsAny<CancellationToken>()), Times.Once);
        _eventMock.Verify(e => e.DispatchAsync(It.IsAny<IReadOnlyList<DomainEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidEquipoCommand_CreatesAndPersists()
    {
        var form = CreateCompleteForm();
        _formRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<TriviaFormId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(form);

        var cmd = new CreateTriviaGameCommand(
            Nombre: "Trivia Equipo",
            Modalidad: "Equipo",
            ModoInicio: "Automatico",
            FormularioId: form.Id.Value,
            TiempoInicio: DateTimeOffset.UtcNow.AddDays(1),
            MinimoParticipantes: 2,
            MaximoJugadores: null,
            MaximoEquipos: 5,
            MinimoJugadoresPorEquipo: 2,
            MaximoJugadoresPorEquipo: 5);

        PartidaTrivia? captured = null;
        _partidaRepoMock
            .Setup(r => r.AddAsync(It.IsAny<PartidaTrivia>(), It.IsAny<CancellationToken>()))
            .Callback<PartidaTrivia, CancellationToken>((p, _) => captured = p);

        var result = await _handler.Handle(cmd, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Trivia Equipo", result.Nombre);
        Assert.Equal("Lobby", result.Estado);
        Assert.Equal("Equipo", result.Modalidad);
        Assert.Equal("Automatico", result.ModoInicio);
        Assert.Equal(5, result.MaximoEquipos);
        Assert.Equal(2, result.MinimoJugadoresPorEquipo);
        Assert.Equal(5, result.MaximoJugadoresPorEquipo);
        Assert.Null(result.MaximoJugadores);

        _partidaRepoMock.Verify(r => r.AddAsync(It.IsAny<PartidaTrivia>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_FormNotFound_ThrowsTriviaFormNotFoundException()
    {
        _formRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<TriviaFormId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TriviaForm?)null);

        var cmd = new CreateTriviaGameCommand(
            Nombre: "Test",
            Modalidad: "Individual",
            ModoInicio: "Manual",
            FormularioId: Guid.NewGuid(),
            TiempoInicio: DateTimeOffset.UtcNow.AddDays(1),
            MinimoParticipantes: 2,
            MaximoJugadores: 10,
            MaximoEquipos: null,
            MinimoJugadoresPorEquipo: null,
            MaximoJugadoresPorEquipo: null);

        await Assert.ThrowsAsync<TriviaFormNotFoundException>(
            () => _handler.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_FormIncomplete_ThrowsFormularioIncompletoException()
    {
        // Se crea un formulario con opciones duplicadas ("A", "A").
        // TriviaForm.Create y Question.Create permiten la creación porque validan
        // cantidad de opciones (4) y exactamente 1 correcta, pero NO validan textos
        // duplicados a nivel de factory. La validación de duplicados es responsabilidad
        // de TriviaFormCompletenessValidator.IsComplete, que retorna false en este caso.
        var incompleteForm = TriviaForm.Create(
            FormTitle.Create("Incomplete"),
            OperatorId.Create("op-1"),
            new List<QuestionDraft>
            {
                QuestionDraft.Create("Q1 with duplicate options", 100, 30, 1,
                    new List<AnswerOptionDraft>
                    {
                        AnswerOptionDraft.Create("A", true),
                        AnswerOptionDraft.Create("A", false),
                        AnswerOptionDraft.Create("B", false),
                        AnswerOptionDraft.Create("C", false),
                    }),
            });

        _formRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<TriviaFormId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(incompleteForm);

        var cmd = new CreateTriviaGameCommand(
            Nombre: "Test",
            Modalidad: "Individual",
            ModoInicio: "Manual",
            FormularioId: incompleteForm.Id.Value,
            TiempoInicio: DateTimeOffset.UtcNow.AddDays(1),
            MinimoParticipantes: 2,
            MaximoJugadores: 10,
            MaximoEquipos: null,
            MinimoJugadoresPorEquipo: null,
            MaximoJugadoresPorEquipo: null);

        await Assert.ThrowsAsync<FormularioIncompletoException>(
            () => _handler.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ValidCommand_SetsOperatorFromCurrentUser()
    {
        var form = CreateCompleteForm();
        _formRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<TriviaFormId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(form);

        _userMock.Setup(u => u.OperatorId).Returns("operator-789");

        var cmd = new CreateTriviaGameCommand(
            Nombre: "Test",
            Modalidad: "Individual",
            ModoInicio: "Manual",
            FormularioId: form.Id.Value,
            TiempoInicio: DateTimeOffset.UtcNow.AddDays(1),
            MinimoParticipantes: 2,
            MaximoJugadores: 10,
            MaximoEquipos: null,
            MinimoJugadoresPorEquipo: null,
            MaximoJugadoresPorEquipo: null);

        PartidaTrivia? captured = null;
        _partidaRepoMock
            .Setup(r => r.AddAsync(It.IsAny<PartidaTrivia>(), It.IsAny<CancellationToken>()))
            .Callback<PartidaTrivia, CancellationToken>((p, _) => captured = p);

        await _handler.Handle(cmd, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal("operator-789", captured.CreatedByOperatorId.Value);
    }

    [Fact]
    public async Task Handle_ValidCommand_PartidaEnLobby()
    {
        var form = CreateCompleteForm();
        _formRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<TriviaFormId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(form);

        var cmd = new CreateTriviaGameCommand(
            Nombre: "Test",
            Modalidad: "Individual",
            ModoInicio: "Manual",
            FormularioId: form.Id.Value,
            TiempoInicio: DateTimeOffset.UtcNow.AddDays(1),
            MinimoParticipantes: 2,
            MaximoJugadores: 10,
            MaximoEquipos: null,
            MinimoJugadoresPorEquipo: null,
            MaximoJugadoresPorEquipo: null);

        PartidaTrivia? captured = null;
        _partidaRepoMock
            .Setup(r => r.AddAsync(It.IsAny<PartidaTrivia>(), It.IsAny<CancellationToken>()))
            .Callback<PartidaTrivia, CancellationToken>((p, _) => captured = p);

        await _handler.Handle(cmd, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal("Lobby", captured.Estado.ToString());
    }
}
