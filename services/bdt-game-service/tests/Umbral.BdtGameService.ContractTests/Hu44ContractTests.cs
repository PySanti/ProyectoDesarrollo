using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Umbral.BdtGameService.Domain.Entities;
using Umbral.BdtGameService.Domain.Enums;
using Umbral.BdtGameService.Domain.ValueObjects;
using Umbral.BdtGameService.Infrastructure.Persistence;

namespace Umbral.BdtGameService.ContractTests;

public sealed class Hu44ContractTests : IClassFixture<BdtApiFactory>
{
    private readonly BdtApiFactory _factory;
    private readonly HttpClient _client;

    public Hu44ContractTests(BdtApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetActiveStage_Should_Match_Hu44_Response_Shape()
    {
        await ClearDatabaseAsync();
        var participanteId = Guid.NewGuid();
        var partida = CreateStartedIndividualGame(participanteId);
        await SeedAsync(partida);

        var response = await _client.SendAsync(CreateActiveStageRequest(partida.PartidaId, participanteId));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(body);
        Assert.True(document.RootElement.TryGetProperty("partidaId", out _));
        Assert.True(document.RootElement.TryGetProperty("nombre", out _));
        Assert.True(document.RootElement.TryGetProperty("estado", out _));
        Assert.True(document.RootElement.TryGetProperty("modalidad", out _));
        Assert.True(document.RootElement.TryGetProperty("exploradorId", out _));
        Assert.True(document.RootElement.TryGetProperty("etapaActiva", out var etapaActiva));
        Assert.True(etapaActiva.TryGetProperty("etapaId", out _));
        Assert.True(etapaActiva.TryGetProperty("orden", out _));
        Assert.True(etapaActiva.TryGetProperty("estado", out _));
        Assert.True(etapaActiva.TryGetProperty("tiempoLimiteSegundos", out _));
        Assert.True(etapaActiva.TryGetProperty("iniciadaEnUtc", out _));
        Assert.True(etapaActiva.TryGetProperty("cierraEnUtc", out _));
        Assert.True(document.RootElement.TryGetProperty("puedeSubirTesoro", out _));
        Assert.True(document.RootElement.TryGetProperty("requiereGeolocalizacion", out _));
        Assert.True(document.RootElement.TryGetProperty("mensaje", out _));
    }

    [Fact]
    public async Task GetActiveStage_Should_Match_BadRequest_Status()
    {
        var response = await _client.SendAsync(CreateActiveStageRequest("invalid", Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetActiveStage_Should_Match_Unauthorized_Status()
    {
        var response = await _client.GetAsync($"/api/bdt/games/{Guid.NewGuid()}/active-stage");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetActiveStage_Should_Match_Forbidden_Status()
    {
        var response = await _client.SendAsync(CreateActiveStageRequest(Guid.NewGuid(), Guid.NewGuid(), role: "Operador"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetActiveStage_Should_Match_NotFound_Error_Shape()
    {
        await ClearDatabaseAsync();

        var response = await _client.SendAsync(CreateActiveStageRequest(Guid.NewGuid(), Guid.NewGuid()));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var document = JsonDocument.Parse(body);
        Assert.True(document.RootElement.TryGetProperty("message", out _));
    }

    [Fact]
    public async Task GetActiveStage_Should_Match_Conflict_Error_Shape()
    {
        await ClearDatabaseAsync();
        var participanteId = Guid.NewGuid();
        var partida = CreateIndividualGame();
        partida.RegistrarParticipanteIndividual(participanteId, DateTime.UtcNow);
        await SeedAsync(partida);

        var response = await _client.SendAsync(CreateActiveStageRequest(partida.PartidaId, participanteId));
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

    private static HttpRequestMessage CreateActiveStageRequest(Guid partidaId, Guid userId, string role = "Participante")
    {
        return CreateActiveStageRequest(partidaId.ToString(), userId, role);
    }

    private static HttpRequestMessage CreateActiveStageRequest(string partidaId, Guid userId, string role = "Participante")
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/bdt/games/{partidaId}/active-stage");
        request.Headers.Add("X-Test-Role", role);
        request.Headers.Add("X-Test-UserId", userId.ToString());
        return request;
    }

    private static PartidaBDT CreateStartedIndividualGame(Guid participanteId)
    {
        var partida = CreateIndividualGame();
        partida.RegistrarParticipanteIndividual(participanteId, DateTime.UtcNow);
        partida.IniciarManualmente(Guid.NewGuid(), DateTime.UtcNow);
        return partida;
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
