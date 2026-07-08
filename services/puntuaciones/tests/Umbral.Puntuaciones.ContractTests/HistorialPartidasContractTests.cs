using System.Net;
using System.Text.Json;

namespace Umbral.Puntuaciones.ContractTests;

public class HistorialPartidasContractTests : IClassFixture<PuntuacionesWebFactory>
{
    private readonly PuntuacionesWebFactory _factory;

    public HistorialPartidasContractTests(PuntuacionesWebFactory factory) => _factory = factory;

    [Fact]
    public async Task Sin_token_devuelve_401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/puntuaciones/participantes/{Guid.NewGuid()}/historial-partidas");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Autenticado_sin_partidas_devuelve_200_con_shape_del_contrato()
    {
        var client = _factory.CreateClientAutenticado();
        var participanteId = Guid.NewGuid();

        var response = await client.GetAsync($"/puntuaciones/participantes/{participanteId}/historial-partidas");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(participanteId, json.RootElement.GetProperty("participanteId").GetGuid());
        Assert.Equal(0, json.RootElement.GetProperty("partidas").GetArrayLength());
    }
}
