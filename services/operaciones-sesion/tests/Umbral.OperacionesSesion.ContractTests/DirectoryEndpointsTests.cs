using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Umbral.OperacionesSesion.Application.DTOs;
using Xunit;

namespace Umbral.OperacionesSesion.ContractTests;

public class DirectoryEndpointsTests : IClassFixture<OperacionesSesionWebFactory>
{
    private const string Ruta = Rutas.Base + "/directory/partidas";

    private readonly OperacionesSesionWebFactory _factory;
    private readonly HttpClient _client;

    public DirectoryEndpointsTests(OperacionesSesionWebFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClientAs(Guid.NewGuid());
    }

    // Publica una partida para que exista la fila SesionPartida con su snapshot de nombre.
    private async Task<Guid> PublicarAsync(string nombre)
    {
        var partidaId = Guid.NewGuid();
        _factory.Stub.Respuestas[partidaId] = new ConfiguracionPartidaDto(
            nombre, "Individual", "Manual", null, 1, 10,
            new List<JuegoResumenDto> { new(Guid.NewGuid(), 1, "Trivia") });

        var publish = await _client.PostAsync($"{Rutas.Base}/partidas/{partidaId}/publicacion", null);
        Assert.Equal(HttpStatusCode.Created, publish.StatusCode);
        return partidaId;
    }

    [Fact]
    public async Task Resuelve_el_nombre_de_una_partida_publicada()
    {
        var partidaId = await PublicarAsync("Copa UMBRAL");

        var response = await _client.PostAsJsonAsync(Ruta, new { partidaIds = new[] { partidaId } });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ResolverNombresPartidaResponse>();
        var dto = Assert.Single(body!.Partidas);
        Assert.Equal(partidaId, dto.PartidaId);
        Assert.Equal("Copa UMBRAL", dto.Nombre);
    }

    [Fact]
    public async Task Id_desconocido_se_omite_con_200()
    {
        var response = await _client.PostAsJsonAsync(Ruta, new { partidaIds = new[] { Guid.NewGuid() } });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ResolverNombresPartidaResponse>();
        Assert.Empty(body!.Partidas);
    }

    [Fact]
    public async Task Body_sin_partidaIds_devuelve_lista_vacia()
    {
        var response = await _client.PostAsJsonAsync(Ruta, new { });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ResolverNombresPartidaResponse>();
        Assert.Empty(body!.Partidas);
    }

    [Fact]
    public async Task Participante_sin_permisos_de_operador_puede_resolver()
    {
        // El punto del slice: el movil (Participante) no llega a /partidas/** por el gateway,
        // asi que este endpoint tiene que servirle a el.
        var partidaId = await PublicarAsync("Copa UMBRAL");
        var participante = _factory.CreateClientAs(Guid.NewGuid(), "ParticiparEnPartidas");

        var response = await participante.PostAsJsonAsync(Ruta, new { partidaIds = new[] { partidaId } });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ResolverNombresPartidaResponse>();
        Assert.Equal("Copa UMBRAL", Assert.Single(body!.Partidas).Nombre);
    }

    [Fact]
    public async Task Sin_token_devuelve_401()
    {
        var anonimo = _factory.CreateClient(); // sin X-Test-Sub

        var response = await anonimo.PostAsJsonAsync(Ruta, new { partidaIds = Array.Empty<Guid>() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Lote_sobre_el_tope_devuelve_400()
    {
        // El tope lo aplica el ValidationBehavior del pipeline, no el controller: por eso se
        // prueba aqui y no en el unit de DirectoryController.
        var demasiados = Enumerable.Range(0, 201).Select(_ => Guid.NewGuid()).ToArray();

        var response = await _client.PostAsJsonAsync(Ruta, new { partidaIds = demasiados });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        // Forma de error del servicio: { message }, no ValidationProblemDetails.
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        // Clavado al mensaje del validador: un 400 de binding tambien traeria { message },
        // asi que sin esto el test pasaria por el motivo equivocado.
        Assert.Contains("200", body!["message"]);
    }
}
