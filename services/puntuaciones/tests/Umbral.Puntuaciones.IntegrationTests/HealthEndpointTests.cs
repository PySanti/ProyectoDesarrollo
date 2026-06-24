using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Umbral.Puntuaciones.Infrastructure.Persistence;

namespace Umbral.Puntuaciones.IntegrationTests;

public class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthEndpointTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Health_returns_200()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void DbContext_is_registered()
    {
        using var scope = _factory.Services.CreateScope();

        var db = scope.ServiceProvider.GetService<PuntuacionesDbContext>();

        Assert.NotNull(db);
    }
}
