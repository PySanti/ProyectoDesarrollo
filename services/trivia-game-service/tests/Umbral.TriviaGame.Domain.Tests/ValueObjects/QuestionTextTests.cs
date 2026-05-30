using Umbral.TriviaGame.Domain.Common;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Domain.Tests.ValueObjects;

public class QuestionTextTests
{
    [Fact]
    public void Create_WithValidText_TrimsValue()
    {
        var text = QuestionText.Create("  ¿Cuál es la capital?  ");
        Assert.Equal("¿Cuál es la capital?", text.Value);
    }

    [Fact]
    public void Create_WithEmptyText_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() => QuestionText.Create(" "));
    }

    [Fact]
    public void Create_WhenExceedsMaxLength_ThrowsDomainValidationException()
    {
        var value = new string('x', QuestionText.MaxLength + 1);
        Assert.Throws<DomainValidationException>(() => QuestionText.Create(value));
    }
}
