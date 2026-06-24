using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Umbral.Gateway.IntegrationTests;

public class GatewayEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public GatewayEndpointsTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Health_is_anonymous_and_returns_200()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Explicit_policy_route_without_token_is_401()
    {
        // /partidas carries an explicit AuthorizationPolicy ("Operador"): no token → 401,
        // enforced before the proxy ever contacts the (unreachable) destination.
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/partidas/anything");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Default_policy_route_without_token_is_401()
    {
        // /puntuaciones uses AuthorizationPolicy "Default" (YARP's reserved word → the application
        // default policy = RequireAuthenticatedUser). No token → 401. This pins the reserved-word
        // contract that the three "Default" routes depend on; a config typo (unknown policy name)
        // would not 401 and this test would catch it.
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/puntuaciones/anything");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Fallback_policy_is_fail_secure()
    {
        // Directly proves the fail-secure FLOOR: any route LACKING an explicit AuthorizationPolicy
        // inherits this fallback, which denies anonymous access. This assertion fails iff
        // SetFallbackPolicy(RequireAuthenticatedUser) is removed — independent of any route policy.
        using var scope = _factory.Services.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IAuthorizationPolicyProvider>();

        var fallback = await provider.GetFallbackPolicyAsync();

        Assert.NotNull(fallback);
        Assert.Contains(fallback!.Requirements, r => r is DenyAnonymousAuthorizationRequirement);
    }
}
