using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Umbral.TriviaGame.Application.Dtos;
using Umbral.TriviaGame.Infrastructure.Data;

namespace Umbral.TriviaGame.Api.Tests;

public sealed class TriviaGamesAnswerControllerTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public TriviaGamesAnswerControllerTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AnswerCorrect_Returns200WithEsCorrectaTrue()
    {
        var (client, gameId, firstQuestionId) = await SetupGameAsync();

        var request = new { opcionIndex = 0 };
        var response = await client.PostAsJsonAsync(
            $"/api/trivia-games/{gameId}/questions/{firstQuestionId}/answer", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RespuestaTriviaDto>();
        Assert.NotNull(body);
        Assert.True(body.EsCorrecta);
        Assert.Equal(100, body.PuntajeObtenido);
        Assert.Equal(gameId, body.PartidaId);
        Assert.Equal(firstQuestionId, body.PreguntaId);
    }

    [Fact]
    public async Task AnswerIncorrect_Returns200WithEsCorrectaFalse()
    {
        var (client, gameId, firstQuestionId) = await SetupGameAsync();

        var request = new { opcionIndex = 1 };
        var response = await client.PostAsJsonAsync(
            $"/api/trivia-games/{gameId}/questions/{firstQuestionId}/answer", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RespuestaTriviaDto>();
        Assert.NotNull(body);
        Assert.False(body.EsCorrecta);
        Assert.Equal(0, body.PuntajeObtenido);
        Assert.Equal(gameId, body.PartidaId);
        Assert.Equal(firstQuestionId, body.PreguntaId);
    }

    [Fact]
    public async Task Answer_GameNotExists_Returns404()
    {
        var client = CreateParticipanteClient();
        var request = new { opcionIndex = 0 };
        var response = await client.PostAsJsonAsync(
            $"/api/trivia-games/{Guid.NewGuid()}/questions/{Guid.NewGuid()}/answer", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Answer_QuestionNotInGame_Returns400()
    {
        var (client, gameId, _) = await SetupGameAsync();
        var unknownQuestionId = Guid.NewGuid();

        var request = new { opcionIndex = 0 };
        var response = await client.PostAsJsonAsync(
            $"/api/trivia-games/{gameId}/questions/{unknownQuestionId}/answer", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Answer_DuplicateAnswer_Returns400()
    {
        var (client, gameId, firstQuestionId) = await SetupGameAsync();

        var request = new { opcionIndex = 1 };

        var firstResponse = await client.PostAsJsonAsync(
            $"/api/trivia-games/{gameId}/questions/{firstQuestionId}/answer", request);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        var secondResponse = await client.PostAsJsonAsync(
            $"/api/trivia-games/{gameId}/questions/{firstQuestionId}/answer", request);

        Assert.Equal(HttpStatusCode.BadRequest, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Answer_GameInLobby_Returns400()
    {
        var client = _factory.CreateClient();
        var formId = await CreateValidFormAsync(client);

        var createCmd = new
        {
            nombre = "AnswerLobbyTest",
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

        var createResponse = await client.PostAsJsonAsync("/api/trivia-games", createCmd);
        var created = await createResponse.Content.ReadFromJsonAsync<TriviaGameDetailDto>();
        Assert.NotNull(created);

        var formResponse = await client.GetAsync($"/api/trivia-forms/{formId}");
        var form = await formResponse.Content.ReadFromJsonAsync<TriviaFormDetailDto>();
        Assert.NotNull(form);
        Assert.NotEmpty(form.Questions);

        var joinClient = CreateParticipanteClient();
        var joinResponse = await joinClient.PostAsync(
            $"/api/trivia-games/{created.Id}/join", null);
        Assert.Equal(HttpStatusCode.OK, joinResponse.StatusCode);

        var request = new { opcionIndex = 0 };
        var answerResponse = await joinClient.PostAsJsonAsync(
            $"/api/trivia-games/{created.Id}/questions/{form.Questions[0].Id}/answer", request);

        Assert.Equal(HttpStatusCode.BadRequest, answerResponse.StatusCode);
    }

    [Fact]
    public async Task Answer_Unauthenticated_Returns401()
    {
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                var descriptor = services.SingleOrDefault(d =>
                    d.ServiceType == typeof(Testing.TestClaimsProvider));
                if (descriptor is not null) services.Remove(descriptor);
                services.AddScoped(_ => Testing.TestClaimsProvider.WithNoClaims());
            });
        }).CreateClient();

        var request = new { opcionIndex = 0 };
        var response = await client.PostAsJsonAsync(
            $"/api/trivia-games/{Guid.NewGuid()}/questions/{Guid.NewGuid()}/answer", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Answer_InvalidOptionIndex_Returns400()
    {
        var (client, gameId, firstQuestionId) = await SetupGameAsync();

        var request = new { opcionIndex = 99 };
        var response = await client.PostAsJsonAsync(
            $"/api/trivia-games/{gameId}/questions/{firstQuestionId}/answer", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<(HttpClient, Guid gameId, Guid firstQuestionId)> SetupGameAsync()
    {
        var operadorClient = _factory.CreateClient();
        var formId = await CreateValidFormAsync(operadorClient);

        var formResponse = await operadorClient.GetAsync($"/api/trivia-forms/{formId}");
        var form = await formResponse.Content.ReadFromJsonAsync<TriviaFormDetailDto>();
        Assert.NotNull(form);
        Assert.NotEmpty(form.Questions);
        var firstQuestionId = form.Questions[0].Id;

        var createCmd = new
        {
            nombre = "AnswerTest",
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

        var createResponse = await operadorClient.PostAsJsonAsync("/api/trivia-games", createCmd);
        var created = await createResponse.Content.ReadFromJsonAsync<TriviaGameDetailDto>();
        Assert.NotNull(created);
        var gameId = created.Id;

        var joinClient = CreateParticipanteClient();
        var joinResponse = await joinClient.PostAsync(
            $"/api/trivia-games/{gameId}/join", null);
        Assert.Equal(HttpStatusCode.OK, joinResponse.StatusCode);

        var startResponse = await operadorClient.PostAsync(
            $"/api/trivia-games/{gameId}/start", null);
        var started = await startResponse.Content.ReadFromJsonAsync<TriviaGameDetailDto>();
        Assert.NotNull(started);
        Assert.Equal("Iniciada", started.Estado);

        return (joinClient, gameId, firstQuestionId);
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
            title = "Answer Test Form",
            questions = new[]
            {
                new
                {
                    text = "First question?",
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
                new
                {
                    text = "Second question?",
                    assignedScore = 200,
                    timeLimitSeconds = 30,
                    displayOrder = 2,
                    options = new[]
                    {
                        new { text = "Correct2", isCorrect = true },
                        new { text = "WrongA", isCorrect = false },
                        new { text = "WrongB", isCorrect = false },
                        new { text = "WrongC", isCorrect = false },
                    },
                },
            },
        };

        var response = await client.PostAsJsonAsync("/api/trivia-forms", command);
        var body = await response.Content.ReadFromJsonAsync<TriviaFormDetailDto>();
        return body!.Id;
    }
}
