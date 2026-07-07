using System.Net;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Umbral.Puntuaciones.Application.Commands;
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.IntegrationTests;

public class ProyeccionYRankingE2ETests : IClassFixture<PuntuacionesWebFactory>
{
    private readonly PuntuacionesWebFactory _factory;
    private static readonly DateTime Ahora = DateTime.UtcNow;

    public ProyeccionYRankingE2ETests(PuntuacionesWebFactory factory) => _factory = factory;

    private async Task Proyectar(IBaseRequest comando)
    {
        using var scope = _factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        await sender.Send(comando);
    }

    [Fact]
    public async Task Flujo_completo_de_eventos_produce_ranking_consultable()
    {
        var partidaId = Guid.NewGuid();
        var sesionId = Guid.NewGuid();
        var juegoId = Guid.NewGuid();
        var ganador = Guid.NewGuid();
        var segundo = Guid.NewGuid();

        await Proyectar(new ProyectarPartidaPublicadaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, Modalidad.Individual));
        await Proyectar(new ProyectarPartidaIniciadaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, Ahora));
        await Proyectar(new ProyectarJuegoActivadoCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juegoId, 1, TipoJuego.Trivia));
        await Proyectar(new ProyectarPuntajeTriviaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juegoId, Guid.NewGuid(), ganador, 20, 1000, null));
        await Proyectar(new ProyectarPuntajeTriviaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juegoId, Guid.NewGuid(), segundo, 10, 2000, null));

        var client = _factory.CreateClientAutenticado();
        var response = await client.GetAsync($"/puntuaciones/partidas/{partidaId}/juegos/{juegoId}/ranking");
        var body = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var entradas = json.RootElement.GetProperty("entradas");
        Assert.Equal(2, entradas.GetArrayLength());
        Assert.Equal(ganador, entradas[0].GetProperty("competidorId").GetGuid());
        Assert.Equal(1, entradas[0].GetProperty("posicion").GetInt32());
        Assert.Equal(20, entradas[0].GetProperty("puntos").GetInt32());
        Assert.Equal("Trivia", json.RootElement.GetProperty("tipoJuego").GetString());
    }

    [Fact]
    public async Task Marcador_propio_devuelve_posicion_y_404_para_desconocido()
    {
        var partidaId = Guid.NewGuid();
        var sesionId = Guid.NewGuid();
        var juegoId = Guid.NewGuid();
        var equipoId = Guid.NewGuid();

        await Proyectar(new ProyectarJuegoActivadoCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juegoId, 1, TipoJuego.BusquedaDelTesoro));
        await Proyectar(new ProyectarEtapaBdtGanadaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juegoId, Guid.NewGuid(), Guid.NewGuid(), 25, 4000, equipoId));

        var client = _factory.CreateClientAutenticado();
        var ok = await client.GetAsync($"/puntuaciones/partidas/{partidaId}/juegos/{juegoId}/marcadores/{equipoId}");
        var notFound = await client.GetAsync($"/puntuaciones/partidas/{partidaId}/juegos/{juegoId}/marcadores/{Guid.NewGuid()}");
        using var json = JsonDocument.Parse(await ok.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);
        Assert.Equal(25, json.RootElement.GetProperty("puntos").GetInt32());
        Assert.Equal(1, json.RootElement.GetProperty("posicion").GetInt32());
        Assert.Equal("Equipo", json.RootElement.GetProperty("tipoCompetidor").GetString());
    }

    [Fact]
    public async Task Ranking_de_juego_desconocido_devuelve_404()
    {
        var client = _factory.CreateClientAutenticado();

        var response = await client.GetAsync($"/puntuaciones/partidas/{Guid.NewGuid()}/juegos/{Guid.NewGuid()}/ranking");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Evento_duplicado_no_duplica_puntos_e2e()
    {
        var partidaId = Guid.NewGuid();
        var sesionId = Guid.NewGuid();
        var juegoId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        await Proyectar(new ProyectarJuegoActivadoCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juegoId, 1, TipoJuego.Trivia));
        var duplicado = new ProyectarPuntajeTriviaCommand(eventId, Ahora, partidaId, sesionId, juegoId, Guid.NewGuid(), participanteId, 10, 1000, null);
        await Proyectar(duplicado);
        await Proyectar(duplicado);

        var client = _factory.CreateClientAutenticado();
        var response = await client.GetAsync($"/puntuaciones/partidas/{partidaId}/juegos/{juegoId}/marcadores/{participanteId}");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(10, json.RootElement.GetProperty("puntos").GetInt32());
        Assert.Equal(1, json.RootElement.GetProperty("unidadesGanadas").GetInt32());
    }
}
