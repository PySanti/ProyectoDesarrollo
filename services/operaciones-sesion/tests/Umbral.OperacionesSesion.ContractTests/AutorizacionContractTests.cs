using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Umbral.OperacionesSesion.ContractTests;

/// <summary>
/// Matriz de permisos funcionales (SP-5a): endpoints de operador exigen GestionarPartidas,
/// endpoints de participante exigen ParticiparEnPartidas, GETs compartidos aceptan cualquiera.
/// </summary>
public class AutorizacionContractTests : IClassFixture<OperacionesSesionWebFactory>
{
    private readonly OperacionesSesionWebFactory _factory;

    public AutorizacionContractTests(OperacionesSesionWebFactory factory) => _factory = factory;

    [Theory]
    [InlineData("POST", "/partidas/{id}/publicacion")]
    [InlineData("POST", "/partidas/{id}/inicio")]
    [InlineData("POST", "/partidas/{id}/inicio-automatico")]
    [InlineData("POST", "/partidas/{id}/cancelacion")]
    [InlineData("POST", "/partidas/{id}/juego-actual/finalizacion")]
    [InlineData("POST", "/partidas/{id}/pregunta-actual/avance")]
    [InlineData("POST", "/partidas/{id}/etapa-actual/avance")]
    [InlineData("POST", "/partidas/{id}/pistas")]
    public async Task Endpoint_de_operador_sin_GestionarPartidas_es_403(string method, string template)
    {
        var client = _factory.CreateClientAs(Guid.NewGuid(), "ParticiparEnPartidas");
        var url = Rutas.Base + template.Replace("{id}", Guid.NewGuid().ToString());

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url)
        {
            Content = JsonContent.Create(new { })
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("POST", "/partidas/{id}/inscripciones")]
    [InlineData("DELETE", "/partidas/{id}/inscripciones/mia")]
    [InlineData("POST", "/partidas/{id}/inscripciones-equipo")]
    [InlineData("DELETE", "/partidas/{id}/inscripciones-equipo/mia")]
    [InlineData("POST", "/convocatorias/{id}/aceptacion")]
    [InlineData("POST", "/convocatorias/{id}/rechazo")]
    [InlineData("POST", "/partidas/{id}/pregunta-actual/respuesta")]
    [InlineData("POST", "/partidas/{id}/etapa-actual/tesoro")]
    [InlineData("GET", "/mi-sesion")]
    [InlineData("GET", "/mis-convocatorias")]
    public async Task Endpoint_de_participante_sin_ParticiparEnPartidas_es_403(string method, string template)
    {
        var client = _factory.CreateClientAs(Guid.NewGuid(), "GestionarPartidas");
        var url = Rutas.Base + template.Replace("{id}", Guid.NewGuid().ToString());

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url)
        {
            Content = method == "GET" ? null : JsonContent.Create(new { })
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("GestionarPartidas")]
    [InlineData("ParticiparEnPartidas")]
    public async Task GET_compartido_acepta_cualquier_permiso(string roles)
    {
        var client = _factory.CreateClientAs(Guid.NewGuid(), roles);

        var lobby = await client.GetAsync($"{Rutas.Base}/partidas/{Guid.NewGuid()}/lobby");
        var estado = await client.GetAsync($"{Rutas.Base}/partidas/{Guid.NewGuid()}/estado");

        // Autorizado (la partida no existe → 404 de dominio, jamás 403/401).
        Assert.NotEqual(HttpStatusCode.Forbidden, lobby.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, lobby.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, estado.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, estado.StatusCode);
    }

    [Fact]
    public async Task Sin_identidad_es_401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"{Rutas.Base}/mi-sesion");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // HU-38: envios-tesoro usa policy por rol base (OperadorOAdministrador), no GestionarPartidas —
    // el Administrador observador debe verlo aunque no tenga el permiso funcional de gestión.
    [Fact]
    public async Task Envios_tesoro_sin_rol_Operador_ni_Administrador_es_403()
    {
        var client = _factory.CreateClientAs(Guid.NewGuid(), "ParticiparEnPartidas");

        var response = await client.GetAsync($"{Rutas.Base}/partidas/{Guid.NewGuid()}/juego-actual/envios-tesoro");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("Operador")]
    [InlineData("Administrador")]
    public async Task Envios_tesoro_con_rol_Operador_o_Administrador_no_es_403(string rol)
    {
        var client = _factory.CreateClientAs(Guid.NewGuid(), rol);

        var response = await client.GetAsync($"{Rutas.Base}/partidas/{Guid.NewGuid()}/juego-actual/envios-tesoro");

        // Autorizado (la partida no existe → 404 de dominio, jamás 403/401).
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
