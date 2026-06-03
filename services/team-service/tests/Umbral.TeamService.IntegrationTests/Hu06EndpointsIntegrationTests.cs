using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Umbral.TeamService.IntegrationTests;

public sealed class Hu06EndpointsIntegrationTests : IClassFixture<TeamApiFactory>
{
    private readonly HttpClient _client;

    public Hu06EndpointsIntegrationTests(TeamApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PatchTeamsLeadership_Should_Return_Unauthorized_When_Unauthenticated()
    {
        var response = await _client.PatchAsJsonAsync("/api/teams/leadership", new { nuevoLiderUserId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PatchTeamsLeadership_Should_Return_Forbidden_For_NonParticipant()
    {
        var request = CreateTransferRequest(Guid.NewGuid().ToString(), Guid.NewGuid(), "Operador");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PatchTeamsLeadership_Should_Return_BadRequest_For_Empty_Target()
    {
        var request = CreateTransferRequest(Guid.NewGuid().ToString(), Guid.Empty);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PatchTeamsLeadership_Should_Return_NotFound_When_No_ActiveTeam_Exists()
    {
        var request = CreateTransferRequest(Guid.NewGuid().ToString(), Guid.NewGuid());

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PatchTeamsLeadership_Should_Return_Ok_When_Leader_Transfers_To_Member()
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
        Assert.Equal(leaderUserId, document.RootElement.GetProperty("liderAnteriorUserId").GetString());
        Assert.Equal(targetUserId.ToString(), document.RootElement.GetProperty("nuevoLiderUserId").GetString());
        Assert.Equal("Activo", document.RootElement.GetProperty("equipoEstado").GetString());
    }

    [Fact]
    public async Task PatchTeamsLeadership_Should_Return_Conflict_When_Actor_Is_Not_Leader()
    {
        var leaderUserId = Guid.NewGuid().ToString();
        var actorUserId = Guid.NewGuid().ToString();
        var targetUserId = Guid.NewGuid();
        var accessCode = await CreateTeamAndGetAccessCodeAsync(leaderUserId);
        await JoinTeamAsync(accessCode, actorUserId);
        await JoinTeamAsync(accessCode, targetUserId.ToString());

        var response = await _client.SendAsync(CreateTransferRequest(actorUserId, targetUserId));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PatchTeamsLeadership_Should_Return_Conflict_When_Target_Is_Not_Member()
    {
        var leaderUserId = Guid.NewGuid().ToString();
        var accessCode = await CreateTeamAndGetAccessCodeAsync(leaderUserId);
        await JoinTeamAsync(accessCode, Guid.NewGuid().ToString());

        var response = await _client.SendAsync(CreateTransferRequest(leaderUserId, Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PatchTeamsLeadership_Should_Return_Conflict_When_Target_Is_Current_Leader()
    {
        var leaderUserId = Guid.NewGuid();
        var accessCode = await CreateTeamAndGetAccessCodeAsync(leaderUserId.ToString());
        await JoinTeamAsync(accessCode, Guid.NewGuid().ToString());

        var response = await _client.SendAsync(CreateTransferRequest(leaderUserId.ToString(), leaderUserId));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PatchTeamsLeadership_Should_Allow_Former_Leader_To_Leave_After_Transfer()
    {
        var leaderUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var accessCode = await CreateTeamAndGetAccessCodeAsync(leaderUserId.ToString());
        await JoinTeamAsync(accessCode, targetUserId.ToString());
        var transferResponse = await _client.SendAsync(CreateTransferRequest(leaderUserId.ToString(), targetUserId));
        Assert.Equal(HttpStatusCode.OK, transferResponse.StatusCode);

        var leaveRequest = new HttpRequestMessage(HttpMethod.Delete, "/api/teams/membership");
        leaveRequest.Headers.Add("X-Test-Role", "Participante");
        leaveRequest.Headers.Add("X-Test-UserId", leaderUserId.ToString());

        var leaveResponse = await _client.SendAsync(leaveRequest);

        Assert.Equal(HttpStatusCode.OK, leaveResponse.StatusCode);
    }

    private static HttpRequestMessage CreateTransferRequest(string userId, Guid newLeaderUserId, string role = "Participante")
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, "/api/teams/leadership")
        {
            Content = JsonContent.Create(new { nuevoLiderUserId = newLeaderUserId })
        };
        request.Headers.Add("X-Test-Role", role);
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
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Unexpected status: {response.StatusCode}. Body: {body}");
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
        var body = await createResponse.Content.ReadAsStringAsync();
        Assert.True(createResponse.StatusCode == HttpStatusCode.Created, $"Unexpected status: {createResponse.StatusCode}. Body: {body}");

        using var document = JsonDocument.Parse(body);
        return document.RootElement.GetProperty("codigoAcceso").GetString()!;
    }
}
