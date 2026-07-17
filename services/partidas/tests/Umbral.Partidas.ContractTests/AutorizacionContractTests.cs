using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Umbral.Partidas.ContractTests;

/// <summary>
/// SP-5a / Task-5: toda mutación y toda lectura de configuración exigen GestionarPartidas.
/// GET ya no queda abierto a cualquier autenticado: el único caller interno (Operaciones→Publicar,
/// SP-3a §12) reenvía el bearer de quien ya tiene GestionarPartidas, así que cerrar el GET no rompe
/// ese flujo — ver el comentario sobre <c>GetPartida</c> en <c>PartidasController</c>.
/// </summary>
public class AutorizacionContractTests : IClassFixture<PartidasWebFactory>
{
    private readonly PartidasWebFactory _factory;

    public AutorizacionContractTests(PartidasWebFactory factory) => _factory = factory;

    [Fact]
    public async Task Sin_token_es_401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/partidas/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("POST", "/partidas")]
    [InlineData("POST", "/partidas/{id}/juegos/trivia")]
    [InlineData("POST", "/partidas/{id}/juegos/bdt")]
    [InlineData("GET", "/partidas/{id}")]
    [InlineData("GET", "/partidas")]
    public async Task Endpoint_sin_GestionarPartidas_es_403(string method, string template)
    {
        var client = _factory.CreateClientAs(Guid.NewGuid(), "ParticiparEnPartidas");
        var url = template.Replace("{id}", Guid.NewGuid().ToString());
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (method == "POST")
        {
            request.Content = JsonContent.Create(new { });
        }

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Get_partida_con_GestionarPartidas_pasa()
    {
        var client = _factory.CreateClientAs(Guid.NewGuid(), "GestionarPartidas");

        var response = await client.GetAsync($"/partidas/{Guid.NewGuid()}");

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Health_es_anonimo()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
