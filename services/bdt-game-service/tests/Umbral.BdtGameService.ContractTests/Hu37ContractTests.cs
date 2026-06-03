using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Umbral.BdtGameService.Domain.Entities;
using Umbral.BdtGameService.Domain.Enums;
using Umbral.BdtGameService.Domain.ValueObjects;
using Umbral.BdtGameService.Infrastructure.Persistence;

namespace Umbral.BdtGameService.ContractTests;

public sealed class Hu37ContractTests : IClassFixture<BdtApiFactory>
{
    private readonly BdtApiFactory _factory;
    private readonly HttpClient _client;

    public Hu37ContractTests(BdtApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetOperatorPublished_Should_Match_Hu37_Response_Shape()
    {
        await ClearDatabaseAsync();
        await SeedAsync(PartidaBDT.CrearPublicada("BDT publicada", Modalidad.Individual, new AreaBusqueda("Area"), OneStage()));

        var response = await _client.SendAsync(CreateGetRequest());
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(body);
        var item = Assert.Single(document.RootElement.EnumerateArray());
        Assert.True(item.TryGetProperty("partidaId", out _));
        Assert.True(item.TryGetProperty("nombre", out _));
        Assert.True(item.TryGetProperty("modalidad", out _));
        Assert.True(item.TryGetProperty("estado", out _));
        Assert.True(item.TryGetProperty("areaBusqueda", out _));
        Assert.True(item.TryGetProperty("cantidadEtapas", out _));
    }

    [Fact]
    public async Task GetOperatorPublished_Should_Match_Unauthenticated_Contract_Status()
    {
        var response = await _client.GetAsync("/api/bdt/operator/games/published");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetOperatorPublished_Should_Match_Forbidden_Contract_Status()
    {
        var response = await _client.SendAsync(CreateGetRequest(role: "Participante"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static HttpRequestMessage CreateGetRequest(string role = "Operador")
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/bdt/operator/games/published");
        request.Headers.Add("X-Test-Role", role);
        request.Headers.Add("X-Test-UserId", Guid.NewGuid().ToString());
        return request;
    }

    private async Task ClearDatabaseAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BdtDbContext>();
        dbContext.Partidas.RemoveRange(dbContext.Partidas);
        await dbContext.SaveChangesAsync();
    }

    private async Task SeedAsync(params PartidaBDT[] partidas)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BdtDbContext>();
        await dbContext.Partidas.AddRangeAsync(partidas);
        await dbContext.SaveChangesAsync();
    }

    private static EtapaBDT[] OneStage() => new[] { EtapaBDT.Crear(1, "QR-1", 60) };
}
