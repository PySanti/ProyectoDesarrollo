using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.TestHost;
using Umbral.TriviaGame.Application.Dtos;

namespace Umbral.TriviaGame.Api.Tests;

public sealed class TriviaGameLobbyControllerTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public TriviaGameLobbyControllerTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateOperadorClient()
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                var descriptor = services.SingleOrDefault(d =>
                    d.ServiceType == typeof(Testing.TestClaimsProvider));
                if (descriptor is not null) services.Remove(descriptor);
                services.AddScoped(_ => Testing.TestClaimsProvider.WithOperadorRole());
            });
        }).CreateClient();
    }

    private HttpClient CreateParticipanteClient()
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                var descriptor = services.SingleOrDefault(d =>
                    d.ServiceType == typeof(Testing.TestClaimsProvider));
                if (descriptor is not null) services.Remove(descriptor);
                services.AddScoped(_ => Testing.TestClaimsProvider.WithoutOperadorRole());
            });
        }).CreateClient();
    }

    private static async Task<Guid> CreateValidFormAsync(HttpClient client)
    {
        var command = new
        {
            title = "Form for Lobby",
            questions = new[]
            {
                new
                {
                    text = "Test question?",
                    assignedScore = 100,
                    timeLimitSeconds = 30,
                    displayOrder = 1,
                    options = new[]
                    {
                        new { text = "Correct", isCorrect = true },
                        new { text = "Wrong1", isCorrect = false },
                        new { text = "Wrong2", isCorrect = false },
                        new { text = "Wrong3", isCorrect = false },
                    },
                },
            },
        };

        var response = await client.PostAsJsonAsync("/api/trivia-forms", command);
        var body = await response.Content.ReadFromJsonAsync<TriviaFormDetailDto>();
        return body!.Id;
    }

    private static async Task<Guid> CreateIndividualGameAsync(HttpClient client, Guid formId)
    {
        var command = new
        {
            nombre = "Lobby Test Game",
            modalidad = "Individual",
            modoInicio = "Manual",
            formularioId = formId,
            tiempoInicio = DateTimeOffset.UtcNow.AddHours(1),
            minimoParticipantes = 1,
            maximoJugadores = 10,
            maximoEquipos = (int?)null,
            minimoJugadoresPorEquipo = (int?)null,
            maximoJugadoresPorEquipo = (int?)null,
        };

        var response = await client.PostAsJsonAsync("/api/trivia-games", command);
        var body = await response.Content.ReadFromJsonAsync<TriviaGameDetailDto>();
        return body!.Id;
    }

    [Fact]
    public async Task GetLobby_UserInscrito_Returns200WithData()
    {
        var operadorClient = CreateOperadorClient();
        var formId = await CreateValidFormAsync(operadorClient);
        var gameId = await CreateIndividualGameAsync(operadorClient, formId);

        var participanteClient = CreateParticipanteClient();
        var joinResponse = await participanteClient.PostAsync($"/api/trivia-games/{gameId}/join", null);
        Assert.Equal(HttpStatusCode.OK, joinResponse.StatusCode);

        var lobbyResponse = await participanteClient.GetAsync($"/api/trivia-games/{gameId}/lobby");
        Assert.Equal(HttpStatusCode.OK, lobbyResponse.StatusCode);

        var body = await lobbyResponse.Content.ReadFromJsonAsync<TriviaGameLobbyDto>();
        Assert.NotNull(body);
        Assert.Equal(gameId, body.PartidaId);
        Assert.Equal(1, body.ParticipantesActual);
        Assert.Single(body.Participantes);
    }

    [Fact]
    public async Task GetLobby_GameNotExists_Returns404()
    {
        var participanteClient = CreateParticipanteClient();
        var response = await participanteClient.GetAsync($"/api/trivia-games/{Guid.NewGuid()}/lobby");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetLobby_UserNotInscrito_Returns403()
    {
        var operadorClient = CreateOperadorClient();
        var formId = await CreateValidFormAsync(operadorClient);
        var gameId = await CreateIndividualGameAsync(operadorClient, formId);

        var participanteClient = CreateParticipanteClient();
        var response = await participanteClient.GetAsync($"/api/trivia-games/{gameId}/lobby");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
