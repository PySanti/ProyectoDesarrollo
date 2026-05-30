using Umbral.TriviaGame.Domain.Common;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Domain.Tests.ValueObjects;

public class FormTitleTests
{
    [Fact]
    public void Create_WithValidTitle_TrimsValue()
    {
        var title = FormTitle.Create("  Trivia de demo  ");
        Assert.Equal("Trivia de demo", title.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyTitle_ThrowsDomainValidationException(string? value)
    {
        Assert.Throws<DomainValidationException>(() => FormTitle.Create(value!));
    }

    [Fact]
    public void Create_WhenExceedsMaxLength_ThrowsDomainValidationException()
    {
        var value = new string('a', FormTitle.MaxLength + 1);
        Assert.Throws<DomainValidationException>(() => FormTitle.Create(value));
    }
}
