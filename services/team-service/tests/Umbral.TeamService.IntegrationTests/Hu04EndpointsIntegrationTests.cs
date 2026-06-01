using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Umbral.TeamService.IntegrationTests;

public sealed class Hu04EndpointsIntegrationTests : IClassFixture<TeamApiFactory>
{
    private readonly HttpClient _client;

    public Hu04EndpointsIntegrationTests(TeamApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostTeamsJoinByCode_Should_Return_Unauthorized_When_Unauthenticated()
    {
        var response = await _client.PostAsJsonAsync("/api/teams/join-by-code", new { codigoAcceso = "ABCD1234" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostTeamsJoinByCode_Should_Return_Forbidden_For_NonParticipant()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/teams/join-by-code")
        {
            Content = JsonContent.Create(new { codigoAcceso = "ABCD1234" })
        };
        request.Headers.Add("X-Test-Role", "Operador");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PostTeamsJoinByCode_Should_Return_BadRequest_For_InvalidPayload()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/teams/join-by-code")
        {
            Content = JsonContent.Create(new { codigoAcceso = "" })
        };
        request.Headers.Add("X-Test-Role", "Participante");
        request.Headers.Add("X-Test-UserId", Guid.NewGuid().ToString());

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostTeamsJoinByCode_Should_Return_NotFound_When_Code_Does_Not_Exist()
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
    public async Task PostTeamsJoinByCode_Should_Return_Ok_When_Code_Is_Valid()
    {
        var accessCode = await CreateTeamAndGetAccessCodeAsync(Guid.NewGuid().ToString());

        var joinRequest = new HttpRequestMessage(HttpMethod.Post, "/api/teams/join-by-code")
        {
            Content = JsonContent.Create(new { codigoAcceso = accessCode })
        };
        var joiningUserId = Guid.NewGuid().ToString();
        joinRequest.Headers.Add("X-Test-Role", "Participante");
        joinRequest.Headers.Add("X-Test-UserId", joiningUserId);

        var response = await _client.SendAsync(joinRequest);

        var errorBody = await response.Content.ReadAsStringAsync();

        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Unexpected status: {response.StatusCode}. Body: {errorBody}");

        using var document = JsonDocument.Parse(errorBody);
        var integrantes = document.RootElement.GetProperty("integrantes");
        Assert.Contains(integrantes.EnumerateArray(), x => x.GetProperty("userId").GetString() == joiningUserId);
    }

    [Fact]
    public async Task PostTeamsJoinByCode_Should_Return_Conflict_When_Target_Team_Is_Full()
    {
        var accessCode = await CreateTeamAndGetAccessCodeAsync(Guid.NewGuid().ToString());

        for (var i = 0; i < 4; i++)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/teams/join-by-code")
            {
                Content = JsonContent.Create(new { codigoAcceso = accessCode })
            };
            request.Headers.Add("X-Test-Role", "Participante");
            request.Headers.Add("X-Test-UserId", Guid.NewGuid().ToString());
            var response = await _client.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Unexpected status: {response.StatusCode}. Body: {responseBody}");
        }

        var fullRequest = new HttpRequestMessage(HttpMethod.Post, "/api/teams/join-by-code")
        {
            Content = JsonContent.Create(new { codigoAcceso = accessCode })
        };
        fullRequest.Headers.Add("X-Test-Role", "Participante");
        fullRequest.Headers.Add("X-Test-UserId", Guid.NewGuid().ToString());

        var fullResponse = await _client.SendAsync(fullRequest);

        Assert.Equal(HttpStatusCode.Conflict, fullResponse.StatusCode);
    }

    [Fact]
    public async Task PostTeamsJoinByCode_Should_Preserve_Max5_When_Two_Joins_Arrive_Concurrently_With_4_Members()
    {
        var accessCode = await CreateTeamAndGetAccessCodeAsync(Guid.NewGuid().ToString());

        for (var i = 0; i < 3; i++)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/teams/join-by-code")
            {
                Content = JsonContent.Create(new { codigoAcceso = accessCode })
            };
            request.Headers.Add("X-Test-Role", "Participante");
            request.Headers.Add("X-Test-UserId", Guid.NewGuid().ToString());
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        var concurrentUserA = Guid.NewGuid().ToString();
        var concurrentUserB = Guid.NewGuid().ToString();

        Task<HttpResponseMessage> SendJoinAsync(string userId)
        {
            var joinRequest = new HttpRequestMessage(HttpMethod.Post, "/api/teams/join-by-code")
            {
                Content = JsonContent.Create(new { codigoAcceso = accessCode })
            };
            joinRequest.Headers.Add("X-Test-Role", "Participante");
            joinRequest.Headers.Add("X-Test-UserId", userId);
            return _client.SendAsync(joinRequest);
        }

        var responses = await Task.WhenAll(SendJoinAsync(concurrentUserA), SendJoinAsync(concurrentUserB));
        var statusCodes = responses.Select(x => x.StatusCode).ToArray();
        Assert.Contains(HttpStatusCode.OK, statusCodes);
        Assert.Contains(HttpStatusCode.Conflict, statusCodes);

        var winnerResponse = responses.Single(x => x.StatusCode == HttpStatusCode.OK);
        var winnerPayload = await winnerResponse.Content.ReadAsStringAsync();
        using var winnerDoc = JsonDocument.Parse(winnerPayload);
        var integrantes = winnerDoc.RootElement.GetProperty("integrantes");
        Assert.Equal(5, integrantes.GetArrayLength());

        var loserResponse = responses.Single(x => x.StatusCode == HttpStatusCode.Conflict);
        var loserBody = await loserResponse.Content.ReadAsStringAsync();
        Assert.Contains("maximo", loserBody, StringComparison.OrdinalIgnoreCase);
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
