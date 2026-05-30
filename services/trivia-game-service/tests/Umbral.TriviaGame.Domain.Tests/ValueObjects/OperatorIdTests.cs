using Umbral.TriviaGame.Domain.Common;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Domain.Tests.ValueObjects;

public class OperatorIdTests
{
    [Fact]
    public void Create_WithValidValue_TrimsValue()
    {
        var operatorId = OperatorId.Create("  keycloak-sub-123  ");
        Assert.Equal("keycloak-sub-123", operatorId.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyValue_ThrowsDomainValidationException(string? value)
    {
        Assert.Throws<DomainValidationException>(() => OperatorId.Create(value!));
    }

    [Fact]
    public void Create_WhenExceedsMaxLength_ThrowsDomainValidationException()
    {
        var value = new string('o', OperatorId.MaxLength + 1);
        Assert.Throws<DomainValidationException>(() => OperatorId.Create(value));
    }
}
