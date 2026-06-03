using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Umbral.BdtGameService.Domain.Entities;
using Umbral.BdtGameService.Domain.Enums;
using Umbral.BdtGameService.Domain.ValueObjects;
using Umbral.BdtGameService.Infrastructure.Persistence;

namespace Umbral.BdtGameService.ContractTests;

public sealed class Hu39ContractTests : IClassFixture<BdtApiFactory>
{
    private readonly BdtApiFactory _factory;
    private readonly HttpClient _client;

    public Hu39ContractTests(BdtApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostIndividualInscription_Should_Match_Hu39_Response_Shape()
    {
        await ClearDatabaseAsync();
        var partida = await SeedAsync(CreateIndividualGame(2));

        var response = await _client.SendAsync(CreateJoinRequest(partida.PartidaId, Guid.NewGuid()));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(body);
        Assert.True(document.RootElement.TryGetProperty("partidaId", out _));
        Assert.True(document.RootElement.TryGetProperty("nombre", out _));
        Assert.True(document.RootElement.TryGetProperty("modalidad", out _));
        Assert.True(document.RootElement.TryGetProperty("estado", out _));
        Assert.True(document.RootElement.TryGetProperty("inscripcionId", out _));
        Assert.True(document.RootElement.TryGetProperty("participanteUserId", out _));
        Assert.True(document.RootElement.TryGetProperty("posicionEnLobby", out _));
        Assert.True(document.RootElement.TryGetProperty("mensaje", out _));
    }

    [Fact]
    public async Task PostIndividualInscription_Should_Match_BadRequest_Status()
    {
        var response = await _client.SendAsync(CreateJoinRequest("invalid", Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostIndividualInscription_Should_Match_Unauthorized_Status()
    {
        var response = await _client.PostAsync($"/api/bdt/games/{Guid.NewGuid()}/individual-inscriptions", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostIndividualInscription_Should_Match_Forbidden_Status()
    {
        var response = await _client.SendAsync(CreateJoinRequest(Guid.NewGuid(), Guid.NewGuid(), role: "Operador"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PostIndividualInscription_Should_Match_NotFound_Error_Shape()
    {
        await ClearDatabaseAsync();

        var response = await _client.SendAsync(CreateJoinRequest(Guid.NewGuid(), Guid.NewGuid()));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var document = JsonDocument.Parse(body);
        Assert.True(document.RootElement.TryGetProperty("message", out _));
    }

    [Fact]
    public async Task PostIndividualInscription_Should_Match_Conflict_Error_Shape()
    {
        await ClearDatabaseAsync();
        var partida = await SeedAsync(CreateIndividualGame(1));
        var firstResponse = await _client.SendAsync(CreateJoinRequest(partida.PartidaId, Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        var response = await _client.SendAsync(CreateJoinRequest(partida.PartidaId, Guid.NewGuid()));
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

    private async Task<PartidaBDT> SeedAsync(PartidaBDT partida)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BdtDbContext>();
        await dbContext.Partidas.AddAsync(partida);
        await dbContext.SaveChangesAsync();
        return partida;
    }

    private static HttpRequestMessage CreateJoinRequest(Guid partidaId, Guid userId, string role = "Participante")
    {
        return CreateJoinRequest(partidaId.ToString(), userId, role);
    }

    private static HttpRequestMessage CreateJoinRequest(string partidaId, Guid userId, string role = "Participante")
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/bdt/games/{partidaId}/individual-inscriptions");
        request.Headers.Add("X-Test-Role", role);
        request.Headers.Add("X-Test-UserId", userId.ToString());
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
}
