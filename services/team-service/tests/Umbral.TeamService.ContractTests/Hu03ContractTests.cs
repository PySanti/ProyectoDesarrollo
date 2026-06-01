using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Umbral.TeamService.ContractTests;

public sealed class Hu03ContractTests : IClassFixture<TeamApiFactory>
{
    private readonly HttpClient _client;

    public Hu03ContractTests(TeamApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostTeams_Should_Match_Contract_Status_And_Response_Shape()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/teams")
        {
            Content = JsonContent.Create(new { nombreEquipo = "Exploradores" })
        };
        request.Headers.Add("X-Test-Role", "Participante");
        request.Headers.Add("X-Test-UserId", Guid.NewGuid().ToString());

        var response = await _client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.True(document.RootElement.TryGetProperty("equipoId", out _));
        Assert.True(document.RootElement.TryGetProperty("nombreEquipo", out _));
        Assert.True(document.RootElement.TryGetProperty("codigoAcceso", out _));
        Assert.True(document.RootElement.TryGetProperty("estado", out _));
        Assert.True(document.RootElement.TryGetProperty("liderUserId", out _));
        Assert.True(document.RootElement.TryGetProperty("integrantes", out var integrantes));
        Assert.Equal(JsonValueKind.Array, integrantes.ValueKind);
        Assert.Equal(1, integrantes.GetArrayLength());

        var first = integrantes[0];
        Assert.True(first.TryGetProperty("userId", out _));
        Assert.True(first.TryGetProperty("esLider", out _));
    }

    [Fact]
    public async Task PostTeams_Should_Match_Contract_Conflict_Status()
    {
        var userId = Guid.NewGuid().ToString();

        var first = new HttpRequestMessage(HttpMethod.Post, "/api/teams")
        {
            Content = JsonContent.Create(new { nombreEquipo = "Equipo Uno" })
        };
        first.Headers.Add("X-Test-Role", "Participante");
        first.Headers.Add("X-Test-UserId", userId);
        var firstResponse = await _client.SendAsync(first);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var second = new HttpRequestMessage(HttpMethod.Post, "/api/teams")
        {
            Content = JsonContent.Create(new { nombreEquipo = "Equipo Dos" })
        };
        second.Headers.Add("X-Test-Role", "Participante");
        second.Headers.Add("X-Test-UserId", userId);

        var secondResponse = await _client.SendAsync(second);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }
}
