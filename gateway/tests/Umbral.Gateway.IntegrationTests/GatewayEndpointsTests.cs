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
        // /partidas carries an explicit AuthorizationPolicy ("GestionarPartidas"): no token → 401,
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
    public async Task IdentityTeamsListing_GET_con_Operador_con_GestionarEquipos_pasa_la_politica()
    {
        var client = CreateClientWithRoles("Operador,GestionarEquipos");
        var response = await client.GetAsync("/identity/teams");
        AssertPolicyPassed(response);
    }

    [Fact]
    public async Task IdentityTeamsListing_GET_con_Participante_con_GestionarEquipos_pasa_la_politica()
    {
        // El caso nuevo: antes esta misma ruta, con el mismo Participante, era 403 (test de abajo).
        var client = CreateClientWithRoles("Participante,GestionarEquipos");
        var response = await client.GetAsync("/identity/teams");
        AssertPolicyPassed(response);
    }

    [Fact]
    public async Task IdentityTeamsListing_GET_con_Participante_es_403()
    {
        // El listado es de la web (admin/operador); un participante puro no pasa.
        var client = CreateClientWithRoles("Participante");
        var response = await client.GetAsync("/identity/teams");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task IdentityTeamsListing_POST_sigue_siendo_de_Participante()
    {
        // La ruta nueva solo matchea GET: crear equipo cae en la ruta Participante intacta.
        var client = CreateClientWithRoles("Participante");
        var response = await client.PostAsync("/identity/teams", new StringContent("{}"));
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
    public async Task Partidas_con_Administrador_sin_privilegio_es_403()
    {
        var client = CreateClientWithRoles("Administrador");
        var response = await client.GetAsync("/partidas/anything");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Partidas_con_Participante_con_GestionarPartidas_pasa_la_politica()
    {
        // El caso nuevo: privilegio-sin-rol, un Participante con el privilegio ya no es 403.
        var client = CreateClientWithRoles("Participante,GestionarPartidas");
        var response = await client.GetAsync("/partidas/anything");
        AssertPolicyPassed(response);
    }

    [Fact]
    public async Task Partidas_con_Operador_con_GestionarPartidas_pasa_la_politica()
    {
        var client = CreateClientWithRoles("Operador,GestionarPartidas");
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

    [Fact]
    public async Task Preflight_cors_desde_origen_permitido_pasa()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Options, "/identity/users");
        request.Headers.Add("Origin", "http://localhost:5173");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        var response = await client.SendAsync(request);

        // El middleware CORS responde el preflight ANTES de la política de autorización.
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("http://localhost:5173",
            Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
    }

    [Fact]
    public async Task Respuesta_con_origin_lleva_un_solo_allow_origin()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("Origin", "http://localhost:5173");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("http://localhost:5173",
            Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
    }

    [Fact]
    public async Task IdentityAdminTeams_sin_token_es_401()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/identity/admin/teams");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task IdentityAdminTeams_con_Participante_es_403()
    {
        // Sin la ruta explícita, /identity/admin/teams caía en el catch-all identity (policy
        // Default = cualquier autenticado); este 403 pinnea la RBAC gruesa Administrador-only.
        var client = CreateClientWithRoles("Participante");
        var response = await client.GetAsync("/identity/admin/teams");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task IdentityAdminTeams_con_Operador_es_403()
    {
        var client = CreateClientWithRoles("Operador");
        var response = await client.GetAsync("/identity/admin/teams");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task IdentityAdminTeams_con_Administrador_sin_privilegio_es_403()
    {
        var client = CreateClientWithRoles("Administrador");
        var response = await client.GetAsync("/identity/admin/teams/cualquier-cosa");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task IdentityAdminTeams_con_Participante_con_GestionarEquipos_pasa_la_politica()
    {
        var client = CreateClientWithRoles("Participante,GestionarEquipos");
        var response = await client.GetAsync("/identity/admin/teams/cualquier-cosa");
        AssertPolicyPassed(response);
    }

    // S6: separar la LECTURA de listados (dropdowns de la web) de las MUTACIONES.

    [Fact]
    public async Task IdentityUsersListing_GET_con_GestionarEquipos_pasa_la_politica()
    {
        // El dropdown de líder (TeamsAdminPage) lee el directorio de usuarios; un portador de
        // GestionarEquipos que no es Administrador debe poder leerlo.
        var client = CreateClientWithRoles("Participante,GestionarEquipos");
        var response = await client.GetAsync("/identity/users");
        AssertPolicyPassed(response);
    }

    [Fact]
    public async Task IdentityUsersListing_GET_con_Operador_es_403()
    {
        // El directorio de usuarios NO se abre al Operador simple: solo Administrador/GestionarEquipos.
        var client = CreateClientWithRoles("Operador");
        var response = await client.GetAsync("/identity/users");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task IdentityUsers_POST_con_GestionarEquipos_sigue_403()
    {
        // Solo se amplió el GET del listado: crear/editar usuario sigue Administrador-only.
        var client = CreateClientWithRoles("GestionarEquipos");
        var response = await client.PostAsync("/identity/users", new StringContent("{}"));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task IdentityAdminTeamsListing_GET_con_GestionarPartidas_pasa_la_politica()
    {
        // El dropdown de rendimiento (M6) lista equipos; el Operador porta GestionarPartidas.
        var client = CreateClientWithRoles("Operador,GestionarPartidas");
        var response = await client.GetAsync("/identity/admin/teams");
        AssertPolicyPassed(response);
    }

    [Fact]
    public async Task IdentityAdminTeams_POST_con_GestionarPartidas_sigue_403()
    {
        // Solo se amplió el GET del listado: crear/renombrar/borrar equipo sigue GestionarEquipos-only.
        var client = CreateClientWithRoles("Operador,GestionarPartidas");
        var response = await client.PostAsync("/identity/admin/teams", new StringContent("{}"));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
