using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.TestHost;
using Umbral.TriviaGame.Application.Dtos;

namespace Umbral.TriviaGame.Api.Tests;

public sealed class JoinTriviaGameControllerTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public JoinTriviaGameControllerTests(IntegrationTestFactory factory)
    {
        _factory = factory;
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
            title = "Form for Join",
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
            nombre = "Join Test Game",
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
    public async Task Join_IndividualGameEnLobby_Returns200()
    {
        var operadorClient = _factory.CreateClient();
        var formId = await CreateValidFormAsync(operadorClient);
        var gameId = await CreateIndividualGameAsync(operadorClient, formId);

        var participanteClient = CreateParticipanteClient();
        var response = await participanteClient.PostAsync($"/api/trivia-games/{gameId}/join", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TriviaInscripcionDto>();
        Assert.NotNull(body);
        Assert.Equal(gameId, body.PartidaId);
        Assert.NotEqual(Guid.Empty, body.InscripcionId);
    }

    [Fact]
    public async Task Join_GameNotExists_Returns404()
    {
        var participanteClient = CreateParticipanteClient();
        var response = await participanteClient.PostAsync($"/api/trivia-games/{Guid.NewGuid()}/join", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Join_EquipoGame_Returns409()
    {
        var operadorClient = _factory.CreateClient();
        var formId = await CreateValidFormAsync(operadorClient);

        var createCmd = new
        {
            nombre = "Equipo Game",
            modalidad = "Equipo",
            modoInicio = "Manual",
            formularioId = formId,
            tiempoInicio = DateTimeOffset.UtcNow.AddHours(1),
            minimoParticipantes = 2,
            maximoJugadores = (int?)null,
            maximoEquipos = 5,
            minimoJugadoresPorEquipo = 1,
            maximoJugadoresPorEquipo = 4,
        };

        var createResponse = await operadorClient.PostAsJsonAsync("/api/trivia-games", createCmd);
        var created = await createResponse.Content.ReadFromJsonAsync<TriviaGameDetailDto>();
        Assert.NotNull(created);

        var participanteClient = CreateParticipanteClient();
        var response = await participanteClient.PostAsync($"/api/trivia-games/{created.Id}/join", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Join_CupoLleno_Returns409()
    {
        var operadorClient = _factory.CreateClient();
        var formId = await CreateValidFormAsync(operadorClient);

        var createCmd = new
        {
            nombre = "Full Game",
            modalidad = "Individual",
            modoInicio = "Manual",
            formularioId = formId,
            tiempoInicio = DateTimeOffset.UtcNow.AddHours(1),
            minimoParticipantes = 1,
            maximoJugadores = 1,
            maximoEquipos = (int?)null,
            minimoJugadoresPorEquipo = (int?)null,
            maximoJugadoresPorEquipo = (int?)null,
        };

        var createResponse = await operadorClient.PostAsJsonAsync("/api/trivia-games", createCmd);
        var created = await createResponse.Content.ReadFromJsonAsync<TriviaGameDetailDto>();
        Assert.NotNull(created);

        var participanteClient = CreateParticipanteClient();
        var firstJoin = await participanteClient.PostAsync($"/api/trivia-games/{created.Id}/join", null);
        Assert.Equal(HttpStatusCode.OK, firstJoin.StatusCode);

        var secondJoin = await participanteClient.PostAsync($"/api/trivia-games/{created.Id}/join", null);
        Assert.Equal(HttpStatusCode.Conflict, secondJoin.StatusCode);
    }

    [Fact]
    public async Task Join_Duplicado_Returns409()
    {
        var operadorClient = _factory.CreateClient();
        var formId = await CreateValidFormAsync(operadorClient);
        var gameId = await CreateIndividualGameAsync(operadorClient, formId);

        var participanteClient = CreateParticipanteClient();
        var firstJoin = await participanteClient.PostAsync($"/api/trivia-games/{gameId}/join", null);
        Assert.Equal(HttpStatusCode.OK, firstJoin.StatusCode);

        var secondJoin = await participanteClient.PostAsync($"/api/trivia-games/{gameId}/join", null);
        Assert.Equal(HttpStatusCode.Conflict, secondJoin.StatusCode);
    }

    [Fact]
    public async Task Join_GameAlreadyStarted_Returns409()
    {
        var operadorClient = _factory.CreateClient();
        var formId = await CreateValidFormAsync(operadorClient);
        var gameId = await CreateIndividualGameAsync(operadorClient, formId);

        var participanteClient = CreateParticipanteClient();
        var firstJoin = await participanteClient.PostAsync($"/api/trivia-games/{gameId}/join", null);
        Assert.Equal(HttpStatusCode.OK, firstJoin.StatusCode);

        var startResponse = await operadorClient.PostAsync($"/api/trivia-games/{gameId}/start", null);
        Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);

        var secondJoin = await participanteClient.PostAsync($"/api/trivia-games/{gameId}/join", null);
        Assert.Equal(HttpStatusCode.Conflict, secondJoin.StatusCode);
    }
}
