using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Umbral.BdtGameService.Infrastructure.Persistence;

namespace Umbral.BdtGameService.IntegrationTests;

public sealed class Hu34CreateBdtGameIntegrationTests : IClassFixture<BdtApiFactory>
{
    private readonly BdtApiFactory _factory;
    private readonly HttpClient _client;

    public Hu34CreateBdtGameIntegrationTests(BdtApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostBdtGames_Should_Create_Lobby_Game_For_Operator()
    {
        await ClearDatabaseAsync();
        var request = CreatePostRequest(ValidIndividualPayload());

        var response = await _client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var document = JsonDocument.Parse(body);
        Assert.Equal("Busqueda QR Campus", document.RootElement.GetProperty("nombre").GetString());
        Assert.Equal("Individual", document.RootElement.GetProperty("modalidad").GetString());
        Assert.Equal("Lobby", document.RootElement.GetProperty("estado").GetString());
        Assert.Equal("Manual", document.RootElement.GetProperty("modoInicio").GetString());
        Assert.Equal(1, document.RootElement.GetProperty("cantidadEtapas").GetInt32());
        Assert.Equal(1, await CountPartidasAsync());
    }

    [Fact]
    public async Task PostBdtGames_Should_Return_BadRequest_For_Missing_Stages()
    {
        var payload = ValidIndividualPayload() with { Etapas = Array.Empty<CreateStagePayload>() };

        var response = await _client.SendAsync(CreatePostRequest(payload));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostBdtGames_Should_Return_BadRequest_For_Null_Stages()
    {
        var payload = ValidIndividualPayload() with { Etapas = null! };

        var response = await _client.SendAsync(CreatePostRequest(payload));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("", 300)]
    [InlineData("QR-ETAPA-1", 0)]
    public async Task PostBdtGames_Should_Return_BadRequest_For_Invalid_Stage(string codigoQrEsperado, int tiempoLimiteSegundos)
    {
        var payload = ValidIndividualPayload() with
        {
            Etapas = new[] { new CreateStagePayload(1, codigoQrEsperado, tiempoLimiteSegundos) }
        };

        var response = await _client.SendAsync(CreatePostRequest(payload));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("Carrera")]
    [InlineData("ManualAutomatico")]
    public async Task PostBdtGames_Should_Return_BadRequest_For_Invalid_Enums(string invalidValue)
    {
        var invalidModality = ValidIndividualPayload() with { Modalidad = invalidValue };
        var invalidStartMode = ValidIndividualPayload() with { ModoInicio = invalidValue };

        var modalityResponse = await _client.SendAsync(CreatePostRequest(invalidModality));
        var startModeResponse = await _client.SendAsync(CreatePostRequest(invalidStartMode));

        Assert.Equal(HttpStatusCode.BadRequest, modalityResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, startModeResponse.StatusCode);
    }

    [Fact]
    public async Task PostBdtGames_Should_Return_Conflict_For_Invalid_Modality_Limits()
    {
        var payload = ValidIndividualPayload() with { MaximoParticipantes = null };

        var response = await _client.SendAsync(CreatePostRequest(payload));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetOperatorPublishedBdtGames_Should_Return_Created_Game_After_Post()
    {
        await ClearDatabaseAsync();
        var createResponse = await _client.SendAsync(CreatePostRequest(ValidIndividualPayload()));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var listRequest = new HttpRequestMessage(HttpMethod.Get, "/api/bdt/operator/games/published");
        listRequest.Headers.Add("X-Test-Role", "Operador");
        listRequest.Headers.Add("X-Test-UserId", Guid.NewGuid().ToString());

        var listResponse = await _client.SendAsync(listRequest);
        var body = await listResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        using var document = JsonDocument.Parse(body);
        Assert.Single(document.RootElement.EnumerateArray());
        Assert.Equal("Busqueda QR Campus", document.RootElement[0].GetProperty("nombre").GetString());
    }

    [Fact]
    public async Task PostBdtGames_Should_Return_Unauthorized_When_Unauthenticated()
    {
        var response = await _client.PostAsJsonAsync("/api/bdt/games", ValidIndividualPayload());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostBdtGames_Should_Return_Forbidden_For_NonOperator()
    {
        var response = await _client.SendAsync(CreatePostRequest(ValidIndividualPayload(), role: "Participante"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static HttpRequestMessage CreatePostRequest(CreateBdtPayload payload, string role = "Operador")
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/bdt/games")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("X-Test-Role", role);
        request.Headers.Add("X-Test-UserId", Guid.NewGuid().ToString());
        return request;
    }

    private async Task ClearDatabaseAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BdtDbContext>();
        dbContext.Partidas.RemoveRange(dbContext.Partidas);
        await dbContext.SaveChangesAsync();
    }

    private async Task<int> CountPartidasAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BdtDbContext>();
        return await dbContext.Partidas.CountAsync();
    }

    private static CreateBdtPayload ValidIndividualPayload()
    {
        return new CreateBdtPayload(
            "Busqueda QR Campus",
            "Patio central y biblioteca",
            "Individual",
            2,
            20,
            null,
            null,
            "Manual",
            new[] { new CreateStagePayload(1, "QR-ETAPA-1", 300) });
    }

    private sealed record CreateBdtPayload(
        string Nombre,
        string AreaBusqueda,
        string Modalidad,
        int MinimoParticipantes,
        int? MaximoParticipantes,
        int? MaximoEquipos,
        int? MinimoJugadoresPorEquipo,
        string ModoInicio,
        IReadOnlyList<CreateStagePayload> Etapas);

    private sealed record CreateStagePayload(int Orden, string CodigoQrEsperado, int TiempoLimiteSegundos);
}
