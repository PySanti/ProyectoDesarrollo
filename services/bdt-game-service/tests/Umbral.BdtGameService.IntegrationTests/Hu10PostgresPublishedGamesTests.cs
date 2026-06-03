using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Umbral.BdtGameService.Domain.Entities;
using Umbral.BdtGameService.Domain.Enums;
using Umbral.BdtGameService.Domain.ValueObjects;
using Umbral.BdtGameService.Infrastructure.Persistence;

namespace Umbral.BdtGameService.IntegrationTests;

public sealed class Hu10PostgresPublishedGamesTests : IClassFixture<PostgresBdtApiFactory>
{
    private readonly PostgresBdtApiFactory _factory;
    private readonly HttpClient _client;

    public Hu10PostgresPublishedGamesTests(PostgresBdtApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetPublished_Should_Return_Only_Lobby_Games_From_PostgreSQL_Without_Mutating_State()
    {
        await _factory.ResetDatabaseAsync();
        await SeedAsync(
            PartidaBDT.CrearPublicada("Individual publicada", Modalidad.Individual, new AreaBusqueda("Area norte"), OneStage()),
            PartidaBDT.CrearPublicada("Equipo publicada", Modalidad.Equipo, new AreaBusqueda("Area sur"), TwoStages()),
            PartidaBDT.CrearNoPublicada("Cancelada", Modalidad.Equipo, new AreaBusqueda("Area este"), OneStage(), EstadoPartida.Cancelada));
        var before = await CountPartidasAsync();

        var response = await _client.SendAsync(CreateGetRequest("/api/bdt/games/published"));
        var body = await response.Content.ReadAsStringAsync();
        var after = await CountPartidasAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(before, after);
        using var document = JsonDocument.Parse(body);
        Assert.Equal(2, document.RootElement.GetArrayLength());
        Assert.All(document.RootElement.EnumerateArray(), item => Assert.Equal("Lobby", item.GetProperty("estado").GetString()));
        Assert.Contains(document.RootElement.EnumerateArray(), item => item.GetProperty("modalidad").GetString() == "Individual");
        Assert.Contains(document.RootElement.EnumerateArray(), item => item.GetProperty("modalidad").GetString() == "Equipo");
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
        return await dbContext.Partidas.CountAsync();
    }

    private static HttpRequestMessage CreateGetRequest(string path, string role = "Participante")
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("X-Test-Role", role);
        request.Headers.Add("X-Test-UserId", Guid.NewGuid().ToString());
        return request;
    }

    private static EtapaBDT[] OneStage() => new[] { EtapaBDT.Crear(1, "QR-1", 60) };

    private static EtapaBDT[] TwoStages() => new[] { EtapaBDT.Crear(1, "QR-1", 60), EtapaBDT.Crear(2, "QR-2", 90) };
}
