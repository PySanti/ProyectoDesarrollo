using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.TestHost;
using Umbral.TriviaGame.Application.Dtos;

namespace Umbral.TriviaGame.Api.Tests;

public sealed class TriviaGameQuestionResultControllerTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public TriviaGameQuestionResultControllerTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetQuestionResult_DespuesDeRespuestaCorrecta_Returns200ConResultado()
    {
        var (client, gameId, firstQuestionId) = await SetupGameAndAnswerAsync(opcionIndex: 0);

        var response = await client.GetAsync(
            $"/api/trivia-games/{gameId}/questions/{firstQuestionId}/result");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<QuestionResultDto>();
        Assert.NotNull(body);
        Assert.Equal(firstQuestionId, body.PreguntaId);
        Assert.Equal("First question?", body.TextoPregunta);
        Assert.Equal(0, body.OpcionCorrectaIndex);
        Assert.Equal("Correct", body.OpcionCorrectaText);
        Assert.Equal(0, body.MiOpcionIndex);
        Assert.Equal("Correct", body.MiOpcionText);
        Assert.True(body.EsCorrecta);
        Assert.Equal(100, body.PuntajeObtenido);
    }

    [Fact]
    public async Task GetQuestionResult_PreguntaActiva_Returns400()
    {
        var (client, gameId, firstQuestionId) = await SetupGameAsync();

        var response = await client.GetAsync(
            $"/api/trivia-games/{gameId}/questions/{firstQuestionId}/result");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetQuestionResult_GameNotFound_Returns404()
    {
        var client = CreateParticipanteClient();

        var response = await client.GetAsync(
            $"/api/trivia-games/{Guid.NewGuid()}/questions/{Guid.NewGuid()}/result");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetQuestionResult_Unauthenticated_Returns401()
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

        var response = await client.GetAsync(
            $"/api/trivia-games/{Guid.NewGuid()}/questions/{Guid.NewGuid()}/result");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
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
            nombre = "QuestionResultTest",
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

    private async Task<(HttpClient, Guid gameId, Guid firstQuestionId)> SetupGameAndAnswerAsync(int opcionIndex)
    {
        var (client, gameId, firstQuestionId) = await SetupGameAsync();

        var request = new { opcionIndex };
        var answerResponse = await client.PostAsJsonAsync(
            $"/api/trivia-games/{gameId}/questions/{firstQuestionId}/answer", request);
        Assert.Equal(HttpStatusCode.OK, answerResponse.StatusCode);

        return (client, gameId, firstQuestionId);
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
            title = "Result Test Form",
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
