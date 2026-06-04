using Moq;
using Umbral.TriviaGame.Application.Handlers;
using Umbral.TriviaGame.Application.Ports;
using Umbral.TriviaGame.Application.Queries;
using Umbral.TriviaGame.Domain.Drafts;
using Umbral.TriviaGame.Domain.Entities;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Application.Tests.Handlers;

public class GetTriviaFormByIdQueryHandlerTests
{
    private readonly Mock<ITriviaFormRepository> _repoMock = new();
    private readonly GetTriviaFormByIdQueryHandler _handler;

    public GetTriviaFormByIdQueryHandlerTests()
    {
        _handler = new GetTriviaFormByIdQueryHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_FormExists_ReturnsDto()
    {
        var form = TriviaForm.Create(
            FormTitle.Create("Test Form"),
            OperatorId.Create("op-1"),
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

        var formId = form.Id.Value;

        _repoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<TriviaFormId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(form);

        var query = new GetTriviaFormByIdQuery(formId);
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(formId, result.Id);
        Assert.Equal("Test Form", result.Title);
        Assert.True(result.IsComplete);
        Assert.Single(result.Questions);
    }

    [Fact]
    public async Task Handle_FormNotFound_ReturnsNull()
    {
        var formId = Guid.NewGuid();

        _repoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<TriviaFormId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TriviaForm?)null);

        var query = new GetTriviaFormByIdQuery(formId);
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Null(result);
    }
}
