using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Umbral.BdtGameService.Domain.Entities;
using Umbral.BdtGameService.Domain.Enums;
using Umbral.BdtGameService.Domain.ValueObjects;
using Umbral.BdtGameService.Infrastructure.Persistence;

namespace Umbral.BdtGameService.IntegrationTests;

public sealed class Hu43StartBdtGameIntegrationTests : IClassFixture<BdtApiFactory>
{
    private readonly BdtApiFactory _factory;
    private readonly HttpClient _client;

    public Hu43StartBdtGameIntegrationTests(BdtApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostStart_Should_Start_Game_And_Return_Active_Stage()
    {
        await ClearDatabaseAsync();
        var partida = CreateIndividualGame(minimoParticipantes: 1, modoInicio: ModoInicioPartida.Manual);
        partida.RegistrarParticipanteIndividual(Guid.NewGuid(), DateTime.UtcNow);
        await SeedAsync(partida);

        var response = await _client.SendAsync(CreateStartRequest(partida.PartidaId));
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(response.StatusCode == HttpStatusCode.OK, body);
        using var document = JsonDocument.Parse(body);
        Assert.Equal(partida.PartidaId, document.RootElement.GetProperty("partidaId").GetGuid());
        Assert.Equal("Iniciada", document.RootElement.GetProperty("estado").GetString());
        Assert.Equal("Individual", document.RootElement.GetProperty("modalidad").GetString());
        var etapaActiva = document.RootElement.GetProperty("etapaActiva");
        Assert.Equal(1, etapaActiva.GetProperty("orden").GetInt32());
        Assert.Equal(60, etapaActiva.GetProperty("tiempoLimiteSegundos").GetInt32());
        Assert.NotEqual(default, etapaActiva.GetProperty("iniciadaEnUtc").GetDateTime());
        Assert.NotEqual(default, etapaActiva.GetProperty("cierraEnUtc").GetDateTime());
        Assert.Equal("Partida BDT iniciada.", document.RootElement.GetProperty("mensaje").GetString());
        await AssertPersistedStartedStateAsync(partida.PartidaId);
    }

    [Fact]
    public async Task PostStart_Should_Return_BadRequest_For_Invalid_PartidaId()
    {
        var response = await _client.SendAsync(CreateStartRequest("not-a-guid"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostStart_Should_Return_Unauthorized_When_Unauthenticated()
    {
        var response = await _client.PostAsync($"/api/bdt/games/{Guid.NewGuid()}/start", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostStart_Should_Return_Forbidden_For_NonOperator()
    {
        await ClearDatabaseAsync();
        var partida = CreateIndividualGame(1, ModoInicioPartida.Manual);
        partida.RegistrarParticipanteIndividual(Guid.NewGuid(), DateTime.UtcNow);
        await SeedAsync(partida);

        var response = await _client.SendAsync(CreateStartRequest(partida.PartidaId, role: "Participante"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-guid")]
    public async Task PostStart_Should_Return_Forbidden_For_Missing_Or_Malformed_Sub(string? userId)
    {
        await ClearDatabaseAsync();
        var partida = CreateIndividualGame(1, ModoInicioPartida.Manual);
        partida.RegistrarParticipanteIndividual(Guid.NewGuid(), DateTime.UtcNow);
        await SeedAsync(partida);

        var response = await _client.SendAsync(CreateStartRequest(partida.PartidaId.ToString(), userId, "Operador"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PostStart_Should_Return_NotFound_For_Missing_Game()
    {
        await ClearDatabaseAsync();

        var response = await _client.SendAsync(CreateStartRequest(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostStart_Should_Return_Conflict_For_NonLobby_Game()
    {
        await ClearDatabaseAsync();
        var partida = CreateIndividualGame(1, ModoInicioPartida.Manual);
        partida.RegistrarParticipanteIndividual(Guid.NewGuid(), DateTime.UtcNow);
        partida.IniciarManualmente(Guid.NewGuid(), DateTime.UtcNow);
        await SeedAsync(partida);

        var response = await _client.SendAsync(CreateStartRequest(partida.PartidaId));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PostStart_Should_Return_Conflict_When_Minimum_Participation_Is_Not_Met()
    {
        await ClearDatabaseAsync();
        var partida = CreateIndividualGame(minimoParticipantes: 2, modoInicio: ModoInicioPartida.Manual);
        partida.RegistrarParticipanteIndividual(Guid.NewGuid(), DateTime.UtcNow);
        await SeedAsync(partida);

        var response = await _client.SendAsync(CreateStartRequest(partida.PartidaId));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PostStart_Should_Return_Conflict_For_Strictly_Automatic_Game()
    {
        await ClearDatabaseAsync();
        var partida = CreateIndividualGame(1, ModoInicioPartida.Automatico);
        partida.RegistrarParticipanteIndividual(Guid.NewGuid(), DateTime.UtcNow);
        await SeedAsync(partida);

        var response = await _client.SendAsync(CreateStartRequest(partida.PartidaId));

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

    private async Task AssertPersistedStartedStateAsync(Guid partidaId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BdtDbContext>();
        var persisted = await dbContext.Partidas
            .Include(partida => partida.Etapas)
            .SingleAsync(partida => partida.PartidaId == partidaId);

        Assert.Equal(EstadoPartida.Iniciada, persisted.Estado);
        var activeStage = Assert.Single(persisted.Etapas.Where(etapa => etapa.Estado == EstadoEtapa.Activa));
        Assert.Equal(1, activeStage.Orden);
        Assert.NotNull(activeStage.IniciadaEnUtc);
        Assert.NotNull(activeStage.CierraEnUtc);
    }

    private static HttpRequestMessage CreateStartRequest(Guid partidaId, string role = "Operador")
    {
        return CreateStartRequest(partidaId.ToString(), Guid.NewGuid().ToString(), role);
    }

    private static HttpRequestMessage CreateStartRequest(string partidaId)
    {
        return CreateStartRequest(partidaId, Guid.NewGuid().ToString(), "Operador");
    }

    private static HttpRequestMessage CreateStartRequest(string partidaId, string? userId, string role)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/bdt/games/{partidaId}/start");
        request.Headers.Add("X-Test-Role", role);
        if (userId is not null)
        {
            request.Headers.Add("X-Test-UserId", userId);
        }

        return request;
    }

    private static PartidaBDT CreateIndividualGame(int minimoParticipantes, ModoInicioPartida modoInicio)
    {
        return PartidaBDT.CrearPublicada(
            "Ruta QR",
            Modalidad.Individual,
            new AreaBusqueda("Campus"),
            minimoParticipantes,
            maximoParticipantes: 5,
            maximoEquipos: null,
            minimoJugadoresPorEquipo: null,
            modoInicio,
            new[] { EtapaBDT.Crear(1, "QR-1", 60), EtapaBDT.Crear(2, "QR-2", 90) });
    }
}
