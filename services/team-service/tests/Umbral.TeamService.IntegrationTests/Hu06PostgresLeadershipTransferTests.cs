using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Umbral.TeamService.Infrastructure.Persistence;

namespace Umbral.TeamService.IntegrationTests;

public sealed class Hu06PostgresLeadershipTransferTests : IClassFixture<PostgresTeamApiFactory>
{
    private readonly PostgresTeamApiFactory _factory;
    private readonly HttpClient _client;

    public Hu06PostgresLeadershipTransferTests(PostgresTeamApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PatchTeamsLeadership_Should_Persist_New_Leader_With_Npgsql()
    {
        await _factory.ResetDatabaseAsync();
        var leaderUserId = Guid.NewGuid();
        var newLeaderUserId = Guid.NewGuid();
        var accessCode = await CreateTeamAndGetAccessCodeAsync(leaderUserId);
        await JoinTeamAsync(accessCode, newLeaderUserId);

        var transferResponse = await _client.SendAsync(CreateTransferRequest(leaderUserId, newLeaderUserId));

        Assert.Equal(HttpStatusCode.OK, transferResponse.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TeamDbContext>();
        var persistedMembers = await dbContext.ParticipantesEquipo
            .AsNoTracking()
            .OrderBy(member => member.UsuarioId)
            .ToListAsync();
        Assert.Equal(2, persistedMembers.Count);
        Assert.Single(persistedMembers.Where(member => member.EsLider));
        Assert.Contains(persistedMembers, member => member.UsuarioId == leaderUserId && !member.EsLider);
        Assert.Contains(persistedMembers, member => member.UsuarioId == newLeaderUserId && member.EsLider);
    }

    private async Task<string> CreateTeamAndGetAccessCodeAsync(Guid creatorUserId)
    {
        var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/teams")
        {
            Content = JsonContent.Create(new { nombreEquipo = $"Equipo-{Guid.NewGuid():N}" })
        };
        createRequest.Headers.Add("X-Test-Role", "Participante");
        createRequest.Headers.Add("X-Test-UserId", creatorUserId.ToString());

        var createResponse = await _client.SendAsync(createRequest);
        var createBody = await createResponse.Content.ReadAsStringAsync();
        Assert.True(createResponse.StatusCode == HttpStatusCode.Created, $"Unexpected status: {createResponse.StatusCode}. Body: {createBody}");

        using var document = JsonDocument.Parse(createBody);
        return document.RootElement.GetProperty("codigoAcceso").GetString()!;
    }

    private async Task JoinTeamAsync(string accessCode, Guid userId)
    {
        var joinRequest = new HttpRequestMessage(HttpMethod.Post, "/api/teams/join-by-code")
        {
            Content = JsonContent.Create(new { codigoAcceso = accessCode })
        };
        joinRequest.Headers.Add("X-Test-Role", "Participante");
        joinRequest.Headers.Add("X-Test-UserId", userId.ToString());

        var joinResponse = await _client.SendAsync(joinRequest);
        var joinBody = await joinResponse.Content.ReadAsStringAsync();
        Assert.True(joinResponse.StatusCode == HttpStatusCode.OK, $"Unexpected status: {joinResponse.StatusCode}. Body: {joinBody}");
    }

    private static HttpRequestMessage CreateTransferRequest(Guid actorUserId, Guid newLeaderUserId)
    {
        var transferRequest = new HttpRequestMessage(HttpMethod.Patch, "/api/teams/leadership")
        {
            Content = JsonContent.Create(new { nuevoLiderUserId = newLeaderUserId })
        };
        transferRequest.Headers.Add("X-Test-Role", "Participante");
        transferRequest.Headers.Add("X-Test-UserId", actorUserId.ToString());
        return transferRequest;
    }
}
