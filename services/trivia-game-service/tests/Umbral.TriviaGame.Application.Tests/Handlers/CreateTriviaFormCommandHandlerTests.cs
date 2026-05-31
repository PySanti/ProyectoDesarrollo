using Moq;
using Umbral.TriviaGame.Application.Commands;
using Umbral.TriviaGame.Application.Dtos;
using Umbral.TriviaGame.Application.Handlers;
using Umbral.TriviaGame.Application.Ports;
using Umbral.TriviaGame.Domain.Common;
using Umbral.TriviaGame.Domain.Entities;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Application.Tests.Handlers;

public class CreateTriviaFormCommandHandlerTests
{
    private readonly Mock<ITriviaFormRepository> _repoMock = new();
    private readonly Mock<ICurrentUserService> _userMock = new();
    private readonly Mock<IDomainEventDispatcher> _eventMock = new();
    private readonly CreateTriviaFormCommandHandler _handler;

    private static readonly Guid SubmittedFormId = Guid.NewGuid();

    public CreateTriviaFormCommandHandlerTests()
    {
        _userMock.Setup(u => u.OperatorId).Returns("operator-123");
        _handler = new CreateTriviaFormCommandHandler(
            _repoMock.Object, _userMock.Object, _eventMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesAndPersistsForm()
    {
        var cmd = new CreateTriviaFormCommand(
            "My Form",
            new List<QuestionInputDto>
            {
                new("Q1", 10, 30, 1,
                    new List<AnswerOptionInputDto>
                    {
                        new("A", true),
                        new("B", false),
                        new("C", false),
                        new("D", false),
                    }),
            });

        TriviaForm? capturedForm = null;
        _repoMock
            .Setup(r => r.AddAsync(It.IsAny<TriviaForm>(), It.IsAny<CancellationToken>()))
            .Callback<TriviaForm, CancellationToken>((f, _) => capturedForm = f);

        var result = await _handler.Handle(cmd, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("My Form", result.Title);
        Assert.True(result.IsComplete);
        Assert.Single(result.Questions);
        Assert.Equal("Q1", result.Questions[0].Text);
        Assert.Equal(4, result.Questions[0].Options.Count);

        _repoMock.Verify(r => r.AddAsync(It.IsAny<TriviaForm>(), It.IsAny<CancellationToken>()), Times.Once);
        _eventMock.Verify(e => e.DispatchAsync(It.IsAny<IReadOnlyList<DomainEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCommand_SetsCorrectOperator()
    {
        _userMock.Setup(u => u.OperatorId).Returns("operator-456");

        var cmd = new CreateTriviaFormCommand(
            "Form Title",
            new List<QuestionInputDto>
            {
                new("Q1", 10, 30, 1,
                    new List<AnswerOptionInputDto>
                    {
                        new("A", true),
                        new("B", false),
                        new("C", false),
                        new("D", false),
                    }),
            });

        TriviaForm? capturedForm = null;
        _repoMock
            .Setup(r => r.AddAsync(It.IsAny<TriviaForm>(), It.IsAny<CancellationToken>()))
            .Callback<TriviaForm, CancellationToken>((f, _) => capturedForm = f);

        await _handler.Handle(cmd, CancellationToken.None);

        Assert.NotNull(capturedForm);
        Assert.Equal("operator-456", capturedForm.CreatedByOperatorId.Value);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsCompleteReasons()
    {
        var cmd = new CreateTriviaFormCommand(
            "Incomplete Form",
            new List<QuestionInputDto>
            {
                new("Q1", 10, 30, 1,
                    new List<AnswerOptionInputDto>
                    {
                        new("A", true),
                        new("B", false),
                        new("C", false),
                        new("D", false),
                    }),
            });

        var result = await _handler.Handle(cmd, CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.IsComplete);
        Assert.Empty(result.IncompleteReasons);
    }
}
