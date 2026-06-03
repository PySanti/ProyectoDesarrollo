using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Umbral.BdtGameService.Domain.Entities;
using Umbral.BdtGameService.Domain.Enums;
using Umbral.BdtGameService.Domain.ValueObjects;
using Umbral.BdtGameService.Infrastructure.Persistence;

namespace Umbral.BdtGameService.IntegrationTests;

public sealed class Hu39PostgresJoinIndividualBdtTests : IClassFixture<PostgresBdtApiFactory>, IAsyncLifetime
{
    private readonly PostgresBdtApiFactory _factory;
    private readonly HttpClient _client;

    public Hu39PostgresJoinIndividualBdtTests(PostgresBdtApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task PostIndividualInscription_Should_Persist_Explorer_With_Npgsql()
    {
        var partida = await SeedAsync(CreateIndividualGame(2));

        var response = await _client.SendAsync(CreateJoinRequest(partida.PartidaId, Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, await CountExploradoresAsync());
    }

    [Fact]
    public async Task PostIndividualInscription_Should_Map_Postgres_Unique_Conflict_To_409()
    {
        var partida = await SeedAsync(CreateIndividualGame(2));
        var participanteId = Guid.NewGuid();
        var firstResponse = await _client.SendAsync(CreateJoinRequest(partida.PartidaId, participanteId));
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        var response = await _client.SendAsync(CreateJoinRequest(partida.PartidaId, participanteId));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(1, await CountExploradoresAsync());
    }

    [Fact]
    public async Task PostIndividualInscription_Should_Not_Exceed_Capacity_With_Npgsql()
    {
        var partida = await SeedAsync(CreateIndividualGame(1));
        var firstResponse = await _client.SendAsync(CreateJoinRequest(partida.PartidaId, Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        var response = await _client.SendAsync(CreateJoinRequest(partida.PartidaId, Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(1, await CountExploradoresAsync());
    }

    [Fact]
    public async Task PostIndividualInscription_Should_Not_Exceed_Capacity_With_Concurrent_Npgsql_Requests()
    {
        var partida = await SeedAsync(CreateIndividualGame(1));
        await AddInsertDelayTriggerAsync();
        var firstParticipantId = Guid.NewGuid();
        var secondParticipantId = Guid.NewGuid();

        var responses = await Task.WhenAll(
            _factory.CreateClient().SendAsync(CreateJoinRequest(partida.PartidaId, firstParticipantId)),
            _factory.CreateClient().SendAsync(CreateJoinRequest(partida.PartidaId, secondParticipantId)));

        Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.OK));
        Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.Conflict));
        Assert.All(responses, response => Assert.Contains(response.StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.Conflict }));
        Assert.Equal(1, await CountExploradoresAsync());
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

    private async Task AddInsertDelayTriggerAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BdtDbContext>();
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE OR REPLACE FUNCTION delay_explorador_insert()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $$
            BEGIN
                PERFORM pg_sleep(0.2);
                RETURN NEW;
            END;
            $$;

            DROP TRIGGER IF EXISTS delay_explorador_insert_trigger ON exploradores_bdt;

            CREATE TRIGGER delay_explorador_insert_trigger
            BEFORE INSERT ON exploradores_bdt
            FOR EACH ROW
            EXECUTE FUNCTION delay_explorador_insert();
            """);
    }

    private static HttpRequestMessage CreateJoinRequest(Guid partidaId, Guid userId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/bdt/games/{partidaId}/individual-inscriptions");
        request.Headers.Add("X-Test-Role", "Participante");
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
