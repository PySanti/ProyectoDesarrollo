using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Umbral.TriviaGame.Application.Dtos;
using Umbral.TriviaGame.Domain.Entities;
using Umbral.TriviaGame.Domain.Enums;
using Umbral.TriviaGame.Domain.ValueObjects;
using Umbral.TriviaGame.Infrastructure.Data;

namespace Umbral.TriviaGame.Api.Tests;

public sealed class TriviaGameRankingControllerTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public TriviaGameRankingControllerTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetRanking_GameNotExists_Returns404()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/trivia-games/{Guid.NewGuid()}/ranking");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetRanking_GameExistsNoParticipants_Returns200EmptyList()
    {
        var client = _factory.CreateClient();
        var formId = await CreateValidFormAsync(client);
        var gameId = await CreateIndividualGameAsync(client, formId);

        var response = await client.GetAsync($"/api/trivia-games/{gameId}/ranking");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<RankingEntryDto>>();
        Assert.NotNull(body);
        Assert.Empty(body);
    }

    [Fact]
    public async Task GetRanking_WithParticipants_Returns200WithSortedList()
    {
        var client = _factory.CreateClient();
        var formId = await CreateValidFormAsync(client);
        var gameId = await CreateIndividualGameAsync(client, formId);

        using var scope = _factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<TriviaGameDbContext>();
        var inscripcion1 = TriviaInscripcion.Create(PartidaId.Create(gameId), "user-1");
        var inscripcion2 = TriviaInscripcion.Create(PartidaId.Create(gameId), "user-2");
        ctx.TriviaInscripciones.Add(inscripcion1);
        ctx.TriviaInscripciones.Add(inscripcion2);
        await ctx.SaveChangesAsync();

        var response = await client.GetAsync($"/api/trivia-games/{gameId}/ranking");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<RankingEntryDto>>();
        Assert.NotNull(body);
        Assert.Equal(2, body.Count);
        Assert.Equal(0, body[0].PuntajeAcumulado);
        Assert.Equal(0, body[1].PuntajeAcumulado);
        Assert.Equal(1, body[0].Posicion);
        Assert.Equal(2, body[1].Posicion);
    }

    private static async Task<Guid> CreateValidFormAsync(HttpClient client)
    {
        var command = new
        {
            title = "Form for Ranking",
            questions = new[]
            {
                new
                {
                    text = "Q1?",
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
            nombre = "Ranking Test Game",
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
}
