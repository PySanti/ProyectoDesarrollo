using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Umbral.BdtGameService.Domain.Entities;
using Umbral.BdtGameService.Domain.Enums;
using Umbral.BdtGameService.Domain.ValueObjects;
using Umbral.BdtGameService.Infrastructure.Persistence;

namespace Umbral.BdtGameService.ContractTests;

public sealed class Hu10Hu12ContractTests : IClassFixture<BdtApiFactory>
{
    private readonly BdtApiFactory _factory;
    private readonly HttpClient _client;

    public Hu10Hu12ContractTests(BdtApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetPublished_Should_Match_Hu10_Response_Shape()
    {
        await ClearDatabaseAsync();
        await SeedAsync(PartidaBDT.CrearPublicada("BDT publicada", Modalidad.Individual, new AreaBusqueda("Area"), OneStage()));

        var response = await _client.SendAsync(CreateGetRequest("/api/bdt/games/published"));
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
    public async Task GetPublished_Should_Match_Hu12_Filter_Contract()
    {
        await ClearDatabaseAsync();
        await SeedAsync(
            PartidaBDT.CrearPublicada("Individual", Modalidad.Individual, new AreaBusqueda("Area"), OneStage()),
            PartidaBDT.CrearPublicada("Equipo", Modalidad.Equipo, new AreaBusqueda("Area"), OneStage()));

        var response = await _client.SendAsync(CreateGetRequest("/api/bdt/games/published?modalidad=Equipo"));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(body);
        var item = Assert.Single(document.RootElement.EnumerateArray());
        Assert.Equal("Equipo", item.GetProperty("modalidad").GetString());
        Assert.Equal("Lobby", item.GetProperty("estado").GetString());
    }

    [Fact]
    public async Task GetPublished_Should_Match_Invalid_Modality_Contract_Status()
    {
        var response = await _client.SendAsync(CreateGetRequest("/api/bdt/games/published?modalidad=Mixta"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetPublished_Should_Match_Unauthenticated_Contract_Status()
    {
        var response = await _client.GetAsync("/api/bdt/games/published");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static HttpRequestMessage CreateGetRequest(string path, string role = "Participante")
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
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
