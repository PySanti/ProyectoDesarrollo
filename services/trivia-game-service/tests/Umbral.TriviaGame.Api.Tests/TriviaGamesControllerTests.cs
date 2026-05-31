using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.TestHost;
using Umbral.TriviaGame.Application.Dtos;

namespace Umbral.TriviaGame.Api.Tests;

public sealed class TriviaGamesControllerTests : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public TriviaGamesControllerTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateIndividual_Valid_Returns201WithGameDetail()
    {
        var client = _factory.CreateClient();
        var formId = await CreateValidFormAsync(client);

        var command = new
        {
            nombre = "Mi Partida",
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

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TriviaGameDetailDto>();
        Assert.NotNull(body);
        Assert.Equal("Mi Partida", body.Nombre);
        Assert.Equal("Lobby", body.Estado);
        Assert.Equal("Individual", body.Modalidad);
        Assert.Equal(10, body.MaximoJugadores);
        Assert.Null(body.MaximoEquipos);
    }

    [Fact]
    public async Task CreateEquipo_Valid_Returns201()
    {
        var client = _factory.CreateClient();
        var formId = await CreateValidFormAsync(client);

        var command = new
        {
            nombre = "Partida Equipos",
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

        var response = await client.PostAsJsonAsync("/api/trivia-games", command);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TriviaGameDetailDto>();
        Assert.NotNull(body);
        Assert.Equal("Partida Equipos", body.Nombre);
        Assert.Equal("Equipo", body.Modalidad);
        Assert.Equal(5, body.MaximoEquipos);
        Assert.Equal(1, body.MinimoJugadoresPorEquipo);
        Assert.Equal(4, body.MaximoJugadoresPorEquipo);
    }

    [Fact]
    public async Task Create_FormNotExists_Returns404()
    {
        var client = _factory.CreateClient();

        var command = new
        {
            nombre = "NoForm",
            modalidad = "Individual",
            modoInicio = "Manual",
            formularioId = Guid.NewGuid(),
            tiempoInicio = DateTimeOffset.UtcNow.AddHours(1),
            minimoParticipantes = 1,
            maximoJugadores = 10,
            maximoEquipos = (int?)null,
            minimoJugadoresPorEquipo = (int?)null,
            maximoJugadoresPorEquipo = (int?)null,
        };

        var response = await client.PostAsJsonAsync("/api/trivia-games", command);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_InvalidData_Returns400()
    {
        var client = _factory.CreateClient();
        var formId = await CreateValidFormAsync(client);

        var command = new
        {
            nombre = "Test",
            modalidad = "Individual",
            modoInicio = "Manual",
            formularioId = formId,
            tiempoInicio = DateTimeOffset.UtcNow.AddHours(1),
            minimoParticipantes = 0,
            maximoJugadores = (int?)null,
            maximoEquipos = (int?)null,
            minimoJugadoresPorEquipo = (int?)null,
            maximoJugadoresPorEquipo = (int?)null,
        };

        var response = await client.PostAsJsonAsync("/api/trivia-games", command);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateThenGet_ReturnsSameGame()
    {
        var client = _factory.CreateClient();
        var formId = await CreateValidFormAsync(client);

        var createCmd = new
        {
            nombre = "GetTest",
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

        var getResponse = await client.GetAsync($"/api/trivia-games/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<TriviaGameDetailDto>();
        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched.Id);
        Assert.Equal(created.Nombre, fetched.Nombre);
        Assert.Equal(created.Estado, fetched.Estado);
    }

    [Fact]
    public async Task Get_NonExistentGame_Returns404()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/trivia-games/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateGame_WithMinimos_Returns201WithLobby()
    {
        var client = _factory.CreateClient();
        var formId = await CreateValidFormAsync(client);

        var createCmd = new
        {
            nombre = "LobbyTest",
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
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<TriviaGameDetailDto>();
        Assert.NotNull(created);
        Assert.Equal("Lobby", created.Estado);
    }

    [Fact]
    public async Task Start_WhenMinimosNoCumplidos_Returns409()
    {
        var client = _factory.CreateClient();
        var formId = await CreateValidFormAsync(client);

        var createCmd = new
        {
            nombre = "NoMinimos",
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

        var startResponse = await client.PostAsync($"/api/trivia-games/{created.Id}/start", null);

        Assert.Equal(HttpStatusCode.Conflict, startResponse.StatusCode);
    }

    [Fact]
    public async Task Start_NonExistentGame_Returns404()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync($"/api/trivia-games/{Guid.NewGuid()}/start", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithoutOperadorRole_Returns403()
    {
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(Testing.TestClaimsProvider));
                if (descriptor is not null) services.Remove(descriptor);
                services.AddScoped(_ => Testing.TestClaimsProvider.WithoutOperadorRole());
            });
        }).CreateClient();

        var command = new
        {
            nombre = "Unauthorized",
            modalidad = "Individual",
            modoInicio = "Manual",
            formularioId = Guid.NewGuid(),
            tiempoInicio = DateTimeOffset.UtcNow.AddHours(1),
            minimoParticipantes = 1,
            maximoJugadores = 10,
            maximoEquipos = (int?)null,
            minimoJugadoresPorEquipo = (int?)null,
            maximoJugadoresPorEquipo = (int?)null,
        };

        var response = await client.PostAsJsonAsync("/api/trivia-games", command);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static async Task<Guid> CreateValidFormAsync(HttpClient client)
    {
        var command = new
        {
            title = "Form for Game",
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
}
