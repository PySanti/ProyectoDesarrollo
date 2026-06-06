using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Umbral.BdtGameService.Domain.Entities;
using Umbral.BdtGameService.Domain.Enums;
using Umbral.BdtGameService.Domain.ValueObjects;
using Umbral.BdtGameService.Infrastructure.Persistence;

namespace Umbral.BdtGameService.IntegrationTests;

public sealed class Hu37OperatorPublishedGamesIntegrationTests : IClassFixture<BdtApiFactory>
{
    private readonly BdtApiFactory _factory;
    private readonly HttpClient _client;

    public Hu37OperatorPublishedGamesIntegrationTests(BdtApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetOperatorPublished_Should_Return_Unauthorized_When_Unauthenticated()
    {
        var response = await _client.GetAsync("/api/bdt/operator/games/published");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetOperatorPublished_Should_Return_Forbidden_For_NonOperator()
    {
        var response = await _client.SendAsync(CreateGetRequest(role: "Participante"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-guid")]
    public async Task GetOperatorPublished_Should_Return_Forbidden_When_Sub_Claim_Is_Missing_Or_Malformed(string? userId)
    {
        var response = await _client.SendAsync(CreateGetRequest(userId: userId));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetOperatorPublished_Should_Return_Empty_List_When_No_Published_Games_Exist()
    {
        await ClearDatabaseAsync();
        await SeedAsync(PartidaBDT.CrearNoPublicada("Iniciada", Modalidad.Individual, new AreaBusqueda("Area"), OneStage(), EstadoPartida.Iniciada));

        var response = await _client.SendAsync(CreateGetRequest());
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(body);
        Assert.Equal(JsonValueKind.Array, document.RootElement.ValueKind);
        Assert.Equal(0, document.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task GetOperatorPublished_Should_Accept_NameIdentifier_UserId_When_Sub_Claim_Is_Not_Present()
    {
        await ClearDatabaseAsync();
        await SeedAsync(PartidaBDT.CrearPublicada("BDT operador", Modalidad.Individual, new AreaBusqueda("Area"), OneStage()));

        var response = await _client.SendAsync(CreateGetRequest(userIdClaimMode: "NameIdentifierOnly"));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(body);
        var item = Assert.Single(document.RootElement.EnumerateArray());
        Assert.Equal("BDT operador", item.GetProperty("nombre").GetString());
    }

    [Fact]
    public async Task GetOperatorPublished_Should_Return_Only_Lobby_Games_With_Contract_Fields()
    {
        await ClearDatabaseAsync();
        await SeedAsync(
            PartidaBDT.CrearPublicada("Aventura individual", Modalidad.Individual, new AreaBusqueda("Area norte"), OneStage()),
            PartidaBDT.CrearPublicada("Busqueda equipos", Modalidad.Equipo, new AreaBusqueda("Area sur"), TwoStages()),
            PartidaBDT.CrearNoPublicada("Cancelada", Modalidad.Equipo, new AreaBusqueda("Area este"), OneStage(), EstadoPartida.Cancelada));

        var response = await _client.SendAsync(CreateGetRequest());
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(body);
        Assert.Equal(2, document.RootElement.GetArrayLength());
        Assert.All(document.RootElement.EnumerateArray(), item =>
        {
            Assert.True(item.TryGetProperty("partidaId", out _));
            Assert.True(item.TryGetProperty("nombre", out _));
            Assert.True(item.TryGetProperty("modalidad", out _));
            Assert.Equal("Lobby", item.GetProperty("estado").GetString());
            Assert.True(item.TryGetProperty("areaBusqueda", out _));
            Assert.True(item.TryGetProperty("cantidadEtapas", out _));
        });
        Assert.Contains(document.RootElement.EnumerateArray(), item => item.GetProperty("cantidadEtapas").GetInt32() == 2);
    }

    [Fact]
    public async Task GetOperatorPublished_Should_Return_Games_Ordered_By_Name_Then_Id()
    {
        await ClearDatabaseAsync();
        var alphaLaterId = PartidaBDT.CrearPublicada("Alpha", Modalidad.Individual, new AreaBusqueda("Area norte"), OneStage());
        var beta = PartidaBDT.CrearPublicada("Beta", Modalidad.Individual, new AreaBusqueda("Area sur"), OneStage());
        var alphaEarlierId = PartidaBDT.CrearPublicada("Alpha", Modalidad.Equipo, new AreaBusqueda("Area este"), OneStage());

        await SeedAsync(beta, alphaLaterId, alphaEarlierId);

        var response = await _client.SendAsync(CreateGetRequest());
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(body);
        var items = document.RootElement.EnumerateArray().ToList();

        Assert.Equal(new[] { "Alpha", "Alpha", "Beta" }, items.Select(item => item.GetProperty("nombre").GetString()).ToArray());
        var firstAlphaId = Guid.Parse(items[0].GetProperty("partidaId").GetString()!);
        var secondAlphaId = Guid.Parse(items[1].GetProperty("partidaId").GetString()!);
        Assert.True(firstAlphaId.CompareTo(secondAlphaId) < 0);
    }

    [Fact]
    public async Task GetOperatorPublished_Should_Not_Mutate_Bdt_State()
    {
        await ClearDatabaseAsync();
        await SeedAsync(
            PartidaBDT.CrearPublicada("Publicada", Modalidad.Individual, new AreaBusqueda("Area norte"), OneStage()),
            PartidaBDT.CrearNoPublicada("Terminada", Modalidad.Equipo, new AreaBusqueda("Area sur"), OneStage(), EstadoPartida.Terminada));
        var before = await CountPartidasAsync();

        var response = await _client.SendAsync(CreateGetRequest());

        var after = await CountPartidasAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(before, after);
    }

    private static HttpRequestMessage CreateGetRequest(string role = "Operador", string? userId = "default", string? userIdClaimMode = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/bdt/operator/games/published");
        request.Headers.Add("X-Test-Role", role);
        if (userId is not null)
        {
            request.Headers.Add("X-Test-UserId", userId == "default" ? Guid.NewGuid().ToString() : userId);
        }

        if (!string.IsNullOrWhiteSpace(userIdClaimMode))
        {
            request.Headers.Add("X-Test-UserId-Claim", userIdClaimMode);
        }

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
