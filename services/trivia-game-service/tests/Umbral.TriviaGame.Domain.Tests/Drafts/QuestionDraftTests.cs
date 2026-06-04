using Umbral.TriviaGame.Domain.Common;
using Umbral.TriviaGame.Domain.Drafts;

namespace Umbral.TriviaGame.Domain.Tests.Drafts;

public class QuestionDraftTests
{
    private static IReadOnlyList<AnswerOptionDraft> SampleOptions() =>
    [
        AnswerOptionDraft.Create("A", true),
        AnswerOptionDraft.Create("B", false),
        AnswerOptionDraft.Create("C", false),
        AnswerOptionDraft.Create("D", false)
    ];

    [Fact]
    public void Create_WithValidData_TrimsTextAndStoresFields()
    {
        var draft = QuestionDraft.Create(
            "  ¿Capital de Francia?  ",
            assignedScore: 10,
            timeLimitSeconds: 30,
            displayOrder: 1,
            SampleOptions());

        Assert.Equal("¿Capital de Francia?", draft.Text);
        Assert.Equal(10, draft.AssignedScore);
        Assert.Equal(30, draft.TimeLimitSeconds);
        Assert.Equal(1, draft.DisplayOrder);
        Assert.Equal(4, draft.Options.Count);
    }

    [Fact]
    public void Create_WithEmptyText_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() =>
            QuestionDraft.Create(" ", 10, 30, 1, SampleOptions()));
    }

    [Fact]
    public void Create_WithInvalidDisplayOrder_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() =>
            QuestionDraft.Create("Pregunta", 10, 30, 0, SampleOptions()));
    }

    [Fact]
    public void Create_WithNullOptions_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() =>
            QuestionDraft.Create("Pregunta", 10, 30, 1, null));
    }

    [Fact]
    public void ToAnswerOptions_MaterializesAllOptions()
    {
        var draft = QuestionDraft.Create("Pregunta", 10, 30, 1, SampleOptions());
        var options = draft.ToAnswerOptions();

        Assert.Equal(4, options.Count);
        Assert.Single(options, o => o.IsCorrect);
    }

    [Fact]
    public void ToAssignedScore_DelegatesValidation()
    {
        var draft = QuestionDraft.Create("Pregunta", 10, 30, 1, SampleOptions());
        Assert.Equal(10, draft.ToAssignedScore().Value);
    }
}
