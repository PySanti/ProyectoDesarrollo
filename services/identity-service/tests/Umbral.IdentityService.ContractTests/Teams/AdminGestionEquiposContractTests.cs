using System.Net;

namespace Umbral.IdentityService.ContractTests.Teams;

/// <summary>
/// Task 5: los paneles de administrar equipos ajenos exigen rol AND privilegio, no solo el
/// privilegio — los puertos de servicio están expuestos y una policy de solo-privilegio dejaría
/// escalar a cualquier rol al que el panel le dé GestionarEquipos (p.ej. un Participante llamando
/// directo al puerto 5001). Tres casos por policy compuesta, o el AND no queda probado:
/// rol sin privilegio → 403, privilegio sin el rol correcto → 403 (éste prueba el AND),
/// rol con privilegio → 200.
/// </summary>
public sealed class AdminGestionEquiposContractTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    public AdminGestionEquiposContractTests(IdentityApiFactory factory) => _factory = factory;

    // ── AdminTeamsController (/identity/admin/teams) — policy AdminGestionarEquipos ──────────

    [Fact]
    public async Task AdminTeams_rol_Administrador_sin_privilegio_es_403()
    {
        var client = _factory.CreateClientWithRoles(Guid.NewGuid(), "Administrador");

        var response = await client.GetAsync("/identity/admin/teams");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminTeams_privilegio_sin_rol_Administrador_es_403()
    {
        // Operador con GestionarEquipos: tiene el privilegio, pero no el rol que exige la policy.
        var client = _factory.CreateClientWithRoles(Guid.NewGuid(), "Operador", "GestionarEquipos");

        var response = await client.GetAsync("/identity/admin/teams");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminTeams_rol_Administrador_con_privilegio_pasa()
    {
        var client = _factory.CreateClientWithRoles(Guid.NewGuid(), "Administrador", "GestionarEquipos");

        var response = await client.GetAsync("/identity/admin/teams");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── TeamsAdminController (/identity/teams) — policy OperadorOAdminGestionarEquipos ───────

    [Fact]
    public async Task TeamsAdmin_rol_Operador_sin_privilegio_es_403()
    {
        var client = _factory.CreateClientWithRoles(Guid.NewGuid(), "Operador");

        var response = await client.GetAsync("/identity/teams");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task TeamsAdmin_privilegio_sin_rol_Operador_ni_Administrador_es_403()
    {
        // Participante con GestionarEquipos: tiene el privilegio, pero ningún rol de los que exige la policy.
        var client = _factory.CreateClientWithRoles(Guid.NewGuid(), "Participante", "GestionarEquipos");

        var response = await client.GetAsync("/identity/teams");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task TeamsAdmin_rol_Operador_con_privilegio_pasa()
    {
        var client = _factory.CreateClientWithRoles(Guid.NewGuid(), "Operador", "GestionarEquipos");

        var response = await client.GetAsync("/identity/teams");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TeamsAdmin_rol_Administrador_con_privilegio_pasa()
    {
        // Con los defaults nuevos el Administrador ya trae GestionarEquipos vía CreateClientAs.
        var client = _factory.CreateClientAs("Administrador", Guid.NewGuid());

        var response = await client.GetAsync("/identity/teams");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
