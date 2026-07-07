using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Umbral.IdentityService.ContractTests;

/// <summary>
/// Contract tests para el cambio de rol de usuario (SP-5b G5): PATCH /identity/users/{userId}/role.
///
/// El caso 409 por equipo activo NO se cubre aquí: armar un equipo real de punta a punta requiere el
/// flujo completo de teams (crear equipo + invitación + aceptación) contra el arnés de ContractTests,
/// lo que es costoso para un solo caso ya cubierto a nivel de handler
/// (<c>CambiarRolUsuarioHandlerTests.Participante_con_equipo_activo_...</c>). Queda documentado como
/// decisión aceptada por el brief de la tarea.
/// </summary>
public sealed class ChangeUserRoleContractTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    public ChangeUserRoleContractTests(IdentityApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Patch_UsuarioInexistente_Returns404()
    {
        var admin = _factory.CreateClientAs("Administrador", Guid.NewGuid());

        var response = await admin.PatchAsJsonAsync($"/identity/users/{Guid.NewGuid()}/role", new { rol = "Operador" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Patch_PromocionAAdministrador_Returns200_Y_SegundoPatch_Returns409_PorInmutabilidad()
    {
        var admin = _factory.CreateClientAs("Administrador", Guid.NewGuid());

        var creado = await admin.PostAsJsonAsync("/identity/users", new
        {
            name = "Bob",
            email = "bob.role-contract@umbral.dev",
            initialRole = "Operador"
        });
        Assert.Equal(HttpStatusCode.Created, creado.StatusCode);
        using var creadoDoc = JsonDocument.Parse(await creado.Content.ReadAsStringAsync());
        var userId = creadoDoc.RootElement.GetProperty("userId").GetGuid();

        var promocion = await admin.PatchAsJsonAsync($"/identity/users/{userId}/role", new { rol = "Administrador" });
        var promocionBody = await promocion.Content.ReadAsStringAsync();
        using var promocionDoc = JsonDocument.Parse(promocionBody);

        Assert.Equal(HttpStatusCode.OK, promocion.StatusCode);
        Assert.Equal("Administrador", promocionDoc.RootElement.GetProperty("rol").GetString());

        var segundoPatch = await admin.PatchAsJsonAsync($"/identity/users/{userId}/role", new { rol = "Operador" });

        Assert.Equal(HttpStatusCode.Conflict, segundoPatch.StatusCode);
    }

    [Fact]
    public async Task Patch_ConRolInvalido_Returns400()
    {
        var admin = _factory.CreateClientAs("Administrador", Guid.NewGuid());

        var creado = await admin.PostAsJsonAsync("/identity/users", new
        {
            name = "Carla",
            email = "carla.role-contract@umbral.dev",
            initialRole = "Operador"
        });
        using var creadoDoc = JsonDocument.Parse(await creado.Content.ReadAsStringAsync());
        var userId = creadoDoc.RootElement.GetProperty("userId").GetGuid();

        var response = await admin.PatchAsJsonAsync($"/identity/users/{userId}/role", new { rol = "SuperUser" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Patch_ConRolOperador_NoAdmin_Returns403()
    {
        var operador = _factory.CreateClientAs("Operador", Guid.NewGuid());

        var response = await operador.PatchAsJsonAsync($"/identity/users/{Guid.NewGuid()}/role", new { rol = "Operador" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Patch_SinIdentidad_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PatchAsJsonAsync($"/identity/users/{Guid.NewGuid()}/role", new { rol = "Operador" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
