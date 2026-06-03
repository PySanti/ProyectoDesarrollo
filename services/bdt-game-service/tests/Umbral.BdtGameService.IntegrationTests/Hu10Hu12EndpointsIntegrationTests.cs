using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Umbral.BdtGameService.Domain.Entities;
using Umbral.BdtGameService.Domain.Enums;
using Umbral.BdtGameService.Domain.ValueObjects;
using Umbral.BdtGameService.Infrastructure.Persistence;

namespace Umbral.BdtGameService.IntegrationTests;

public sealed class Hu10Hu12EndpointsIntegrationTests : IClassFixture<BdtApiFactory>
{
    private readonly BdtApiFactory _factory;
    private readonly HttpClient _client;

    public Hu10Hu12EndpointsIntegrationTests(BdtApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetPublished_Should_Return_Unauthorized_When_Unauthenticated()
    {
        var response = await _client.GetAsync("/api/bdt/games/published");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetPublished_Should_Return_Forbidden_For_NonParticipant()
    {
        var request = CreateGetRequest("/api/bdt/games/published", role: "Operador");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetPublished_Should_Return_Empty_List_When_No_Published_Games_Exist()
    {
        await ClearDatabaseAsync();
        await SeedAsync(PartidaBDT.CrearNoPublicada("Iniciada", Modalidad.Individual, new AreaBusqueda("Area"), OneStage(), EstadoPartida.Iniciada));

        var response = await _client.SendAsync(CreateGetRequest("/api/bdt/games/published"));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(body);
        Assert.Equal(JsonValueKind.Array, document.RootElement.ValueKind);
        Assert.Equal(0, document.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task GetPublished_Should_Return_Only_Lobby_Games_For_Hu10()
    {
        await ClearDatabaseAsync();
        await SeedAsync(
            PartidaBDT.CrearPublicada("Individual publicada", Modalidad.Individual, new AreaBusqueda("Area norte"), OneStage()),
            PartidaBDT.CrearPublicada("Equipo publicada", Modalidad.Equipo, new AreaBusqueda("Area sur"), TwoStages()),
            PartidaBDT.CrearNoPublicada("Cancelada", Modalidad.Equipo, new AreaBusqueda("Area este"), OneStage(), EstadoPartida.Cancelada));

        var response = await _client.SendAsync(CreateGetRequest("/api/bdt/games/published"));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(body);
        Assert.Equal(2, document.RootElement.GetArrayLength());
        Assert.All(document.RootElement.EnumerateArray(), item => Assert.Equal("Lobby", item.GetProperty("estado").GetString()));
        Assert.Contains(document.RootElement.EnumerateArray(), item => item.GetProperty("modalidad").GetString() == "Individual");
        Assert.Contains(document.RootElement.EnumerateArray(), item => item.GetProperty("modalidad").GetString() == "Equipo");
    }

    [Theory]
    [InlineData("Individual")]
    [InlineData("Equipo")]
    public async Task GetPublished_Should_Filter_By_Modality_For_Hu12(string modalidad)
    {
        await ClearDatabaseAsync();
        await SeedAsync(
            PartidaBDT.CrearPublicada("Individual publicada", Modalidad.Individual, new AreaBusqueda("Area norte"), OneStage()),
            PartidaBDT.CrearPublicada("Equipo publicada", Modalidad.Equipo, new AreaBusqueda("Area sur"), TwoStages()),
            PartidaBDT.CrearNoPublicada("Iniciada misma modalidad", Enum.Parse<Modalidad>(modalidad), new AreaBusqueda("Area oeste"), OneStage(), EstadoPartida.Iniciada));

        var response = await _client.SendAsync(CreateGetRequest($"/api/bdt/games/published?modalidad={modalidad}"));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(body);
        Assert.Single(document.RootElement.EnumerateArray());
        Assert.Equal(modalidad, document.RootElement[0].GetProperty("modalidad").GetString());
        Assert.Equal("Lobby", document.RootElement[0].GetProperty("estado").GetString());
    }

    [Fact]
    public async Task GetPublished_Should_Return_BadRequest_For_Invalid_Modality()
    {
        var response = await _client.SendAsync(CreateGetRequest("/api/bdt/games/published?modalidad=Mixta"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetPublished_Should_Not_Mutate_Bdt_State_When_Filtering()
    {
        await ClearDatabaseAsync();
        await SeedAsync(
            PartidaBDT.CrearPublicada("Individual publicada", Modalidad.Individual, new AreaBusqueda("Area norte"), OneStage()),
            PartidaBDT.CrearPublicada("Equipo publicada", Modalidad.Equipo, new AreaBusqueda("Area sur"), TwoStages()),
            PartidaBDT.CrearNoPublicada("Cancelada", Modalidad.Equipo, new AreaBusqueda("Area este"), OneStage(), EstadoPartida.Cancelada));
        var before = await CountPartidasAsync();

        var response = await _client.SendAsync(CreateGetRequest("/api/bdt/games/published?modalidad=Equipo"));

        var after = await CountPartidasAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(before, after);
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

    private async Task<int> CountPartidasAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BdtDbContext>();
        return await Task.FromResult(dbContext.Partidas.Count());
    }

    private static EtapaBDT[] OneStage() => new[] { EtapaBDT.Crear(1, "QR-1", 60) };

    private static EtapaBDT[] TwoStages() => new[] { EtapaBDT.Crear(1, "QR-1", 60), EtapaBDT.Crear(2, "QR-2", 90) };
}
