using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Umbral.BdtGameService.Domain.Entities;
using Umbral.BdtGameService.Domain.Enums;
using Umbral.BdtGameService.Domain.ValueObjects;
using Umbral.BdtGameService.Infrastructure.Persistence;

namespace Umbral.BdtGameService.IntegrationTests;

public sealed class Hu44ActiveStageIntegrationTests : IClassFixture<BdtApiFactory>
{
    private readonly BdtApiFactory _factory;
    private readonly HttpClient _client;

    public Hu44ActiveStageIntegrationTests(BdtApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetActiveStage_Should_Return_Active_Stage_For_Registered_Participant_Without_Mutating_State()
    {
        await ClearDatabaseAsync();
        var participanteId = Guid.NewGuid();
        var partida = CreateStartedIndividualGame(participanteId);
        await SeedAsync(partida);
        var beforePartidas = await CountPartidasAsync();
        var beforeExploradores = await CountExploradoresAsync();

        var response = await _client.SendAsync(CreateActiveStageRequest(partida.PartidaId, participanteId));
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(response.StatusCode == HttpStatusCode.OK, body);
        Assert.Equal(beforePartidas, await CountPartidasAsync());
        Assert.Equal(beforeExploradores, await CountExploradoresAsync());
        using var document = JsonDocument.Parse(body);
        Assert.Equal(partida.PartidaId, document.RootElement.GetProperty("partidaId").GetGuid());
        Assert.Equal("Iniciada", document.RootElement.GetProperty("estado").GetString());
        Assert.Equal("Individual", document.RootElement.GetProperty("modalidad").GetString());
        Assert.True(document.RootElement.GetProperty("puedeSubirTesoro").GetBoolean());
        Assert.True(document.RootElement.GetProperty("requiereGeolocalizacion").GetBoolean());
        Assert.NotEqual(Guid.Empty, document.RootElement.GetProperty("exploradorId").GetGuid());
        var etapaActiva = document.RootElement.GetProperty("etapaActiva");
        Assert.Equal(1, etapaActiva.GetProperty("orden").GetInt32());
        Assert.Equal("Activa", etapaActiva.GetProperty("estado").GetString());
        Assert.NotEqual(default, etapaActiva.GetProperty("iniciadaEnUtc").GetDateTime());
        Assert.NotEqual(default, etapaActiva.GetProperty("cierraEnUtc").GetDateTime());
    }

    [Fact]
    public async Task GetActiveStage_Should_Return_BadRequest_For_Invalid_PartidaId()
    {
        var response = await _client.SendAsync(CreateActiveStageRequest("not-a-guid", Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetActiveStage_Should_Return_Unauthorized_When_Unauthenticated()
    {
        var response = await _client.GetAsync($"/api/bdt/games/{Guid.NewGuid()}/active-stage");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetActiveStage_Should_Return_Forbidden_For_NonParticipant()
    {
        await ClearDatabaseAsync();
        var participanteId = Guid.NewGuid();
        var partida = CreateStartedIndividualGame(participanteId);
        await SeedAsync(partida);

        var response = await _client.SendAsync(CreateActiveStageRequest(partida.PartidaId, participanteId, role: "Operador"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-guid")]
    public async Task GetActiveStage_Should_Return_Forbidden_For_Missing_Or_Malformed_Sub(string? userId)
    {
        await ClearDatabaseAsync();
        var partida = CreateStartedIndividualGame(Guid.NewGuid());
        await SeedAsync(partida);

        var response = await _client.SendAsync(CreateActiveStageRequest(partida.PartidaId.ToString(), userId, "Participante"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetActiveStage_Should_Return_NotFound_For_Missing_Game()
    {
        await ClearDatabaseAsync();

        var response = await _client.SendAsync(CreateActiveStageRequest(Guid.NewGuid(), Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetActiveStage_Should_Return_Forbidden_For_Unregistered_Participant()
    {
        await ClearDatabaseAsync();
        var partida = CreateStartedIndividualGame(Guid.NewGuid());
        await SeedAsync(partida);

        var response = await _client.SendAsync(CreateActiveStageRequest(partida.PartidaId, Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetActiveStage_Should_Return_Conflict_For_NonInitiated_Game()
    {
        await ClearDatabaseAsync();
        var participanteId = Guid.NewGuid();
        var partida = CreateIndividualGame();
        partida.RegistrarParticipanteIndividual(participanteId, DateTime.UtcNow);
        await SeedAsync(partida);

        var response = await _client.SendAsync(CreateActiveStageRequest(partida.PartidaId, participanteId));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetActiveStage_Should_Return_Conflict_For_Initiated_Game_Without_Active_Stage()
    {
        await ClearDatabaseAsync();
        var partida = PartidaBDT.CrearNoPublicada(
            "Ruta QR",
            Modalidad.Individual,
            new AreaBusqueda("Campus"),
            new[] { EtapaBDT.Crear(1, "QR-1", 60) },
            EstadoPartida.Iniciada);
        await SeedAsync(partida);

        var response = await _client.SendAsync(CreateActiveStageRequest(partida.PartidaId, Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
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

    private async Task<int> CountPartidasAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BdtDbContext>();
        return await dbContext.Partidas.CountAsync();
    }

    private async Task<int> CountExploradoresAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BdtDbContext>();
        return await dbContext.Set<ExploradorBDT>().CountAsync();
    }

    private static HttpRequestMessage CreateActiveStageRequest(Guid partidaId, Guid userId, string role = "Participante")
    {
        return CreateActiveStageRequest(partidaId.ToString(), userId.ToString(), role);
    }

    private static HttpRequestMessage CreateActiveStageRequest(string partidaId, Guid userId)
    {
        return CreateActiveStageRequest(partidaId, userId.ToString(), "Participante");
    }

    private static HttpRequestMessage CreateActiveStageRequest(string partidaId, string? userId, string role)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/bdt/games/{partidaId}/active-stage");
        request.Headers.Add("X-Test-Role", role);
        if (userId is not null)
        {
            request.Headers.Add("X-Test-UserId", userId);
        }

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
