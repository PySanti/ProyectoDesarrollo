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

public class UpdateTriviaFormCommandHandlerTests
{
    private readonly Mock<ITriviaFormRepository> _repoMock = new();
    private readonly Mock<IDomainEventDispatcher> _eventMock = new();
    private readonly UpdateTriviaFormCommandHandler _handler;

    public UpdateTriviaFormCommandHandlerTests()
    {
        _handler = new UpdateTriviaFormCommandHandler(_repoMock.Object, _eventMock.Object);
    }

    private static TriviaForm CreateExistingForm()
    {
        return TriviaForm.Create(
            FormTitle.Create("Original Title"),
            OperatorId.Create("operator-1"),
            new List<QuestionDraft>
            {
                QuestionDraft.Create("Q1", 10, 30, 1,
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
    public async Task Handle_FormExists_UpdatesAndReturnsDto()
    {
        var existingForm = CreateExistingForm();
        var formId = existingForm.Id.Value;

        _repoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<TriviaFormId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingForm);

        var cmd = new UpdateTriviaFormCommand(
            formId,
            "Updated Title",
            new List<QuestionInputDto>
            {
                new("Updated Q", 20, 60, 1,
                    new List<AnswerOptionInputDto>
                    {
                        new("X", true),
                        new("Y", false),
                        new("Z", false),
                        new("W", false),
                    }),
            });

        var result = await _handler.Handle(cmd, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Updated Title", result.Title);
        Assert.Single(result.Questions);
        Assert.Equal("Updated Q", result.Questions[0].Text);
        Assert.Equal(20, result.Questions[0].AssignedScore);

        _repoMock.Verify(r => r.UpdateAsync(existingForm, It.IsAny<CancellationToken>()), Times.Once);
        _eventMock.Verify(e => e.DispatchAsync(It.IsAny<IReadOnlyList<DomainEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_FormNotFound_ThrowsNotFoundException()
    {
        var formId = Guid.NewGuid();

        _repoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<TriviaFormId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TriviaForm?)null);

        var cmd = new UpdateTriviaFormCommand(
            formId,
            "Title",
            new List<QuestionInputDto>
            {
                new("Q", 10, 30, 1,
                    new List<AnswerOptionInputDto>
                    {
                        new("A", true),
                        new("B", false),
                        new("C", false),
                        new("D", false),
                    }),
            });

        var ex = await Assert.ThrowsAsync<TriviaFormNotFoundException>(
            () => _handler.Handle(cmd, CancellationToken.None));

        Assert.Equal(formId, ex.FormId);
    }
}
