using System.Net;
using System.Text.Json;

namespace Umbral.Puntuaciones.ContractTests;

public class HistorialContractTests : IClassFixture<PuntuacionesWebFactory>
{
    private readonly PuntuacionesWebFactory _factory;

    public HistorialContractTests(PuntuacionesWebFactory factory) => _factory = factory;

    [Fact]
    public async Task Sin_token_devuelve_401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/puntuaciones/partidas/{Guid.NewGuid()}/historial");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Con_rol_Participante_devuelve_403()
    {
        var client = _factory.CreateClientConRoles("Participante");

        var response = await client.GetAsync($"/puntuaciones/partidas/{Guid.NewGuid()}/historial");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Autenticado_sin_roles_devuelve_403()
    {
        var client = _factory.CreateClientAutenticado();

        var response = await client.GetAsync($"/puntuaciones/partidas/{Guid.NewGuid()}/historial");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Con_rol_Operador_y_partida_desconocida_devuelve_404_con_message()
    {
        var client = _factory.CreateClientConRoles("Operador");

        var response = await client.GetAsync($"/puntuaciones/partidas/{Guid.NewGuid()}/historial");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.True(json.RootElement.TryGetProperty("message", out _));
    }

    [Fact]
    public async Task Limit_fuera_de_rango_devuelve_400()
    {
        var client = _factory.CreateClientConRoles("Administrador");

        var response = await client.GetAsync($"/puntuaciones/partidas/{Guid.NewGuid()}/historial?limit=501");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
