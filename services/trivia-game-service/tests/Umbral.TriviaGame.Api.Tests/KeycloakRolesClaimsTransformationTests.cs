using System.Security.Claims;
using Umbral.TriviaGame.Api.Services;

namespace Umbral.TriviaGame.Api.Tests;

public sealed class KeycloakRolesClaimsTransformationTests
{
    [Fact]
    public async Task TransformAsync_Should_Add_Normalized_Realm_Roles()
    {
        var principal = BuildPrincipal(new Claim("realm_access", "{\"roles\":[\"operador\"]}"));
        var transformation = new KeycloakRolesClaimsTransformation();

        var transformed = await transformation.TransformAsync(principal);

        Assert.True(transformed.IsInRole("Operador"));
    }

    [Fact]
    public async Task TransformAsync_Should_Add_Normalized_Client_Roles()
    {
        var principal = BuildPrincipal(new Claim("resource_access", "{\"umbral-mobile\":{\"roles\":[\"participante\"]}}"));
        var transformation = new KeycloakRolesClaimsTransformation();

        var transformed = await transformation.TransformAsync(principal);

        Assert.True(transformed.IsInRole("Participante"));
    }

    private static ClaimsPrincipal BuildPrincipal(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, "test", ClaimTypes.NameIdentifier, ClaimTypes.Role);
        return new ClaimsPrincipal(identity);
    }
}
