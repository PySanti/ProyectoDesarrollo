using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Umbral.BdtGameService.Domain.Entities;
using Umbral.BdtGameService.Domain.Enums;
using Umbral.BdtGameService.Domain.ValueObjects;
using Umbral.BdtGameService.Infrastructure.Persistence;

namespace Umbral.BdtGameService.ContractTests;

public sealed class Hu43ContractTests : IClassFixture<BdtApiFactory>
{
    private readonly BdtApiFactory _factory;
    private readonly HttpClient _client;

    public Hu43ContractTests(BdtApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostStart_Should_Match_Hu43_Response_Shape()
    {
        await ClearDatabaseAsync();
        var partida = CreateIndividualGame();
        partida.RegistrarParticipanteIndividual(Guid.NewGuid(), DateTime.UtcNow);
        await SeedAsync(partida);

        var response = await _client.SendAsync(CreateStartRequest(partida.PartidaId));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(body);
        Assert.True(document.RootElement.TryGetProperty("partidaId", out _));
        Assert.True(document.RootElement.TryGetProperty("nombre", out _));
        Assert.True(document.RootElement.TryGetProperty("estado", out _));
        Assert.True(document.RootElement.TryGetProperty("modalidad", out _));
        Assert.True(document.RootElement.TryGetProperty("etapaActiva", out var etapaActiva));
        Assert.True(etapaActiva.TryGetProperty("etapaId", out _));
        Assert.True(etapaActiva.TryGetProperty("orden", out _));
        Assert.True(etapaActiva.TryGetProperty("tiempoLimiteSegundos", out _));
        Assert.True(etapaActiva.TryGetProperty("iniciadaEnUtc", out _));
        Assert.True(etapaActiva.TryGetProperty("cierraEnUtc", out _));
        Assert.True(document.RootElement.TryGetProperty("mensaje", out _));
    }

    [Fact]
    public async Task PostStart_Should_Match_BadRequest_Status()
    {
        var response = await _client.SendAsync(CreateStartRequest("invalid"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostStart_Should_Match_Unauthorized_Status()
    {
        var response = await _client.PostAsync($"/api/bdt/games/{Guid.NewGuid()}/start", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostStart_Should_Match_Forbidden_Status()
    {
        var response = await _client.SendAsync(CreateStartRequest(Guid.NewGuid(), role: "Participante"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PostStart_Should_Match_NotFound_Error_Shape()
    {
        await ClearDatabaseAsync();

        var response = await _client.SendAsync(CreateStartRequest(Guid.NewGuid()));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var document = JsonDocument.Parse(body);
        Assert.True(document.RootElement.TryGetProperty("message", out _));
    }

    [Fact]
    public async Task PostStart_Should_Match_Conflict_Error_Shape()
    {
        await ClearDatabaseAsync();
        var partida = CreateIndividualGame();
        await SeedAsync(partida);

        var response = await _client.SendAsync(CreateStartRequest(partida.PartidaId));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var document = JsonDocument.Parse(body);
        Assert.True(document.RootElement.TryGetProperty("message", out _));
    }

    private async Task ClearDatabaseAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BdtDbContext>();
        dbContext.Set<ExploradorBDT>().RemoveRange(dbContext.Set<ExploradorBDT>());
        dbContext.Partidas.RemoveRange(dbContext.Partidas);
        await dbContext.SaveChangesAsync();
    }

    private async Task SeedAsync(PartidaBDT partida)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BdtDbContext>();
        await dbContext.Partidas.AddAsync(partida);
        await dbContext.SaveChangesAsync();
    }

    private static HttpRequestMessage CreateStartRequest(Guid partidaId, string role = "Operador")
    {
        return CreateStartRequest(partidaId.ToString(), role);
    }

    private static HttpRequestMessage CreateStartRequest(string partidaId, string role = "Operador")
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/bdt/games/{partidaId}/start");
        request.Headers.Add("X-Test-Role", role);
        request.Headers.Add("X-Test-UserId", Guid.NewGuid().ToString());
        return request;
    }

    private static PartidaBDT CreateIndividualGame()
    {
        return PartidaBDT.CrearPublicada(
            "Ruta QR",
            Modalidad.Individual,
            new AreaBusqueda("Campus"),
            minimoParticipantes: 1,
            maximoParticipantes: 5,
            maximoEquipos: null,
            minimoJugadoresPorEquipo: null,
            ModoInicioPartida.Manual,
            new[] { EtapaBDT.Crear(1, "QR-1", 60) });
    }
}
