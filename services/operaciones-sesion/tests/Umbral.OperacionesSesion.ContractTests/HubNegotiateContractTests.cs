using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Umbral.OperacionesSesion.ContractTests;

public class HubNegotiateContractTests : IClassFixture<OperacionesSesionWebFactory>
{
    private readonly OperacionesSesionWebFactory _factory;
    public HubNegotiateContractTests(OperacionesSesionWebFactory factory) => _factory = factory;

    [Fact]
    public async Task Negotiate_del_hub_responde_bajo_el_prefijo_operaciones_sesion()
    {
        var client = _factory.CreateClientAs(Guid.NewGuid()); // autenticado (hub lleva [Authorize])

        var response = await client.PostAsync($"{Rutas.Base}/hubs/sesion/negotiate?negotiateVersion=1",
            new StringContent(string.Empty));

        // El hub mapeado bajo el prefijo → negotiate 200 con connectionId.
        // Sin el prefijo (hub en /hubs/sesion) esta ruta daría 404.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("connectionId", body);
    }
}
