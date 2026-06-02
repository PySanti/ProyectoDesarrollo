using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Umbral.TeamService.IntegrationTests;

public sealed class Hu07EndpointsIntegrationTests : IClassFixture<TeamApiFactory>
{
    private readonly HttpClient _client;

    public Hu07EndpointsIntegrationTests(TeamApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task DeleteTeamsMembership_Should_Return_Unauthorized_When_Unauthenticated()
    {
        var response = await _client.DeleteAsync("/api/teams/membership");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteTeamsMembership_Should_Return_Forbidden_For_NonParticipant()
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, "/api/teams/membership");
        request.Headers.Add("X-Test-Role", "Operador");
        request.Headers.Add("X-Test-UserId", Guid.NewGuid().ToString());

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteTeamsMembership_Should_Return_NotFound_When_No_ActiveTeam_Exists()
    {
        var request = CreateLeaveRequest(Guid.NewGuid().ToString());

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteTeamsMembership_Should_Return_Ok_When_NonLeader_Leaves()
    {
        var accessCode = await CreateTeamAndGetAccessCodeAsync(Guid.NewGuid().ToString());
        var joiningUserId = Guid.NewGuid().ToString();
        await JoinTeamAsync(accessCode, joiningUserId);

        var response = await _client.SendAsync(CreateLeaveRequest(joiningUserId));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(body);
        Assert.Equal(joiningUserId, document.RootElement.GetProperty("userId").GetString());
        Assert.Equal("SalioDelEquipo", document.RootElement.GetProperty("resultado").GetString());
        Assert.Equal("Activo", document.RootElement.GetProperty("equipoEstado").GetString());
    }

    [Fact]
    public async Task DeleteTeamsMembership_Should_Return_Ok_And_DeleteTeam_When_OnlyLeader_Leaves()
    {
        var leaderUserId = Guid.NewGuid().ToString();
        await CreateTeamAndGetAccessCodeAsync(leaderUserId);

        var response = await _client.SendAsync(CreateLeaveRequest(leaderUserId));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(body);
        Assert.Equal(leaderUserId, document.RootElement.GetProperty("userId").GetString());
        Assert.Equal("EquipoEliminado", document.RootElement.GetProperty("resultado").GetString());
        Assert.Equal("Eliminado", document.RootElement.GetProperty("equipoEstado").GetString());
    }

    [Fact]
    public async Task DeleteTeamsMembership_Should_Return_Conflict_When_Leader_Has_Other_Members()
    {
        var leaderUserId = Guid.NewGuid().ToString();
        var accessCode = await CreateTeamAndGetAccessCodeAsync(leaderUserId);
        await JoinTeamAsync(accessCode, Guid.NewGuid().ToString());

        var response = await _client.SendAsync(CreateLeaveRequest(leaderUserId));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("transferir", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteTeamsMembership_Should_Allow_User_To_Create_Team_After_Leaving()
    {
        var accessCode = await CreateTeamAndGetAccessCodeAsync(Guid.NewGuid().ToString());
        var joiningUserId = Guid.NewGuid().ToString();
        await JoinTeamAsync(accessCode, joiningUserId);
        var leaveResponse = await _client.SendAsync(CreateLeaveRequest(joiningUserId));
        Assert.Equal(HttpStatusCode.OK, leaveResponse.StatusCode);

        var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/teams")
        {
            Content = JsonContent.Create(new { nombreEquipo = $"Equipo-{Guid.NewGuid():N}" })
        };
        createRequest.Headers.Add("X-Test-Role", "Participante");
        createRequest.Headers.Add("X-Test-UserId", joiningUserId);

        var createResponse = await _client.SendAsync(createRequest);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
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
