using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Umbral.TeamService.IntegrationTests;

public sealed class Hu04PostgresConcurrencyTests : IClassFixture<PostgresTeamApiFactory>
{
    private readonly PostgresTeamApiFactory _factory;
    private readonly HttpClient _client;

    public Hu04PostgresConcurrencyTests(PostgresTeamApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostTeamsJoinByCode_Should_Preserve_Max5_When_Two_Joins_Arrive_Concurrently_With_4_Members()
    {
        // Reset database to ensure clean state
        await _factory.ResetDatabaseAsync();

        // Create a team with 3 initial members (so we have room for 2 more)
        var accessCode = await CreateTeamAndGetAccessCodeAsync(Guid.NewGuid().ToString());

        // Add 3 more members to reach 4 total
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

        // Now launch two concurrent join requests
        var concurrentUserA = Guid.NewGuid().ToString();
        var concurrentUserB = Guid.NewGuid().ToString();

        async Task<HttpResponseMessage> SendJoinAsync(string userId)
        {
            var joinRequest = new HttpRequestMessage(HttpMethod.Post, "/api/teams/join-by-code")
            {
                Content = JsonContent.Create(new { codigoAcceso = accessCode })
            };
            joinRequest.Headers.Add("X-Test-Role", "Participante");
            joinRequest.Headers.Add("X-Test-UserId", userId);
            return await _client.SendAsync(joinRequest);
        }

        var responses = await Task.WhenAll(SendJoinAsync(concurrentUserA), SendJoinAsync(concurrentUserB));
        var statusCodes = responses.Select(x => x.StatusCode).ToArray();
        
        // One should succeed, one should fail
        Assert.Contains(HttpStatusCode.OK, statusCodes);
        Assert.Contains(HttpStatusCode.Conflict, statusCodes);

        // Verify the successful response has 5 members
        var winnerResponse = responses.Single(x => x.StatusCode == HttpStatusCode.OK);
        var winnerPayload = await winnerResponse.Content.ReadAsStringAsync();
        using var winnerDoc = JsonDocument.Parse(winnerPayload);
        var integrantes = winnerDoc.RootElement.GetProperty("integrantes");
        Assert.Equal(5, integrantes.GetArrayLength());

        // Verify the failed response has the correct error message
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