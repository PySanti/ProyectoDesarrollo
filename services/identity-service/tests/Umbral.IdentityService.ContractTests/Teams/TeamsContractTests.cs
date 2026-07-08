using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Umbral.IdentityService.ContractTests.Teams;

/// <summary>
/// Contract tests for team CRUD endpoints: create, leave, transfer leadership.
/// Covers HU03, HU06, HU07 surface.
/// </summary>
public sealed class TeamsContractTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    public TeamsContractTests(IdentityApiFactory factory)
    {
        _factory = factory;
    }

    // ── Create team ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateTeam_Returns201_WithCorrectShape_AndNoCodigoAcceso()
    {
        var leaderId = Guid.NewGuid();
        var client = _factory.CreateClientAs("Participante", leaderId);

        var response = await client.PostAsJsonAsync("/identity/teams", new { nombreEquipo = "Equipo Alpha" });
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.True(doc.RootElement.TryGetProperty("equipoId", out _));
        Assert.True(doc.RootElement.TryGetProperty("nombreEquipo", out _));
        Assert.True(doc.RootElement.TryGetProperty("estado", out _));
        Assert.True(doc.RootElement.TryGetProperty("liderUserId", out _));
        Assert.True(doc.RootElement.TryGetProperty("integrantes", out _));

        // Assert no codigoAcceso in response (teams use invitation flow only)
        Assert.False(doc.RootElement.TryGetProperty("codigoAcceso", out _),
            "Response must NOT contain 'codigoAcceso' — join is via invitation only.");
    }

    [Fact]
    public async Task CreateTeam_Returns401_WithNoAuth()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/identity/teams", new { nombreEquipo = "No Auth Team" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateTeam_Returns403_ForAdministrador()
    {
        var client = _factory.CreateClientAs("Administrador", Guid.NewGuid());
        var response = await client.PostAsJsonAsync("/identity/teams", new { nombreEquipo = "Admin Team" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Teams_con_rol_sin_GestionarEquipos_es_403()
    {
        // Operador autenticado pero sin el permiso GestionarEquipos.
        var client = _factory.CreateClientAs("Operador", Guid.NewGuid());

        var response = await client.GetAsync("identity/teams/mine");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateTeam_Returns409_WhenParticipantAlreadyHasTeam()
    {
        var leaderId = Guid.NewGuid();
        var client = _factory.CreateClientAs("Participante", leaderId);

        // First team creation should succeed
        var first = await client.PostAsJsonAsync("/identity/teams", new { nombreEquipo = "First Team" });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        // Second creation by same user should conflict
        var second = await client.PostAsJsonAsync("/identity/teams", new { nombreEquipo = "Second Team" });
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    // ── Leave team ───────────────────────────────────────────────────────────

    [Fact]
    public async Task LeaveTeam_Returns200_WhenMemberLeaves()
    {
        // Create leader and a member who joins via invitation
        var leaderId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        var teamId = await CreateTeamAsync(leaderId, "Leave Test Team");
        var invitacionId = await InviteParticipantAsync(leaderId, memberId);
        await AcceptInvitationAsync(memberId, invitacionId);

        // Member leaves
        var memberClient = _factory.CreateClientAs("Participante", memberId);
        var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, "/identity/teams/membership");
        deleteRequest.Headers.Add("X-Test-Role", "Participante");
        deleteRequest.Headers.Add("X-Test-UserId", memberId.ToString());
        var response = await memberClient.SendAsync(deleteRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.TryGetProperty("userId", out _));
        Assert.True(doc.RootElement.TryGetProperty("equipoId", out _));
        _ = teamId; // used for setup
    }

    [Fact]
    public async Task LeaveTeam_Returns404_WhenNotInATeam()
    {
        var userId = Guid.NewGuid();
        var client = _factory.CreateClientAs("Participante", userId);

        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/identity/teams/membership"));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── Transfer leadership ──────────────────────────────────────────────────

    [Fact]
    public async Task TransferLeadership_Returns200_WithCorrectShape()
    {
        var leaderId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        await CreateTeamAsync(leaderId, "Transfer Test Team");
        var invitacionId = await InviteParticipantAsync(leaderId, memberId);
        await AcceptInvitationAsync(memberId, invitacionId);

        var leaderClient = _factory.CreateClientAs("Participante", leaderId);
        var response = await leaderClient.PatchAsJsonAsync("/identity/teams/leadership",
            new { nuevoLiderUserId = memberId });

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(doc.RootElement.TryGetProperty("equipoId", out _));
        Assert.True(doc.RootElement.TryGetProperty("liderAnteriorUserId", out _));
        Assert.True(doc.RootElement.TryGetProperty("nuevoLiderUserId", out _));
        Assert.True(doc.RootElement.TryGetProperty("equipoEstado", out _));
    }

    [Fact]
    public async Task TransferLeadership_Returns409_ForNonLeader()
    {
        var leaderId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        await CreateTeamAsync(leaderId, "Transfer 409 Non-leader Team");
        var invitacionId = await InviteParticipantAsync(leaderId, memberId);
        await AcceptInvitationAsync(memberId, invitacionId);

        // Member (non-leader) tries to transfer leadership —
        // the handler maps ActorNoEsLiderEquipoException → TransferirLiderazgoConflictException → 409.
        var memberClient = _factory.CreateClientAs("Participante", memberId);
        var response = await memberClient.PatchAsJsonAsync("/identity/teams/leadership",
            new { nuevoLiderUserId = leaderId });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<Guid> CreateTeamAsync(Guid leaderId, string name)
    {
        var client = _factory.CreateClientAs("Participante", leaderId);
        var response = await client.PostAsJsonAsync("/identity/teams", new { nombreEquipo = name });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.TryGetProperty("equipoId", out var idEl));
        return Guid.Parse(idEl.GetString()!);
    }

    private async Task<Guid> InviteParticipantAsync(Guid leaderId, Guid invitadoId)
    {
        var leaderClient = _factory.CreateClientAs("Participante", leaderId);
        var response = await leaderClient.PostAsJsonAsync("/identity/teams/invitations",
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
            $"/identity/teams/invitations/{invitacionId}/acceptance", new { });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
