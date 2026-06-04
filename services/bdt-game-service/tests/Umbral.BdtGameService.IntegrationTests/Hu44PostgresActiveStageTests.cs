using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Umbral.BdtGameService.Domain.Entities;
using Umbral.BdtGameService.Domain.Enums;
using Umbral.BdtGameService.Domain.ValueObjects;
using Umbral.BdtGameService.Infrastructure.Persistence;

namespace Umbral.BdtGameService.IntegrationTests;

public sealed class Hu44PostgresActiveStageTests : IClassFixture<PostgresBdtApiFactory>, IAsyncLifetime
{
    private readonly PostgresBdtApiFactory _factory;
    private readonly HttpClient _client;

    public Hu44PostgresActiveStageTests(PostgresBdtApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetActiveStage_Should_Read_Active_Stage_From_Npgsql_For_Registered_Participant()
    {
        var participanteId = Guid.NewGuid();
        var partida = CreateStartedIndividualGame(participanteId);
        await SeedAsync(partida);

        var response = await _client.SendAsync(CreateActiveStageRequest(partida.PartidaId, participanteId));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(body);
        Assert.Equal("Iniciada", document.RootElement.GetProperty("estado").GetString());
        Assert.Equal("Activa", document.RootElement.GetProperty("etapaActiva").GetProperty("estado").GetString());
        Assert.True(document.RootElement.GetProperty("puedeSubirTesoro").GetBoolean());
    }

    private async Task SeedAsync(PartidaBDT partida)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BdtDbContext>();
        await dbContext.Partidas.AddAsync(partida);
        await dbContext.SaveChangesAsync();
    }

    private static HttpRequestMessage CreateActiveStageRequest(Guid partidaId, Guid userId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/bdt/games/{partidaId}/active-stage");
        request.Headers.Add("X-Test-Role", "Participante");
        request.Headers.Add("X-Test-UserId", userId.ToString());
        return request;
    }

    private static PartidaBDT CreateStartedIndividualGame(Guid participanteId)
    {
        var partida = PartidaBDT.CrearPublicada(
            "Ruta QR",
            Modalidad.Individual,
            new AreaBusqueda("Campus"),
            minimoParticipantes: 1,
            maximoParticipantes: 5,
            maximoEquipos: null,
            minimoJugadoresPorEquipo: null,
            ModoInicioPartida.Manual,
            new[] { EtapaBDT.Crear(1, "QR-1", 60) });
        partida.RegistrarParticipanteIndividual(participanteId, DateTime.UtcNow);
        partida.IniciarManualmente(Guid.NewGuid(), DateTime.UtcNow);
        return partida;
    }
}
