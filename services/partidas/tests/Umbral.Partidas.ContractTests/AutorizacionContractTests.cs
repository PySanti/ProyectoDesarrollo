using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Umbral.Partidas.ContractTests;

/// <summary>
/// SP-5a: mutaciones de configuración exigen GestionarPartidas; GET /partidas/{id}
/// solo exige autenticación (Operaciones reenvía el token del participante — SP-3a §12).
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
    public async Task Mutacion_sin_GestionarPartidas_es_403(string method, string template)
    {
        var client = _factory.CreateClientAs(Guid.NewGuid(), "ParticiparEnPartidas");
        var url = template.Replace("{id}", Guid.NewGuid().ToString());

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url)
        {
            Content = JsonContent.Create(new { })
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Get_partida_con_token_de_participante_pasa()
    {
        // Pin de la llamada interna Operaciones→Partidas con el bearer del participante.
        var client = _factory.CreateClientAs(Guid.NewGuid(), "ParticiparEnPartidas");

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
