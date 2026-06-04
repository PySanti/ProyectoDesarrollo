using Umbral.TriviaGame.Domain.Common;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Domain.Tests.ValueObjects;

public class TimeLimitTests
{
    [Theory]
    [InlineData(5)]
    [InlineData(30)]
    [InlineData(300)]
    public void Create_WithValidSeconds_ReturnsInstance(int seconds)
    {
        var timeLimit = TimeLimit.Create(seconds);
        Assert.Equal(seconds, timeLimit.Seconds);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(0)]
    [InlineData(301)]
    public void Create_WithInvalidSeconds_ThrowsDomainValidationException(int seconds)
    {
        var exception = Assert.Throws<DomainValidationException>(() => TimeLimit.Create(seconds));
        Assert.Contains("temporizador", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
