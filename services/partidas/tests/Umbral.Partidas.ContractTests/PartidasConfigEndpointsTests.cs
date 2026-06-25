// tests/Umbral.Partidas.ContractTests/PartidasConfigEndpointsTests.cs
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Umbral.Partidas.Application.DTOs;

namespace Umbral.Partidas.ContractTests;

public class PartidasConfigEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public PartidasConfigEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private static object CrearPartidaBody(string nombre = "Copa") => new
    {
        nombrePartida = nombre,
        modalidad = "Individual",
        modoInicioPartida = "Manual",
        tiempoInicio = (DateTime?)null,
        minimosParticipacion = 1,
        maximosParticipacion = 10
    };

    [Fact]
    public async Task Full_config_flow_returns_expected_shapes()
    {
        var create = await _client.PostAsJsonAsync("/partidas", CrearPartidaBody("Copa-" + Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        Assert.NotNull(create.Headers.Location); // 201 → Location header (partidas-config.md:27)
        var created = await create.Content.ReadFromJsonAsync<CrearPartidaResponse>();
        Assert.NotNull(created);
        var partidaId = created!.PartidaId;

        var triviaBody = new
        {
            orden = 1,
            preguntas = new[]
            {
                new { texto = "Q", opciones = new[] { new { texto = "A", esCorrecta = true }, new { texto = "B", esCorrecta = false } }, puntaje = 10, tiempoLimiteSegundos = 30 }
            }
        };
        var addTrivia = await _client.PostAsJsonAsync($"/partidas/{partidaId}/juegos/trivia", triviaBody);
        Assert.Equal(HttpStatusCode.Created, addTrivia.StatusCode);
        Assert.NotNull(addTrivia.Headers.Location);

        var bdtBody = new
        {
            orden = 2,
            areaBusqueda = "Plaza",
            etapas = new[] { new { orden = 1, codigoQREsperado = "QR", puntaje = 50, tiempoLimiteSegundos = 120 } }
        };
        var addBdt = await _client.PostAsJsonAsync($"/partidas/{partidaId}/juegos/bdt", bdtBody);
        Assert.Equal(HttpStatusCode.Created, addBdt.StatusCode);
        Assert.NotNull(addBdt.Headers.Location);

        var detail = await _client.GetFromJsonAsync<PartidaDetailDto>($"/partidas/{partidaId}");
        Assert.NotNull(detail);
        Assert.Null(detail!.Estado);
        Assert.Equal("Individual", detail.Modalidad);
        Assert.Equal("Manual", detail.ModoInicioPartida);
        Assert.Equal(2, detail.Juegos.Count);
        Assert.Equal("Trivia", detail.Juegos[0].TipoJuego);
        Assert.Equal("BusquedaDelTesoro", detail.Juegos[1].TipoJuego);
        Assert.NotNull(detail.Juegos[0].Trivia);
        Assert.Null(detail.Juegos[0].BDT);
        Assert.NotNull(detail.Juegos[1].BDT);
        Assert.Null(detail.Juegos[1].Trivia);

        var list = await _client.GetFromJsonAsync<List<PartidaSummaryDto>>("/partidas");
        Assert.NotNull(list);
        Assert.Contains(list!, p => p.PartidaId == partidaId && p.CantidadJuegos == 2);
    }

    [Fact]
    public async Task Add_game_to_missing_partida_returns_404()
    {
        var triviaBody = new
        {
            orden = 1,
            preguntas = new[]
            {
                new { texto = "Q", opciones = new[] { new { texto = "A", esCorrecta = true }, new { texto = "B", esCorrecta = false } }, puntaje = 10, tiempoLimiteSegundos = 30 }
            }
        };
        var response = await _client.PostAsJsonAsync($"/partidas/{Guid.NewGuid()}/juegos/trivia", triviaBody);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Add_bdt_game_to_missing_partida_returns_404()
    {
        var bdtBody = new
        {
            orden = 1,
            areaBusqueda = "Plaza",
            etapas = new[] { new { orden = 1, codigoQREsperado = "QR", puntaje = 50, tiempoLimiteSegundos = 120 } }
        };
        var response = await _client.PostAsJsonAsync($"/partidas/{Guid.NewGuid()}/juegos/bdt", bdtBody);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Add_game_with_duplicate_orden_returns_409()
    {
        var create = await _client.PostAsJsonAsync("/partidas", CrearPartidaBody("Copa-" + Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<CrearPartidaResponse>();
        var partidaId = created!.PartidaId;

        var triviaBody = new
        {
            orden = 1,
            preguntas = new[]
            {
                new { texto = "Q", opciones = new[] { new { texto = "A", esCorrecta = true }, new { texto = "B", esCorrecta = false } }, puntaje = 10, tiempoLimiteSegundos = 30 }
            }
        };
        var first = await _client.PostAsJsonAsync($"/partidas/{partidaId}/juegos/trivia", triviaBody);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var bdtBody = new
        {
            orden = 1, // duplicate orden → conflict (partidas-config.md:69)
            areaBusqueda = "Plaza",
            etapas = new[] { new { orden = 1, codigoQREsperado = "QR", puntaje = 50, tiempoLimiteSegundos = 120 } }
        };
        var conflict = await _client.PostAsJsonAsync($"/partidas/{partidaId}/juegos/bdt", bdtBody);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
    }

    [Fact]
    public async Task Create_partida_with_blank_name_returns_400()
    {
        var response = await _client.PostAsJsonAsync("/partidas", CrearPartidaBody(""));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_missing_partida_returns_404()
    {
        var response = await _client.GetAsync($"/partidas/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
