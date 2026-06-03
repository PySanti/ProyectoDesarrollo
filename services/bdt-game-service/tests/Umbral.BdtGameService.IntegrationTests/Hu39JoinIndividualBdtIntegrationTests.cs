using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Umbral.BdtGameService.Domain.Entities;
using Umbral.BdtGameService.Domain.Enums;
using Umbral.BdtGameService.Domain.ValueObjects;
using Umbral.BdtGameService.Infrastructure.Persistence;

namespace Umbral.BdtGameService.IntegrationTests;

public sealed class Hu39JoinIndividualBdtIntegrationTests : IClassFixture<BdtApiFactory>
{
    private readonly BdtApiFactory _factory;
    private readonly HttpClient _client;

    public Hu39JoinIndividualBdtIntegrationTests(BdtApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostIndividualInscription_Should_Register_Participant_And_Return_Waiting_Data()
    {
        await ClearDatabaseAsync();
        var partida = await SeedAsync(CreateIndividualGame(maximoParticipantes: 2));
        var participanteId = Guid.NewGuid();

        var response = await _client.SendAsync(CreateJoinRequest(partida.PartidaId, participanteId));
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(response.StatusCode == HttpStatusCode.OK, body);
        using var document = JsonDocument.Parse(body);
        Assert.Equal(partida.PartidaId, document.RootElement.GetProperty("partidaId").GetGuid());
        Assert.Equal("Individual", document.RootElement.GetProperty("modalidad").GetString());
        Assert.Equal("Lobby", document.RootElement.GetProperty("estado").GetString());
        Assert.Equal(participanteId, document.RootElement.GetProperty("participanteUserId").GetGuid());
        Assert.Equal(1, document.RootElement.GetProperty("posicionEnLobby").GetInt32());
        Assert.NotEqual(Guid.Empty, document.RootElement.GetProperty("inscripcionId").GetGuid());
        Assert.Equal(1, await CountExploradoresAsync());
    }

    [Fact]
    public async Task PostIndividualInscription_Should_Return_BadRequest_For_Invalid_PartidaId()
    {
        var response = await _client.SendAsync(CreateJoinRequest("not-a-guid", Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostIndividualInscription_Should_Return_Unauthorized_When_Unauthenticated()
    {
        var response = await _client.PostAsync($"/api/bdt/games/{Guid.NewGuid()}/individual-inscriptions", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostIndividualInscription_Should_Return_Forbidden_For_NonParticipant()
    {
        await ClearDatabaseAsync();
        var partida = await SeedAsync(CreateIndividualGame(maximoParticipantes: 2));

        var response = await _client.SendAsync(CreateJoinRequest(partida.PartidaId, Guid.NewGuid(), role: "Operador"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-guid")]
    public async Task PostIndividualInscription_Should_Return_Forbidden_For_Missing_Or_Malformed_Sub(string? userId)
    {
        await ClearDatabaseAsync();
        var partida = await SeedAsync(CreateIndividualGame(maximoParticipantes: 2));

        var response = await _client.SendAsync(CreateJoinRequest(partida.PartidaId.ToString(), userId, "Participante"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, await CountExploradoresAsync());
    }

    [Fact]
    public async Task PostIndividualInscription_Should_Return_NotFound_For_Missing_Game()
    {
        await ClearDatabaseAsync();

        var response = await _client.SendAsync(CreateJoinRequest(Guid.NewGuid(), Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostIndividualInscription_Should_Return_Conflict_For_NonLobby_Game()
    {
        await ClearDatabaseAsync();
        var partida = await SeedAsync(PartidaBDT.CrearNoPublicada(
            "Ruta QR",
            Modalidad.Individual,
            new AreaBusqueda("Campus"),
            new[] { EtapaBDT.Crear(1, "QR-1", 60) },
            EstadoPartida.Iniciada));

        var response = await _client.SendAsync(CreateJoinRequest(partida.PartidaId, Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PostIndividualInscription_Should_Return_Conflict_For_Team_Modality()
    {
        await ClearDatabaseAsync();
        var partida = await SeedAsync(CreateTeamGame());

        var response = await _client.SendAsync(CreateJoinRequest(partida.PartidaId, Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PostIndividualInscription_Should_Return_Conflict_For_Duplicate_Participant()
    {
        await ClearDatabaseAsync();
        var partida = await SeedAsync(CreateIndividualGame(maximoParticipantes: 2));
        var participanteId = Guid.NewGuid();
        var firstResponse = await _client.SendAsync(CreateJoinRequest(partida.PartidaId, participanteId));
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        var response = await _client.SendAsync(CreateJoinRequest(partida.PartidaId, participanteId));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(1, await CountExploradoresAsync());
    }

    [Fact]
    public async Task PostIndividualInscription_Should_Return_Conflict_When_Capacity_Is_Full()
    {
        await ClearDatabaseAsync();
        var partida = await SeedAsync(CreateIndividualGame(maximoParticipantes: 1));
        var firstResponse = await _client.SendAsync(CreateJoinRequest(partida.PartidaId, Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        var response = await _client.SendAsync(CreateJoinRequest(partida.PartidaId, Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(1, await CountExploradoresAsync());
    }

    [Fact]
    public async Task PostIndividualInscription_Should_Ignore_Request_Body_Participant_Id()
    {
        await ClearDatabaseAsync();
        var partida = await SeedAsync(CreateIndividualGame(maximoParticipantes: 2));
        var tokenUserId = Guid.NewGuid();
        var request = CreateJoinRequest(partida.PartidaId, tokenUserId);
        request.Content = JsonContent.Create(new { participanteUserId = Guid.NewGuid() });

        var response = await _client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(response.StatusCode == HttpStatusCode.OK, body);
        using var document = JsonDocument.Parse(body);
        Assert.Equal(tokenUserId, document.RootElement.GetProperty("participanteUserId").GetGuid());
    }

    private async Task ClearDatabaseAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BdtDbContext>();
        dbContext.Set<ExploradorBDT>().RemoveRange(dbContext.Set<ExploradorBDT>());
        dbContext.Partidas.RemoveRange(dbContext.Partidas);
        await dbContext.SaveChangesAsync();
    }

    private async Task<PartidaBDT> SeedAsync(PartidaBDT partida)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BdtDbContext>();
        await dbContext.Partidas.AddAsync(partida);
        await dbContext.SaveChangesAsync();
        return partida;
    }

    private async Task<int> CountExploradoresAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BdtDbContext>();
        return await dbContext.Set<ExploradorBDT>().CountAsync();
    }

    private static HttpRequestMessage CreateJoinRequest(Guid partidaId, Guid userId, string role = "Participante")
    {
        return CreateJoinRequest(partidaId.ToString(), userId.ToString(), role);
    }

    private static HttpRequestMessage CreateJoinRequest(string partidaId, Guid userId, string role = "Participante")
    {
        return CreateJoinRequest(partidaId, userId.ToString(), role);
    }

    private static HttpRequestMessage CreateJoinRequest(string partidaId, string? userId, string role = "Participante")
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/bdt/games/{partidaId}/individual-inscriptions");
        request.Headers.Add("X-Test-Role", role);
        if (userId is not null)
        {
            request.Headers.Add("X-Test-UserId", userId);
        }

        return request;
    }

    private static PartidaBDT CreateIndividualGame(int maximoParticipantes)
    {
        return PartidaBDT.CrearPublicada(
            "Ruta QR",
            Modalidad.Individual,
            new AreaBusqueda("Campus"),
            minimoParticipantes: 1,
            maximoParticipantes: maximoParticipantes,
            maximoEquipos: null,
            minimoJugadoresPorEquipo: null,
            ModoInicioPartida.Manual,
            new[] { EtapaBDT.Crear(1, "QR-1", 60) });
    }

    private static PartidaBDT CreateTeamGame()
    {
        return PartidaBDT.CrearPublicada(
            "Ruta QR Equipo",
            Modalidad.Equipo,
            new AreaBusqueda("Campus"),
            minimoParticipantes: 1,
            maximoParticipantes: null,
            maximoEquipos: 2,
            minimoJugadoresPorEquipo: 1,
            ModoInicioPartida.Manual,
            new[] { EtapaBDT.Crear(1, "QR-1", 60) });
    }
}
