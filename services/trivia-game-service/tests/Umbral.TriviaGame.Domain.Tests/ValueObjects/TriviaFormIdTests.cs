using Umbral.TriviaGame.Domain.Common;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Domain.Tests.ValueObjects;

public class TriviaFormIdTests
{
    [Fact]
    public void Create_WithValidGuid_ReturnsInstance()
    {
        var id = Guid.NewGuid();
        var formId = TriviaFormId.Create(id);
        Assert.Equal(id, formId.Value);
    }

    [Fact]
    public void Create_WithEmptyGuid_ThrowsDomainValidationException()
    {
        var exception = Assert.Throws<DomainValidationException>(() => TriviaFormId.Create(Guid.Empty));
        Assert.Contains("formulario", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Equals_WithSameValue_ReturnsTrue()
    {
        var id = Guid.NewGuid();
        Assert.Equal(TriviaFormId.Create(id), TriviaFormId.Create(id));
    }
}
