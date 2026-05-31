using System.Security.Claims;

namespace Umbral.TriviaGame.Api.Tests.Testing;

public sealed class TestClaimsProvider
{
    public IReadOnlyList<Claim> Claims { get; }

    public TestClaimsProvider(IReadOnlyList<Claim> claims)
    {
        Claims = claims;
    }

    public static TestClaimsProvider WithOperadorRole() => new(new[]
    {
        new Claim("sub", "test-operator-0000-0000-0000-000000000001"),
        new Claim(ClaimTypes.Role, "Operador"),
    });

    public static TestClaimsProvider WithoutOperadorRole() => new(new[]
    {
        new Claim("sub", "test-participant-0000-0000-0000-000000000002"),
        new Claim(ClaimTypes.Role, "Participante"),
    });

    public static TestClaimsProvider WithNoClaims() => new(new List<Claim>());
}
