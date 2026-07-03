using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Umbral.IdentityService.ContractTests.Teams;

/// <summary>Contract tests for GET /api/teams/mine (read del equipo activo del caller).</summary>
public sealed class MiEquipoContractTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    public MiEquipoContractTests(IdentityApiFactory factory) => _factory = factory;

    [Fact]
    public async Task MiEquipo_Returns200_WithShape_ForLeader()
    {
        var leaderId = Guid.NewGuid();
        await CreateTeamAsync(leaderId, "Mine Test Team");

        var client = _factory.CreateClientAs("Participante", leaderId);
        var response = await client.GetAsync("/api/teams/mine");
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(doc.RootElement.TryGetProperty("equipoId", out _));
        Assert.True(doc.RootElement.TryGetProperty("nombreEquipo", out _));
        Assert.True(doc.RootElement.TryGetProperty("estado", out _));
        Assert.True(doc.RootElement.TryGetProperty("participantes", out var participantes));
        Assert.Equal(JsonValueKind.Array, participantes.ValueKind);
        var first = participantes[0];
        Assert.True(first.TryGetProperty("usuarioId", out _));
        Assert.True(first.TryGetProperty("esLider", out _));
    }

    [Fact]
    public async Task MiEquipo_Returns404_WhenNoActiveTeam()
    {
        var userId = Guid.NewGuid();
        var client = _factory.CreateClientAs("Participante", userId);

        var response = await client.GetAsync("/api/teams/mine");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MiEquipo_MemberSeesLeaderFlagFalse_ForThemselves()
    {
        var leaderId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        await CreateTeamAsync(leaderId, "Mine Member Flag Team");
        var invId = await InviteParticipantAsync(leaderId, memberId);
        await AcceptInvitationAsync(memberId, invId);

        var client = _factory.CreateClientAs("Participante", memberId);
        var response = await client.GetAsync("/api/teams/mine");
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var me = doc.RootElement.GetProperty("participantes").EnumerateArray()
            .Single(p => Guid.Parse(p.GetProperty("usuarioId").GetString()!) == memberId);
        Assert.False(me.GetProperty("esLider").GetBoolean());
    }

    // ── Helpers (idénticos a InvitationsContractTests) ──
    private async Task<Guid> CreateTeamAsync(Guid leaderId, string name)
    {
        var client = _factory.CreateClientAs("Participante", leaderId);
        var response = await client.PostAsJsonAsync("/api/teams", new { nombreEquipo = name });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        return Guid.Parse(doc.RootElement.GetProperty("equipoId").GetString()!);
    }

    private async Task<Guid> InviteParticipantAsync(Guid leaderId, Guid invitadoId)
    {
        var leaderClient = _factory.CreateClientAs("Participante", leaderId);
        var response = await leaderClient.PostAsJsonAsync("/api/teams/invitations", new { invitadoUserId = invitadoId });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        return Guid.Parse(doc.RootElement.GetProperty("invitacionEquipoId").GetString()!);
    }

    private async Task AcceptInvitationAsync(Guid invitadoId, Guid invitacionId)
    {
        var client = _factory.CreateClientAs("Participante", invitadoId);
        var response = await client.PostAsJsonAsync($"/api/teams/invitations/{invitacionId}/acceptance", new { });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
