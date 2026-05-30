using Umbral.TriviaGame.Domain.Common;
using Umbral.TriviaGame.Domain.Drafts;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Domain.Tests.ValueObjects;

public class AnswerOptionTests
{
    [Fact]
    public void Create_WithValidOptionText_ReturnsInstance()
    {
        var text = OptionText.Create("París");
        var option = AnswerOption.Create(text, isCorrect: true);

        Assert.Equal("París", option.Text.Value);
        Assert.True(option.IsCorrect);
    }

    [Fact]
    public void Create_WithNullOptionText_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() => AnswerOption.Create(null!, false));
    }

    [Fact]
    public void FromDraft_WithValidDraft_ReturnsEquivalentOption()
    {
        var draft = AnswerOptionDraft.Create("Londres", isCorrect: false);
        var option = AnswerOption.FromDraft(draft);

        Assert.Equal("Londres", option.Text.Value);
        Assert.False(option.IsCorrect);
    }

    [Fact]
    public void Equals_WithSameContent_AreEqual()
    {
        var left = AnswerOption.Create(OptionText.Create("A"), true);
        var right = AnswerOption.Create(OptionText.Create("A"), true);
        Assert.Equal(left, right);
    }
}
