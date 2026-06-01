using System.Net;
using System.Net.Http.Json;

namespace Umbral.TeamService.IntegrationTests;

public sealed class Hu03EndpointsIntegrationTests : IClassFixture<TeamApiFactory>
{
    private readonly HttpClient _client;

    public Hu03EndpointsIntegrationTests(TeamApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostTeams_Should_Return_Unauthorized_When_Unauthenticated()
    {
        var response = await _client.PostAsJsonAsync("/api/teams", new { nombreEquipo = "Exploradores" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostTeams_Should_Return_Forbidden_For_NonParticipant()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/teams")
        {
            Content = JsonContent.Create(new { nombreEquipo = "Exploradores" })
        };
        request.Headers.Add("X-Test-Role", "Operador");

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PostTeams_Should_Return_BadRequest_For_InvalidPayload()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/teams")
        {
            Content = JsonContent.Create(new { nombreEquipo = "" })
        };
        request.Headers.Add("X-Test-Role", "Participante");

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostTeams_Should_Return_Created_When_Valid()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/teams")
        {
            Content = JsonContent.Create(new { nombreEquipo = "Exploradores" })
        };
        request.Headers.Add("X-Test-Role", "Participante");
        request.Headers.Add("X-Test-UserId", Guid.NewGuid().ToString());

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task PostTeams_Should_Return_Conflict_When_User_Already_In_ActiveTeam()
    {
        var userId = Guid.NewGuid().ToString();

        var first = new HttpRequestMessage(HttpMethod.Post, "/api/teams")
        {
            Content = JsonContent.Create(new { nombreEquipo = "Exploradores A" })
        };
        first.Headers.Add("X-Test-Role", "Participante");
        first.Headers.Add("X-Test-UserId", userId);
        var firstResponse = await _client.SendAsync(first);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var second = new HttpRequestMessage(HttpMethod.Post, "/api/teams")
        {
            Content = JsonContent.Create(new { nombreEquipo = "Exploradores B" })
        };
        second.Headers.Add("X-Test-Role", "Participante");
        second.Headers.Add("X-Test-UserId", userId);
        var secondResponse = await _client.SendAsync(second);

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }
}
