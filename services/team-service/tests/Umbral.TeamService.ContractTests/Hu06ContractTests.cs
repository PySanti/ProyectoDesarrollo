using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Umbral.TeamService.ContractTests;

public sealed class Hu06ContractTests : IClassFixture<TeamApiFactory>
{
    private readonly HttpClient _client;

    public Hu06ContractTests(TeamApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PatchTeamsLeadership_Should_Match_Contract_Status_And_Response_Shape()
    {
        var leaderUserId = Guid.NewGuid().ToString();
        var targetUserId = Guid.NewGuid();
        var accessCode = await CreateTeamAndGetAccessCodeAsync(leaderUserId);
        await JoinTeamAsync(accessCode, targetUserId.ToString());

        var response = await _client.SendAsync(CreateTransferRequest(leaderUserId, targetUserId));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(body);
        Assert.True(document.RootElement.TryGetProperty("equipoId", out _));
        Assert.True(document.RootElement.TryGetProperty("liderAnteriorUserId", out _));
        Assert.True(document.RootElement.TryGetProperty("nuevoLiderUserId", out _));
        Assert.True(document.RootElement.TryGetProperty("equipoEstado", out _));
    }

    [Fact]
    public async Task PatchTeamsLeadership_Should_Match_Contract_NotFound_Status()
    {
        var response = await _client.SendAsync(CreateTransferRequest(Guid.NewGuid().ToString(), Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PatchTeamsLeadership_Should_Match_Contract_Conflict_Status()
    {
        var leaderUserId = Guid.NewGuid();
        var accessCode = await CreateTeamAndGetAccessCodeAsync(leaderUserId.ToString());
        await JoinTeamAsync(accessCode, Guid.NewGuid().ToString());

        var response = await _client.SendAsync(CreateTransferRequest(leaderUserId.ToString(), leaderUserId));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private static HttpRequestMessage CreateTransferRequest(string userId, Guid newLeaderUserId)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, "/api/teams/leadership")
        {
            Content = JsonContent.Create(new { nuevoLiderUserId = newLeaderUserId })
        };
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
