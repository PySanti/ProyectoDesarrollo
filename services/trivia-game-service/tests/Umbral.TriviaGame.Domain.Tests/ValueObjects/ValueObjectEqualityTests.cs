using Umbral.TriviaGame.Domain.Common;
using Umbral.TriviaGame.Domain.ValueObjects;

namespace Umbral.TriviaGame.Domain.Tests.ValueObjects;

public class ValueObjectEqualityTests
{
    [Fact]
    public void FormTitle_WithSameValue_AreEqual()
    {
        var left = FormTitle.Create("Demo");
        var right = FormTitle.Create("Demo");
        Assert.True(left == right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void FormTitle_WithDifferentValue_AreNotEqual()
    {
        var left = FormTitle.Create("Demo A");
        var right = FormTitle.Create("Demo B");
        Assert.True(left != right);
    }
}
