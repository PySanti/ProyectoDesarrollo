using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
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
    public async Task Hub_de_operaciones_requiere_autenticacion()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/operaciones-sesion/hubs/sesion");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Hub_de_puntuaciones_requiere_autenticacion()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/puntuaciones/hubs/ranking");

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

    private HttpClient CreateClientWithRoles(string roles)
    {
        var factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { })));
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("X-Test-Roles", roles);
        return client;
    }

    private static void AssertPolicyPassed(HttpResponseMessage response)
    {
        // Destino del cluster muerto: si la política pasó, YARP intenta proxyar → 502/504,
        // nunca 401/403.
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task IdentityUsers_sin_token_es_401()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/identity/users/anything");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task IdentityUsers_con_Participante_es_403_pin_de_precedencia()
    {
        // Si la sub-ruta /identity/users NO ganara sobre /identity/{**} (Default),
        // un Participante autenticado pasaría; el 403 pinnea la precedencia.
        var client = CreateClientWithRoles("Participante");
        var response = await client.GetAsync("/identity/users/anything");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task IdentityUsers_con_Administrador_pasa_la_politica()
    {
        var client = CreateClientWithRoles("Administrador");
        var response = await client.GetAsync("/identity/users/anything");
        AssertPolicyPassed(response);
    }

    [Fact]
    public async Task IdentityTeams_con_Operador_es_403()
    {
        var client = CreateClientWithRoles("Operador");
        var response = await client.GetAsync("/identity/teams/mine");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task IdentityTeams_con_Participante_pasa_la_politica()
    {
        var client = CreateClientWithRoles("Participante");
        var response = await client.GetAsync("/identity/teams/mine");
        AssertPolicyPassed(response);
    }

    [Fact]
    public async Task IdentityResto_autenticado_cualquier_rol_pasa()
    {
        var client = CreateClientWithRoles("Participante");
        var response = await client.GetAsync("/identity/otra-cosa");
        AssertPolicyPassed(response);
    }

    [Fact]
    public async Task Partidas_con_Participante_es_403()
    {
        var client = CreateClientWithRoles("Participante");
        var response = await client.GetAsync("/partidas/anything");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Partidas_con_Administrador_pasa_la_politica()
    {
        var client = CreateClientWithRoles("Administrador");
        var response = await client.GetAsync("/partidas/anything");
        AssertPolicyPassed(response);
    }

    [Fact]
    public async Task Partidas_con_Operador_pasa_la_politica()
    {
        var client = CreateClientWithRoles("Operador");
        var response = await client.GetAsync("/partidas/anything");
        AssertPolicyPassed(response);
    }

    [Fact]
    public async Task IdentityGovernance_sin_token_es_401()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/identity/governance/roles");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task IdentityGovernance_con_Participante_es_403()
    {
        var client = CreateClientWithRoles("Participante");
        var response = await client.GetAsync("/identity/governance/roles");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task IdentityGovernance_con_Administrador_pasa_la_politica()
    {
        var client = CreateClientWithRoles("Administrador");
        var response = await client.GetAsync("/identity/governance/roles");
        AssertPolicyPassed(response);
    }
}
