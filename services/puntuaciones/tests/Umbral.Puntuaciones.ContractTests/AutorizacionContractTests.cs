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
}
