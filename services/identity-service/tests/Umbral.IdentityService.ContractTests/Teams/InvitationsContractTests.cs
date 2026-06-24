using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Umbral.IdentityService.ContractTests.Teams;

/// <summary>
/// Contract tests for the invitation flow endpoints:
/// send, inbox (GET), accept, reject, eligible participants list.
/// </summary>
public sealed class InvitationsContractTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    public InvitationsContractTests(IdentityApiFactory factory)
    {
        _factory = factory;
    }

    // ── Send invitation ──────────────────────────────────────────────────────

    [Fact]
    public async Task SendInvitation_Returns201_WithCorrectShape()
    {
        var leaderId = Guid.NewGuid();
        var invitadoId = Guid.NewGuid();

        await CreateTeamAsync(leaderId, "Invite Test Team 201");

        var leaderClient = _factory.CreateClientAs("Participante", leaderId);
        var response = await leaderClient.PostAsJsonAsync("/api/teams/invitations",
            new { invitadoUserId = invitadoId });
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.True(doc.RootElement.TryGetProperty("invitacionEquipoId", out _));
        Assert.True(doc.RootElement.TryGetProperty("equipoId", out _));
        Assert.True(doc.RootElement.TryGetProperty("invitadoUserId", out _));
        Assert.True(doc.RootElement.TryGetProperty("invitadoPorUserId", out _));
        Assert.True(doc.RootElement.TryGetProperty("estado", out _));
        Assert.True(doc.RootElement.TryGetProperty("fechaCreacionUtc", out _));
    }

    [Fact]
    public async Task SendInvitation_Returns403_ForNonLeader()
    {
        var leaderId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var thirdUserId = Guid.NewGuid();

        await CreateTeamAsync(leaderId, "Non-leader Invite Team");
        var invitacionId = await InviteParticipantAsync(leaderId, memberId);
        await AcceptInvitationAsync(memberId, invitacionId);

        // Member (non-leader) tries to invite
        var memberClient = _factory.CreateClientAs("Participante", memberId);
        var response = await memberClient.PostAsJsonAsync("/api/teams/invitations",
            new { invitadoUserId = thirdUserId });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SendInvitation_Returns409_WhenDuplicatePendingInvitation()
    {
        var leaderId = Guid.NewGuid();
        var invitadoId = Guid.NewGuid();

        await CreateTeamAsync(leaderId, "Duplicate Invite Test Team");

        // First invitation — should succeed
        await InviteParticipantAsync(leaderId, invitadoId);

        // Second invitation to same participant — should return 409
        var leaderClient = _factory.CreateClientAs("Participante", leaderId);
        var response = await leaderClient.PostAsJsonAsync("/api/teams/invitations",
            new { invitadoUserId = invitadoId });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task SendInvitation_Returns409_WhenTeamIsFull()
    {
        var leaderId = Guid.NewGuid();
        await CreateTeamAsync(leaderId, "Full Team Invite Test");

        // Add 4 more members to reach team maximum (5 total)
        for (var i = 0; i < 4; i++)
        {
            var memberId = Guid.NewGuid();
            var invId = await InviteParticipantAsync(leaderId, memberId);
            await AcceptInvitationAsync(memberId, invId);
        }

        // Now team is full — invite a 6th person should return 409
        var extraUserId = Guid.NewGuid();
        var leaderClient = _factory.CreateClientAs("Participante", leaderId);
        var response = await leaderClient.PostAsJsonAsync("/api/teams/invitations",
            new { invitadoUserId = extraUserId });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // ── Inbox (GET /api/teams/invitations) ───────────────────────────────────

    [Fact]
    public async Task GetInvitations_Returns200_WithList()
    {
        var leaderId = Guid.NewGuid();
        var invitadoId = Guid.NewGuid();

        await CreateTeamAsync(leaderId, "Inbox Test Team");
        await InviteParticipantAsync(leaderId, invitadoId);

        var invitadoClient = _factory.CreateClientAs("Participante", invitadoId);
        var response = await invitadoClient.GetAsync("/api/teams/invitations");
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.True(doc.RootElement.GetArrayLength() >= 1);

        var first = doc.RootElement[0];
        Assert.True(first.TryGetProperty("invitacionId", out _));
        Assert.True(first.TryGetProperty("equipoId", out _));
        Assert.True(first.TryGetProperty("nombreEquipo", out _));
        Assert.True(first.TryGetProperty("invitadoPorUserId", out _));
        Assert.True(first.TryGetProperty("fechaCreacionUtc", out _));
    }

    [Fact]
    public async Task GetInvitations_Returns200_EmptyList_WhenNoPendingInvitations()
    {
        var userId = Guid.NewGuid();
        var client = _factory.CreateClientAs("Participante", userId);

        var response = await client.GetAsync("/api/teams/invitations");
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }

    // ── Accept invitation ────────────────────────────────────────────────────

    [Fact]
    public async Task AcceptInvitation_Returns200_WithCorrectShape()
    {
        var leaderId = Guid.NewGuid();
        var invitadoId = Guid.NewGuid();

        await CreateTeamAsync(leaderId, "Accept Test Team");
        var invitacionId = await InviteParticipantAsync(leaderId, invitadoId);

        var invitadoClient = _factory.CreateClientAs("Participante", invitadoId);
        var response = await invitadoClient.PostAsJsonAsync(
            $"/api/teams/invitations/{invitacionId}/acceptance", new { });
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(doc.RootElement.TryGetProperty("invitacionEquipoId", out _));
        Assert.True(doc.RootElement.TryGetProperty("equipoId", out _));
        Assert.True(doc.RootElement.TryGetProperty("invitadoUserId", out _));
        Assert.True(doc.RootElement.TryGetProperty("estadoInvitacion", out _));
    }

    [Fact]
    public async Task AcceptInvitation_Returns404_ForUnknownInvitacion()
    {
        var userId = Guid.NewGuid();
        var client = _factory.CreateClientAs("Participante", userId);

        var fakeId = Guid.NewGuid();
        var response = await client.PostAsJsonAsync(
            $"/api/teams/invitations/{fakeId}/acceptance", new { });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── Reject invitation ────────────────────────────────────────────────────

    [Fact]
    public async Task RejectInvitation_Returns200_WithCorrectShape()
    {
        var leaderId = Guid.NewGuid();
        var invitadoId = Guid.NewGuid();

        await CreateTeamAsync(leaderId, "Reject Test Team");
        var invitacionId = await InviteParticipantAsync(leaderId, invitadoId);

        var invitadoClient = _factory.CreateClientAs("Participante", invitadoId);
        var response = await invitadoClient.PostAsJsonAsync(
            $"/api/teams/invitations/{invitacionId}/rejection", new { });
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(doc.RootElement.TryGetProperty("invitacionEquipoId", out _));
        Assert.True(doc.RootElement.TryGetProperty("equipoId", out _));
        Assert.True(doc.RootElement.TryGetProperty("invitadoUserId", out _));
        Assert.True(doc.RootElement.TryGetProperty("estadoInvitacion", out _));
    }

    [Fact]
    public async Task RejectInvitation_Returns404_ForUnknownInvitacion()
    {
        var userId = Guid.NewGuid();
        var client = _factory.CreateClientAs("Participante", userId);

        var fakeId = Guid.NewGuid();
        var response = await client.PostAsJsonAsync(
            $"/api/teams/invitations/{fakeId}/rejection", new { });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── Eligible participants ────────────────────────────────────────────────

    [Fact]
    public async Task GetEligibleParticipants_Returns200_WithList()
    {
        var leaderId = Guid.NewGuid();
        await CreateTeamAsync(leaderId, "Eligible Test Team");

        var leaderClient = _factory.CreateClientAs("Participante", leaderId);
        var response = await leaderClient.GetAsync("/api/teams/eligible-participants");
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }

    [Fact]
    public async Task GetEligibleParticipants_ExcludesParticipantsAlreadyInTeam()
    {
        var leaderId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        await CreateTeamAsync(leaderId, "Eligible Exclusion Team");
        var invId = await InviteParticipantAsync(leaderId, memberId);
        await AcceptInvitationAsync(memberId, invId);

        // Both leader and member have a team; neither should appear in eligible list
        var leaderClient = _factory.CreateClientAs("Participante", leaderId);
        var response = await leaderClient.GetAsync("/api/teams/eligible-participants");
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);

        // Verify neither leader nor member appears in the eligible list
        foreach (var el in doc.RootElement.EnumerateArray())
        {
            if (el.TryGetProperty("userId", out var userIdEl))
            {
                if (Guid.TryParse(userIdEl.GetString(), out var uid))
                {
                    Assert.NotEqual(leaderId, uid);
                    Assert.NotEqual(memberId, uid);
                }
            }
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<Guid> CreateTeamAsync(Guid leaderId, string name)
    {
        var client = _factory.CreateClientAs("Participante", leaderId);
        var response = await client.PostAsJsonAsync("/api/teams", new { nombreEquipo = name });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.TryGetProperty("equipoId", out var idEl));
        return Guid.Parse(idEl.GetString()!);
    }

    private async Task<Guid> InviteParticipantAsync(Guid leaderId, Guid invitadoId)
    {
        var leaderClient = _factory.CreateClientAs("Participante", leaderId);
        var response = await leaderClient.PostAsJsonAsync("/api/teams/invitations",
            new { invitadoUserId = invitadoId });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.TryGetProperty("invitacionEquipoId", out var idEl));
        return Guid.Parse(idEl.GetString()!);
    }

    private async Task AcceptInvitationAsync(Guid invitadoId, Guid invitacionId)
    {
        var client = _factory.CreateClientAs("Participante", invitadoId);
        var response = await client.PostAsJsonAsync(
            $"/api/teams/invitations/{invitacionId}/acceptance", new { });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
