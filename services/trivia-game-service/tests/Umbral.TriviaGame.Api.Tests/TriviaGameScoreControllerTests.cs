using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.TestHost;
using Umbral.TriviaGame.Application.Dtos;

namespace Umbral.TriviaGame.Api.Tests;

public sealed class TriviaGameScoreControllerTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public TriviaGameScoreControllerTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetScore_UnaRespuestaCorrecta_RetornaPuntajeDePregunta()
    {
        var (client, gameId, operadorClient, formId, _) = await SetupGameAsync();

        var formDetailResponse = await operadorClient.GetAsync($"/api/trivia-forms/{formId}");
        var formDetail = await formDetailResponse.Content.ReadFromJsonAsync<TriviaFormDetailDto>();
        Assert.NotNull(formDetail);
        var firstQuestion = formDetail.Questions.OrderBy(q => q.DisplayOrder).First();
        var correctIndexQ1 = firstQuestion.Options.First(o => o.IsCorrect).Index;

        var answer = new { opcionIndex = correctIndexQ1 };
        var answerResponse = await client.PostAsJsonAsync(
            $"/api/trivia-games/{gameId}/questions/{firstQuestion.Id}/answer", answer);
        Assert.Equal(HttpStatusCode.OK, answerResponse.StatusCode);
        var answerBody = await answerResponse.Content.ReadFromJsonAsync<RespuestaTriviaDto>();
        Assert.NotNull(answerBody);
        Assert.True(answerBody.EsCorrecta);
        Assert.Equal(firstQuestion.AssignedScore, answerBody.PuntajeObtenido);

        var response = await client.GetAsync($"/api/trivia-games/{gameId}/score");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AccumulatedScoreDto>();
        Assert.NotNull(body);
        Assert.Equal(gameId, body.PartidaId);
        Assert.Equal(firstQuestion.AssignedScore, body.PuntajeAcumulado);
        Assert.Equal(1, body.RespuestasCorrectas);
        Assert.Equal(1, body.TotalRespuestas);
    }

    [Fact]
    public async Task GetScore_DosRespuestasCorrectas_RetornaPuntajeAcumulado()
    {
        var (client, gameId, operadorClient, formId, _) = await SetupGameAsync();

        var formResponse = await operadorClient.GetAsync($"/api/trivia-forms/{formId}");
        var form = await formResponse.Content.ReadFromJsonAsync<TriviaFormDetailDto>();
        Assert.NotNull(form);
        Assert.NotEmpty(form.Questions);
        var sortedQuestions = form.Questions.OrderBy(q => q.DisplayOrder).ToList();
        var firstQ = sortedQuestions[0];
        var secondQ = sortedQuestions[1];
        var correctIndexQ1 = firstQ.Options.First(o => o.IsCorrect).Index;
        var correctIndexQ2 = secondQ.Options.First(o => o.IsCorrect).Index;

        var answer1 = new { opcionIndex = correctIndexQ1 };
        var answer1Response = await client.PostAsJsonAsync(
            $"/api/trivia-games/{gameId}/questions/{firstQ.Id}/answer", answer1);
        Assert.Equal(HttpStatusCode.OK, answer1Response.StatusCode);
        var answer1Body = await answer1Response.Content.ReadFromJsonAsync<RespuestaTriviaDto>();
        Assert.NotNull(answer1Body);
        Assert.True(answer1Body.EsCorrecta);
        Assert.Equal(firstQ.AssignedScore, answer1Body.PuntajeObtenido);

        var answer2 = new { opcionIndex = correctIndexQ2 };
        var answer2Response = await client.PostAsJsonAsync(
            $"/api/trivia-games/{gameId}/questions/{secondQ.Id}/answer", answer2);
        Assert.Equal(HttpStatusCode.OK, answer2Response.StatusCode);
        var answer2Body = await answer2Response.Content.ReadFromJsonAsync<RespuestaTriviaDto>();
        Assert.NotNull(answer2Body);
        Assert.True(answer2Body.EsCorrecta);
        Assert.Equal(secondQ.AssignedScore, answer2Body.PuntajeObtenido);

        var scoreResponse = await client.GetAsync($"/api/trivia-games/{gameId}/score");

        Assert.Equal(HttpStatusCode.OK, scoreResponse.StatusCode);
        var body = await scoreResponse.Content.ReadFromJsonAsync<AccumulatedScoreDto>();
        Assert.NotNull(body);
        Assert.Equal(gameId, body.PartidaId);
        Assert.Equal(firstQ.AssignedScore + secondQ.AssignedScore, body.PuntajeAcumulado);
        Assert.True(body.TiempoAcumuladoSegundos >= 0);
        Assert.Equal(2, body.RespuestasCorrectas);
        Assert.Equal(2, body.TotalRespuestas);
    }

    [Fact]
    public async Task GetScore_SinRespuestas_RetornaCeros()
    {
        var (client, gameId, operadorClient, formId, _) = await SetupGameAsync();

        var response = await client.GetAsync($"/api/trivia-games/{gameId}/score");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AccumulatedScoreDto>();
        Assert.NotNull(body);
        Assert.Equal(gameId, body.PartidaId);
        Assert.Equal(0, body.PuntajeAcumulado);
        Assert.Equal(0, body.RespuestasCorrectas);
        Assert.Equal(0, body.TotalRespuestas);
    }

    [Fact]
    public async Task GetScore_GameNotFound_Returns404()
    {
        var client = CreateParticipanteClient();

        var response = await client.GetAsync($"/api/trivia-games/{Guid.NewGuid()}/score");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetScore_Unauthenticated_Returns401()
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

        var response = await client.GetAsync($"/api/trivia-games/{Guid.NewGuid()}/score");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<(HttpClient joinClient, Guid gameId, HttpClient operadorClient, Guid formId, Guid firstQuestionId)> SetupGameAsync()
    {
        var operadorClient = _factory.CreateClient();
        var (formId, firstQuestionId) = await CreateValidFormAsync(operadorClient);

        var createCmd = new
        {
            nombre = "ScoreTest",
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

        return (joinClient, gameId, operadorClient, formId, firstQuestionId);
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

    private static async Task<(Guid formId, Guid firstQuestionId)> CreateValidFormAsync(HttpClient client)
    {
        var command = new
        {
            title = "Score Test Form",
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
        Assert.NotNull(body);
        Assert.NotEmpty(body.Questions);
        var sortedQuestions = body.Questions.OrderBy(q => q.DisplayOrder).ToList();
        return (body.Id, sortedQuestions[0].Id);
    }
}
