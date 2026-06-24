using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Umbral.OperacionesSesion.ContractTests;

public class HealthContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthContractTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Health_body_matches_contract()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("healthy", json.RootElement.GetProperty("status").GetString());
        Assert.Equal("operaciones-sesion", json.RootElement.GetProperty("service").GetString());
    }
}
