using Umbral.IdentityService.Infrastructure.Services.Security;

namespace Umbral.IdentityService.UnitTests;

public sealed class TemporaryPasswordGeneratorTests
{
    private readonly CryptoTemporaryPasswordGenerator _generator = new();

    [Fact]
    public void Generate_Should_Produce_Strong_Password()
    {
        var password = _generator.Generate();

        Assert.Equal(16, password.Length);
        Assert.Contains(password, char.IsLower);
        Assert.Contains(password, char.IsUpper);
        Assert.Contains(password, char.IsDigit);
        Assert.Contains(password, c => !char.IsLetterOrDigit(c));
    }

    [Fact]
    public void Generate_Should_Produce_Unique_Passwords()
    {
        var passwords = Enumerable.Range(0, 50).Select(_ => _generator.Generate()).ToHashSet();

        // Collisions across 50 strong 16-char passwords are effectively impossible.
        Assert.Equal(50, passwords.Count);
    }
}
