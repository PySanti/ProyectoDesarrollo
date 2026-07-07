using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Umbral.Puntuaciones.ContractTests;

// Host de test con scheme de autenticación de prueba: [Authorize] exige un scheme por defecto y el
// bloque Keycloak de Program.cs no se configura en tests.
public sealed class PuntuacionesWebFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    public HttpClient CreateClientAutenticado()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Sub", Guid.NewGuid().ToString());
        return client;
    }

    public HttpClient CreateClientConRoles(params string[] roles)
    {
        var client = CreateClientAutenticado();
        client.DefaultRequestHeaders.Add("X-Test-Roles", string.Join(",", roles));
        return client;
    }
}
