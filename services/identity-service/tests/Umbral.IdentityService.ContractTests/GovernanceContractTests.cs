using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Umbral.IdentityService.ContractTests;

/// <summary>
/// Contract tests for the governance panel: GET permisos matrix + PUT permisos por rol (SP-5b G4).
///
/// El arnés de ContractTests usa EF InMemory (ver <see cref="IdentityApiFactory"/>), y el seed BR-R03
/// de Program.cs solo corre cuando <c>dbContext.Database.IsRelational()</c> es true — con InMemory es
/// false, así que el seed NO se aplica aquí. Por eso cada test fija el estado que necesita con un PUT
/// antes de leerlo, en vez de asumir los defaults de BR-R03 (ver task-4-report.md).
/// </summary>
public sealed class GovernanceContractTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    public GovernanceContractTests(IdentityApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetRoles_Returns200_WithThreeRoles_AndPrivilegiosGobernanzaShape()
    {
        var admin = _factory.CreateClientAs("Administrador", Guid.NewGuid());

        // Fija el estado de Operador que este test necesita leer (sin depender del seed BR-R03).
        var seed = await admin.PutAsJsonAsync("/identity/governance/roles/Operador/permisos",
            new { permisos = new[] { "GestionarPartidas" } });
        Assert.Equal(HttpStatusCode.OK, seed.StatusCode);

        var response = await admin.GetAsync("/identity/governance/roles");
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var roles = doc.RootElement.GetProperty("roles");
        Assert.Equal(3, roles.GetArrayLength());

        var administrador = roles.EnumerateArray().Single(r => r.GetProperty("rol").GetString() == "Administrador");
        Assert.True(administrador.GetProperty("privilegiosGobernanza").GetBoolean());

        var operador = roles.EnumerateArray().Single(r => r.GetProperty("rol").GetString() == "Operador");
        var operadorPermisos = operador.GetProperty("permisos").EnumerateArray().Select(p => p.GetString()).ToList();
        Assert.Contains("GestionarPartidas", operadorPermisos);
    }

    [Fact]
    public async Task Put_Valido_Actualiza_Permisos_Y_Get_Posterior_Refleja_El_Cambio()
    {
        var admin = _factory.CreateClientAs("Administrador", Guid.NewGuid());

        var put = await admin.PutAsJsonAsync("/identity/governance/roles/Operador/permisos",
            new { permisos = new[] { "GestionarEquipos" } });
        var putBody = await put.Content.ReadAsStringAsync();
        using var putDoc = JsonDocument.Parse(putBody);

        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        var putPermisos = putDoc.RootElement.GetProperty("permisos").EnumerateArray().Select(p => p.GetString()).ToList();
        Assert.Equal(new[] { "GestionarEquipos" }, putPermisos);

        var get = await admin.GetAsync("/identity/governance/roles");
        var getBody = await get.Content.ReadAsStringAsync();
        using var getDoc = JsonDocument.Parse(getBody);
        var operador = getDoc.RootElement.GetProperty("roles").EnumerateArray()
            .Single(r => r.GetProperty("rol").GetString() == "Operador");
        var getPermisos = operador.GetProperty("permisos").EnumerateArray().Select(p => p.GetString()).ToList();
        Assert.Equal(new[] { "GestionarEquipos" }, getPermisos);
    }

    [Fact]
    public async Task Put_ConPermisoInvalido_Returns400()
    {
        var admin = _factory.CreateClientAs("Administrador", Guid.NewGuid());

        var response = await admin.PutAsJsonAsync("/identity/governance/roles/Participante/permisos",
            new { permisos = new[] { "NoExiste" } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_ConRolInvalido_Returns400()
    {
        var admin = _factory.CreateClientAs("Administrador", Guid.NewGuid());

        var response = await admin.PutAsJsonAsync("/identity/governance/roles/SuperUser/permisos",
            new { permisos = new[] { "GestionarEquipos" } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetRoles_ConRolParticipante_Returns403()
    {
        var client = _factory.CreateClientAs("Participante", Guid.NewGuid());

        var response = await client.GetAsync("/identity/governance/roles");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetRoles_SinIdentidad_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/identity/governance/roles");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
