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
        var option = AnswerOption.Create(text, isCorrect: true, orden: 0);

        Assert.Equal("París", option.Text.Value);
        Assert.True(option.IsCorrect);
        Assert.Equal(0, option.Orden);
    }

    [Fact]
    public void Create_WithNullOptionText_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() => AnswerOption.Create(null!, false, orden: 0));
    }

    [Fact]
    public void FromDraft_WithValidDraft_ReturnsEquivalentOption()
    {
        var draft = AnswerOptionDraft.Create("Londres", isCorrect: false);
        var option = AnswerOption.FromDraft(draft, orden: 1);

        Assert.Equal("Londres", option.Text.Value);
        Assert.False(option.IsCorrect);
        Assert.Equal(1, option.Orden);
    }

    [Fact]
    public void Equals_WithSameContent_AreEqual()
    {
        var left = AnswerOption.Create(OptionText.Create("A"), true, orden: 0);
        var right = AnswerOption.Create(OptionText.Create("A"), true, orden: 0);
        Assert.Equal(left, right);
    }

    [Fact]
    public void Equals_WithDifferentOrden_AreNotEqual()
    {
        var left = AnswerOption.Create(OptionText.Create("A"), true, orden: 0);
        var right = AnswerOption.Create(OptionText.Create("A"), true, orden: 1);
        Assert.NotEqual(left, right);
    }
}
