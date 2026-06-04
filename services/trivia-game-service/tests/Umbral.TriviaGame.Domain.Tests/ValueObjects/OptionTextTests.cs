using Umbral.TriviaGame.Domain.Common;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Domain.Tests.ValueObjects;

public class OptionTextTests
{
    [Fact]
    public void Create_WithValidText_ReturnsInstance()
    {
        var text = OptionText.Create("París");
        Assert.Equal("París", text.Value);
    }

    [Fact]
    public void Create_WithEmptyText_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() => OptionText.Create(""));
    }

    [Fact]
    public void Create_WhenExceedsMaxLength_ThrowsDomainValidationException()
    {
        var value = new string('o', OptionText.MaxLength + 1);
        Assert.Throws<DomainValidationException>(() => OptionText.Create(value));
    }
}
