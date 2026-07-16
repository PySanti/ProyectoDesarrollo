using System.Net;

namespace Umbral.IdentityService.ContractTests.Teams;

/// <summary>
/// Privilegio-sin-rol: AdminTeamsController y TeamsAdminController exigen solo GestionarEquipos.
/// El rol base ya no participa — ni delimita, ni veta. Dos casos por endpoint: sin el privilegio,
/// cualquier rol (incluido Administrador) es 403; con el privilegio, cualquier rol (incluido
/// Participante) pasa.
/// </summary>
public sealed class AdminGestionEquiposContractTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    public AdminGestionEquiposContractTests(IdentityApiFactory factory) => _factory = factory;

    // ── AdminTeamsController (/identity/admin/teams) — policy GestionarEquipos ──────────

    [Fact]
    public async Task AdminTeams_Administrador_sin_privilegio_es_403()
    {
        var client = _factory.CreateClientWithRoles(Guid.NewGuid(), "Administrador");

        var response = await client.GetAsync("/identity/admin/teams");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminTeams_Participante_con_privilegio_pasa()
    {
        // El caso que antes era 403 por el AND de rol: ahora el privilegio solo alcanza.
        var client = _factory.CreateClientWithRoles(Guid.NewGuid(), "Participante", "GestionarEquipos");

        var response = await client.GetAsync("/identity/admin/teams");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AdminTeams_Administrador_con_privilegio_pasa()
    {
        var client = _factory.CreateClientWithRoles(Guid.NewGuid(), "Administrador", "GestionarEquipos");

        var response = await client.GetAsync("/identity/admin/teams");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── TeamsAdminController (/identity/teams) — policy GestionarEquipos ───────

    [Fact]
    public async Task TeamsAdmin_Operador_sin_privilegio_es_403()
    {
        var client = _factory.CreateClientWithRoles(Guid.NewGuid(), "Operador");

        var response = await client.GetAsync("/identity/teams");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task TeamsAdmin_Participante_con_privilegio_pasa()
    {
        var client = _factory.CreateClientWithRoles(Guid.NewGuid(), "Participante", "GestionarEquipos");

        var response = await client.GetAsync("/identity/teams");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TeamsAdmin_Operador_con_privilegio_pasa()
    {
        var client = _factory.CreateClientWithRoles(Guid.NewGuid(), "Operador", "GestionarEquipos");

        var response = await client.GetAsync("/identity/teams");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TeamsAdmin_Administrador_con_privilegio_pasa()
    {
        // Con los defaults reales el Administrador ya trae GestionarEquipos vía CreateClientAs.
        var client = _factory.CreateClientAs("Administrador", Guid.NewGuid());

        var response = await client.GetAsync("/identity/teams");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
