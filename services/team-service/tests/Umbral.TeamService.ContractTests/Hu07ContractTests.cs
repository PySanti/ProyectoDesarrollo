using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Umbral.TeamService.ContractTests;

public sealed class Hu07ContractTests : IClassFixture<TeamApiFactory>
{
    private readonly HttpClient _client;

    public Hu07ContractTests(TeamApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task DeleteTeamsMembership_Should_Match_Contract_Status_And_Response_Shape()
    {
        var accessCode = await CreateTeamAndGetAccessCodeAsync(Guid.NewGuid().ToString());
        var joiningUserId = Guid.NewGuid().ToString();
        await JoinTeamAsync(accessCode, joiningUserId);
        var request = CreateLeaveRequest(joiningUserId);

        var response = await _client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(body);
        Assert.True(document.RootElement.TryGetProperty("userId", out _));
        Assert.True(document.RootElement.TryGetProperty("equipoId", out _));
        Assert.True(document.RootElement.TryGetProperty("resultado", out _));
        Assert.True(document.RootElement.TryGetProperty("equipoEstado", out _));
    }

    [Fact]
    public async Task DeleteTeamsMembership_Should_Match_Contract_NotFound_Status()
    {
        var response = await _client.SendAsync(CreateLeaveRequest(Guid.NewGuid().ToString()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteTeamsMembership_Should_Match_Contract_Conflict_Status()
    {
        var leaderUserId = Guid.NewGuid().ToString();
        var accessCode = await CreateTeamAndGetAccessCodeAsync(leaderUserId);
        await JoinTeamAsync(accessCode, Guid.NewGuid().ToString());

        var response = await _client.SendAsync(CreateLeaveRequest(leaderUserId));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private static HttpRequestMessage CreateLeaveRequest(string userId)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, "/api/teams/membership");
        request.Headers.Add("X-Test-Role", "Participante");
        request.Headers.Add("X-Test-UserId", userId);
        return request;
    }

    private async Task JoinTeamAsync(string accessCode, string userId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/teams/join-by-code")
        {
            Content = JsonContent.Create(new { codigoAcceso = accessCode })
        };
        request.Headers.Add("X-Test-Role", "Participante");
        request.Headers.Add("X-Test-UserId", userId);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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
