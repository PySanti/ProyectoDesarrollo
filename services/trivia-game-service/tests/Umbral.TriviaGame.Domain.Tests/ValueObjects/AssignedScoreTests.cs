using Umbral.TriviaGame.Domain.Common;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Domain.Tests.ValueObjects;

public class AssignedScoreTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(500)]
    [InlineData(1000)]
    public void Create_WithValidScore_ReturnsInstance(int value)
    {
        var score = AssignedScore.Create(value);
        Assert.Equal(value, score.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1001)]
    public void Create_WithInvalidScore_ThrowsDomainValidationException(int value)
    {
        var exception = Assert.Throws<DomainValidationException>(() => AssignedScore.Create(value));
        Assert.Contains("puntaje", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
