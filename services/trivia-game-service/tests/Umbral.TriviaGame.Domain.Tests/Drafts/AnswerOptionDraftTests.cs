using Umbral.TriviaGame.Domain.Common;
using Umbral.TriviaGame.Domain.Drafts;

namespace Umbral.TriviaGame.Domain.Tests.Drafts;

public class AnswerOptionDraftTests
{
    [Fact]
    public void Create_WithValidText_TrimsValue()
    {
        var draft = AnswerOptionDraft.Create("  Berlín  ", isCorrect: true);
        Assert.Equal("Berlín", draft.Text);
        Assert.True(draft.IsCorrect);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyText_ThrowsDomainValidationException(string? text)
    {
        Assert.Throws<DomainValidationException>(() => AnswerOptionDraft.Create(text!, false));
    }

    [Fact]
    public void ToAnswerOption_MaterializesValueObject()
    {
        var draft = AnswerOptionDraft.Create("Madrid", isCorrect: false);
        var option = draft.ToAnswerOption(orden: 0);
        Assert.Equal("Madrid", option.Text.Value);
    }
}
