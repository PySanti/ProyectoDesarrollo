using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Umbral.TeamService.ContractTests;

public sealed class Hu04ContractTests : IClassFixture<TeamApiFactory>
{
    private readonly HttpClient _client;

    public Hu04ContractTests(TeamApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostTeamsJoinByCode_Should_Match_Contract_Status_And_Response_Shape()
    {
        var accessCode = await CreateTeamAndGetAccessCodeAsync(Guid.NewGuid().ToString());
        var joiningUserId = Guid.NewGuid().ToString();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/teams/join-by-code")
        {
            Content = JsonContent.Create(new { codigoAcceso = accessCode })
        };
        request.Headers.Add("X-Test-Role", "Participante");
        request.Headers.Add("X-Test-UserId", joiningUserId);

        var response = await _client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(document.RootElement.TryGetProperty("equipoId", out _));
        Assert.True(document.RootElement.TryGetProperty("nombreEquipo", out _));
        Assert.True(document.RootElement.TryGetProperty("codigoAcceso", out _));
        Assert.True(document.RootElement.TryGetProperty("estado", out _));
        Assert.True(document.RootElement.TryGetProperty("liderUserId", out _));
        Assert.True(document.RootElement.TryGetProperty("integrantes", out var integrantes));
        Assert.Equal(JsonValueKind.Array, integrantes.ValueKind);
        Assert.Equal(2, integrantes.GetArrayLength());
        Assert.Contains(integrantes.EnumerateArray(), x => x.GetProperty("userId").GetString() == joiningUserId && !x.GetProperty("esLider").GetBoolean());
    }

    [Fact]
    public async Task PostTeamsJoinByCode_Should_Match_Contract_NotFound_Status()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/teams/join-by-code")
        {
            Content = JsonContent.Create(new { codigoAcceso = "MISSING99" })
        };
        request.Headers.Add("X-Test-Role", "Participante");
        request.Headers.Add("X-Test-UserId", Guid.NewGuid().ToString());

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostTeamsJoinByCode_Should_Match_Contract_Conflict_Status()
    {
        var userId = Guid.NewGuid().ToString();
        var firstAccessCode = await CreateTeamAndGetAccessCodeAsync(Guid.NewGuid().ToString());
        var joinRequest = new HttpRequestMessage(HttpMethod.Post, "/api/teams/join-by-code")
        {
            Content = JsonContent.Create(new { codigoAcceso = firstAccessCode })
        };
        joinRequest.Headers.Add("X-Test-Role", "Participante");
        joinRequest.Headers.Add("X-Test-UserId", userId);
        var joinResponse = await _client.SendAsync(joinRequest);
        Assert.Equal(HttpStatusCode.OK, joinResponse.StatusCode);

        var secondAccessCode = await CreateTeamAndGetAccessCodeAsync(Guid.NewGuid().ToString());
        var conflictRequest = new HttpRequestMessage(HttpMethod.Post, "/api/teams/join-by-code")
        {
            Content = JsonContent.Create(new { codigoAcceso = secondAccessCode })
        };
        conflictRequest.Headers.Add("X-Test-Role", "Participante");
        conflictRequest.Headers.Add("X-Test-UserId", userId);

        var conflictResponse = await _client.SendAsync(conflictRequest);

        Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);
    }

    private async Task<string> CreateTeamAndGetAccessCodeAsync(string creatorUserId)
    {
        var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/teams")
        {
            Content = JsonContent.Create(new { nombreEquipo = $"Equipo-{Guid.NewGuid():N}" })
        };
        createRequest.Headers.Add("X-Test-Role", "Participante");
        createRequest.Headers.Add("X-Test-UserId", creatorUserId);

        var createResponse = await _client.SendAsync(createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createBody = await createResponse.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(createBody);
        return document.RootElement.GetProperty("codigoAcceso").GetString()!;
    }
}
