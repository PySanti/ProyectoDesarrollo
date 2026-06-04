using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Umbral.BdtGameService.Domain.Entities;
using Umbral.BdtGameService.Domain.Enums;
using Umbral.BdtGameService.Domain.ValueObjects;
using Umbral.BdtGameService.Infrastructure.Persistence;

namespace Umbral.BdtGameService.IntegrationTests;

public sealed class Hu43RealtimeIntegrationTests : IClassFixture<BdtApiFactory>
{
    private readonly BdtApiFactory _factory;
    private readonly HttpClient _client;

    public Hu43RealtimeIntegrationTests(BdtApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task BdtHub_Should_Reject_Unauthenticated_Connections()
    {
        await using var connection = CreateHubConnection();

        await Assert.ThrowsAnyAsync<Exception>(() => connection.StartAsync());
    }

    [Fact]
    public async Task BdtHub_Should_Deliver_PartidaBdtIniciada_Only_To_Subscribed_Partida_Group()
    {
        await ClearDatabaseAsync();
        var participanteId = Guid.NewGuid();
        var partida = CreateIndividualGame();
        partida.RegistrarParticipanteIndividual(participanteId, DateTime.UtcNow);
        await SeedAsync(partida);

        var subscribedMessage = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        var notSubscribedMessage = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var subscribedConnection = CreateHubConnection("Participante", participanteId);
        await using var notSubscribedConnection = CreateHubConnection("Participante", Guid.NewGuid());

        subscribedConnection.On<JsonElement>("PartidaBDTIniciada", payload => subscribedMessage.TrySetResult(payload));
        notSubscribedConnection.On<JsonElement>("PartidaBDTIniciada", payload => notSubscribedMessage.TrySetResult(payload));

        await subscribedConnection.StartAsync();
        await notSubscribedConnection.StartAsync();
        await subscribedConnection.InvokeAsync("SubscribeToPartida", partida.PartidaId);

        var response = await _client.SendAsync(CreateStartRequest(partida.PartidaId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await subscribedMessage.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("PartidaBDTIniciada", payload.GetProperty("type").GetString());
        Assert.Equal(1, payload.GetProperty("version").GetInt32());
        Assert.Equal(partida.PartidaId, payload.GetProperty("partidaId").GetGuid());
        Assert.Equal("Iniciada", payload.GetProperty("estado").GetString());
        Assert.Equal("Individual", payload.GetProperty("modalidad").GetString());

        var etapaActiva = payload.GetProperty("etapaActiva");
        Assert.NotEqual(Guid.Empty, etapaActiva.GetProperty("etapaId").GetGuid());
        Assert.Equal(1, etapaActiva.GetProperty("orden").GetInt32());
        Assert.Equal(60, etapaActiva.GetProperty("tiempoLimiteSegundos").GetInt32());
        Assert.NotEqual(default, etapaActiva.GetProperty("iniciadaEnUtc").GetDateTime());
        Assert.NotEqual(default, etapaActiva.GetProperty("cierraEnUtc").GetDateTime());
        Assert.NotEqual(default, payload.GetProperty("occurredOnUtc").GetDateTime());

        var completed = await Task.WhenAny(notSubscribedMessage.Task, Task.Delay(300));
        Assert.NotSame(notSubscribedMessage.Task, completed);
    }

    [Fact]
    public async Task BdtHub_Should_Reject_Subscription_For_Unregistered_Participant()
    {
        await ClearDatabaseAsync();
        var registeredParticipantId = Guid.NewGuid();
        var partida = CreateIndividualGame();
        partida.RegistrarParticipanteIndividual(registeredParticipantId, DateTime.UtcNow);
        await SeedAsync(partida);

        await using var connection = CreateHubConnection("Participante", Guid.NewGuid());
        await connection.StartAsync();

        await Assert.ThrowsAsync<HubException>(() => connection.InvokeAsync("SubscribeToPartida", partida.PartidaId));
    }

    [Fact]
    public async Task BdtHub_Should_Deliver_PartidaBdtIniciada_To_Registered_Participant()
    {
        await ClearDatabaseAsync();
        var participanteId = Guid.NewGuid();
        var partida = CreateIndividualGame();
        partida.RegistrarParticipanteIndividual(participanteId, DateTime.UtcNow);
        await SeedAsync(partida);

        var receivedMessage = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var connection = CreateHubConnection("Participante", participanteId);
        connection.On<JsonElement>("PartidaBDTIniciada", payload => receivedMessage.TrySetResult(payload));

        await connection.StartAsync();
        await connection.InvokeAsync("SubscribeToPartida", partida.PartidaId);

        var response = await _client.SendAsync(CreateStartRequest(partida.PartidaId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await receivedMessage.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(partida.PartidaId, payload.GetProperty("partidaId").GetGuid());
    }

    [Fact]
    public async Task BdtHub_Should_Deliver_PartidaBdtIniciada_To_Operator_Subscriber()
    {
        await ClearDatabaseAsync();
        var participanteId = Guid.NewGuid();
        var partida = CreateIndividualGame();
        partida.RegistrarParticipanteIndividual(participanteId, DateTime.UtcNow);
        await SeedAsync(partida);

        var receivedMessage = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var connection = CreateHubConnection("Operador", Guid.NewGuid());
        connection.On<JsonElement>("PartidaBDTIniciada", payload => receivedMessage.TrySetResult(payload));

        await connection.StartAsync();
        await connection.InvokeAsync("SubscribeToPartida", partida.PartidaId);

        var response = await _client.SendAsync(CreateStartRequest(partida.PartidaId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await receivedMessage.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(partida.PartidaId, payload.GetProperty("partidaId").GetGuid());
    }

    private HubConnection CreateHubConnection(string? role = null, Guid? userId = null)
    {
        return new HubConnectionBuilder()
            .WithUrl("http://localhost/hubs/bdt", options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
                if (role is not null)
                {
                    options.Headers.Add("X-Test-Role", role);
                }

                if (userId.HasValue)
                {
                    options.Headers.Add("X-Test-UserId", userId.Value.ToString());
                }
            })
            .Build();
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
