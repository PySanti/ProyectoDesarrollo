using Umbral.TriviaGame.Domain.Common;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Domain.Tests.ValueObjects;

public class QuestionIdTests
{
    [Fact]
    public void Create_WithValidGuid_ReturnsInstance()
    {
        var id = Guid.NewGuid();
        var questionId = QuestionId.Create(id);
        Assert.Equal(id, questionId.Value);
    }

    [Fact]
    public void Create_WithEmptyGuid_ThrowsDomainValidationException()
    {
        var exception = Assert.Throws<DomainValidationException>(() => QuestionId.Create(Guid.Empty));
        Assert.Contains("pregunta", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
