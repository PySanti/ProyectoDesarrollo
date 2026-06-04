using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Umbral.BdtGameService.Domain.Entities;
using Umbral.BdtGameService.Domain.Enums;
using Umbral.BdtGameService.Domain.ValueObjects;
using Umbral.BdtGameService.Infrastructure.Persistence;

namespace Umbral.BdtGameService.IntegrationTests;

public sealed class Hu43PostgresStartBdtGameTests : IClassFixture<PostgresBdtApiFactory>, IAsyncLifetime
{
    private readonly PostgresBdtApiFactory _factory;
    private readonly HttpClient _client;

    public Hu43PostgresStartBdtGameTests(PostgresBdtApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task PostStart_Should_Persist_State_And_Active_Stage_With_Npgsql()
    {
        var partida = CreateIndividualGame();
        partida.RegistrarParticipanteIndividual(Guid.NewGuid(), DateTime.UtcNow);
        await SeedAsync(partida);

        var response = await _client.SendAsync(CreateStartRequest(partida.PartidaId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BdtDbContext>();
        var persisted = await dbContext.Partidas
            .Include(game => game.Etapas)
            .SingleAsync(game => game.PartidaId == partida.PartidaId);
        Assert.Equal(EstadoPartida.Iniciada, persisted.Estado);
        var activeStage = Assert.Single(persisted.Etapas.Where(etapa => etapa.Estado == EstadoEtapa.Activa));
        Assert.Equal(1, activeStage.Orden);
        Assert.NotNull(activeStage.IniciadaEnUtc);
        Assert.NotNull(activeStage.CierraEnUtc);
    }

    [Fact]
    public async Task PostStart_Should_Serialize_Concurrent_Start_Attempts_With_Npgsql()
    {
        var partida = CreateIndividualGame();
        partida.RegistrarParticipanteIndividual(Guid.NewGuid(), DateTime.UtcNow);
        await SeedAsync(partida);

        var firstStart = _client.SendAsync(CreateStartRequest(partida.PartidaId));
        var secondStart = _client.SendAsync(CreateStartRequest(partida.PartidaId));

        var responses = await Task.WhenAll(firstStart, secondStart);
        var statusCodes = responses.Select(response => response.StatusCode).OrderBy(status => status).ToArray();

        Assert.Contains(HttpStatusCode.OK, statusCodes);
        Assert.Contains(HttpStatusCode.Conflict, statusCodes);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BdtDbContext>();
        var persisted = await dbContext.Partidas
            .Include(game => game.Etapas)
            .SingleAsync(game => game.PartidaId == partida.PartidaId);
        Assert.Equal(EstadoPartida.Iniciada, persisted.Estado);
        Assert.Single(persisted.Etapas.Where(etapa => etapa.Estado == EstadoEtapa.Activa));
    }

    private async Task SeedAsync(PartidaBDT partida)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BdtDbContext>();
        await dbContext.Partidas.AddAsync(partida);
        await dbContext.SaveChangesAsync();
    }

    private static HttpRequestMessage CreateStartRequest(Guid partidaId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/bdt/games/{partidaId}/start");
        request.Headers.Add("X-Test-Role", "Operador");
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
