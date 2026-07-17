using System;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Umbral.IdentityService.ContractTests;

/// <summary>
/// Contrato de POST /identity/directory/names (ver contracts/http/identity-api.md).
/// El arnés usa EF InMemory, así que no hay usuarios ni equipos sembrados: estos tests
/// cubren autorización y forma de la respuesta. La resolución real contra datos
/// persistidos vive en IntegrationTests/DirectoryEndpointIntegrationTests.
/// </summary>
public sealed class DirectoryContractTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    public DirectoryContractTests(IdentityApiFactory factory) => _factory = factory;

    private static object CuerpoVacio() => new { participanteIds = Array.Empty<Guid>(), equipoIds = Array.Empty<Guid>() };

    [Fact]
    public async Task Sin_token_devuelve_401()
    {
        var anonimo = _factory.CreateClient();

        var response = await anonimo.PostAsJsonAsync("/identity/directory/names", CuerpoVacio());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("Participante")]
    [InlineData("Operador")]
    [InlineData("Administrador")]
    public async Task Cualquier_rol_autenticado_puede_resolver_nombres(string rol)
    {
        // Participante es el caso crítico: el móvil pinta el ranking en vivo con este
        // endpoint. Si alguien lo endurece a AdminOnly, este test lo atrapa.
        var client = _factory.CreateClientAs(rol, Guid.NewGuid());

        var response = await client.PostAsJsonAsync("/identity/directory/names", CuerpoVacio());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Respuesta_tiene_participantes_y_equipos_como_arrays()
    {
        var client = _factory.CreateClientAs("Operador", Guid.NewGuid());

        var response = await client.PostAsJsonAsync("/identity/directory/names",
            new { participanteIds = new[] { Guid.NewGuid() }, equipoIds = new[] { Guid.NewGuid() } });
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // Ids desconocidos se omiten: arrays presentes y vacíos, nunca null.
        Assert.Equal(JsonValueKind.Array, doc.RootElement.GetProperty("participantes").ValueKind);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.GetProperty("equipos").ValueKind);
        Assert.Equal(0, doc.RootElement.GetProperty("participantes").GetArrayLength());
        Assert.Equal(0, doc.RootElement.GetProperty("equipos").GetArrayLength());
    }

    [Fact]
    public async Task Lote_sobre_el_tope_devuelve_400()
    {
        var client = _factory.CreateClientAs("Operador", Guid.NewGuid());
        var demasiados = new Guid[201];
        for (var i = 0; i < demasiados.Length; i++) demasiados[i] = Guid.NewGuid();

        var response = await client.PostAsJsonAsync("/identity/directory/names",
            new { participanteIds = demasiados, equipoIds = Array.Empty<Guid>() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
