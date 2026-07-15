using System.Net;

namespace Umbral.Puntuaciones.ContractTests;

public class AutorizacionContractTests : IClassFixture<PuntuacionesWebFactory>
{
    private readonly PuntuacionesWebFactory _factory;

    public AutorizacionContractTests(PuntuacionesWebFactory factory) => _factory = factory;

    [Theory]
    [InlineData("/puntuaciones/partidas/11111111-1111-1111-1111-111111111111/juegos/22222222-2222-2222-2222-222222222222/ranking")]
    [InlineData("/puntuaciones/partidas/11111111-1111-1111-1111-111111111111/juegos/22222222-2222-2222-2222-222222222222/marcadores/33333333-3333-3333-3333-333333333333")]
    [InlineData("/puntuaciones/partidas/11111111-1111-1111-1111-111111111111/ranking-consolidado")]
    [InlineData("/puntuaciones/equipos/11111111-1111-1111-1111-111111111111/rendimiento")]
    public async Task Endpoints_de_lectura_sin_token_devuelven_401(string ruta)
    {
        var client = _factory.CreateClient(); // sin X-Test-Sub

        var response = await client.GetAsync(ruta);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Negotiate_del_hub_sin_token_devuelve_401_y_con_token_200()
    {
        var anonimo = _factory.CreateClient();
        var autenticado = _factory.CreateClientAutenticado();

        var sinToken = await anonimo.PostAsync("/puntuaciones/hubs/ranking/negotiate?negotiateVersion=1", null);
        var conToken = await autenticado.PostAsync("/puntuaciones/hubs/ranking/negotiate?negotiateVersion=1", null);

        Assert.Equal(HttpStatusCode.Unauthorized, sinToken.StatusCode);
        Assert.Equal(HttpStatusCode.OK, conToken.StatusCode);
    }

    [Fact]
    public async Task Health_sigue_siendo_anonimo()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // Task 5: EquiposController y HistorialController pasan a exigir rol AND privilegio (no solo el
    // privilegio) — los puertos de servicio están expuestos y una policy de sólo-privilegio dejaría
    // escalar a cualquier rol al que el panel le dé GestionarEquipos/GestionarPartidas. Tres casos
    // por policy compuesta, o el AND no queda probado: rol sin privilegio → 403, privilegio sin el
    // rol correcto → 403 (éste prueba el AND), rol con privilegio → ni 401 ni 403.

    [Fact]
    public async Task Equipos_rendimiento_rol_sin_privilegio_es_403()
    {
        var client = _factory.CreateClientConRoles("Operador");

        var response = await client.GetAsync($"/puntuaciones/equipos/{Guid.NewGuid()}/rendimiento");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Equipos_rendimiento_privilegio_sin_rol_correcto_es_403()
    {
        // Participante con GestionarEquipos: tiene el privilegio, pero ningún rol de los que exige la policy.
        var client = _factory.CreateClientConRoles("Participante", "GestionarEquipos");

        var response = await client.GetAsync($"/puntuaciones/equipos/{Guid.NewGuid()}/rendimiento");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Equipos_rendimiento_rol_con_privilegio_no_es_401_ni_403()
    {
        var client = _factory.CreateClientConRoles("Operador", "GestionarEquipos");

        var response = await client.GetAsync($"/puntuaciones/equipos/{Guid.NewGuid()}/rendimiento");

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Historial_rol_sin_privilegio_es_403()
    {
        var client = _factory.CreateClientConRoles("Operador");

        var response = await client.GetAsync($"/puntuaciones/partidas/{Guid.NewGuid()}/historial");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Historial_privilegio_sin_rol_correcto_es_403()
    {
        // Participante con GestionarPartidas: tiene el privilegio, pero ningún rol de los que exige la policy.
        var client = _factory.CreateClientConRoles("Participante", "GestionarPartidas");

        var response = await client.GetAsync($"/puntuaciones/partidas/{Guid.NewGuid()}/historial");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Historial_rol_con_privilegio_no_es_401_ni_403()
    {
        var client = _factory.CreateClientConRoles("Operador", "GestionarPartidas");

        var response = await client.GetAsync($"/puntuaciones/partidas/{Guid.NewGuid()}/historial");

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
