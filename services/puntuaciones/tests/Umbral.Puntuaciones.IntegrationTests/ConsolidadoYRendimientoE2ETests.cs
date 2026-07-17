using System.Net;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Umbral.Puntuaciones.Application.Commands;
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.IntegrationTests;

public class ConsolidadoYRendimientoE2ETests : IClassFixture<PuntuacionesWebFactory>
{
    private readonly PuntuacionesWebFactory _factory;
    private static readonly DateTime Ahora = DateTime.UtcNow;

    public ConsolidadoYRendimientoE2ETests(PuntuacionesWebFactory factory) => _factory = factory;

    private async Task Proyectar(IBaseRequest comando)
    {
        using var scope = _factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        await sender.Send(comando);
    }

    private async Task SembrarPartidaEquipoTerminada(Guid partidaId, Guid equipoGanador, Guid equipoRival, int puntosGanador, int puntosRival, DateTime fechaFin)
    {
        var sesionId = Guid.NewGuid();
        var juegoId = Guid.NewGuid();
        await Proyectar(new ProyectarPartidaPublicadaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, Modalidad.Equipo));
        await Proyectar(new ProyectarJuegoActivadoCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juegoId, 1, TipoJuego.BusquedaDelTesoro));
        await Proyectar(new ProyectarEtapaBdtGanadaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juegoId, Guid.NewGuid(), Guid.NewGuid(), puntosGanador, 1000, equipoGanador));
        await Proyectar(new ProyectarEtapaBdtGanadaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juegoId, Guid.NewGuid(), Guid.NewGuid(), puntosRival, 2000, equipoRival));
        await Proyectar(new ProyectarPartidaFinalizadaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, fechaFin));
    }

    [Fact]
    public async Task Consolidado_de_partida_terminada_ordena_por_juegos_ganados()
    {
        var partidaId = Guid.NewGuid();
        var sesionId = Guid.NewGuid();
        var juego1 = Guid.NewGuid();
        var juego2 = Guid.NewGuid();
        var juego3 = Guid.NewGuid();
        var constante = Guid.NewGuid();  // gana juego1 y juego2 con 10+10 puntos
        var goleador = Guid.NewGuid();   // gana solo juego3 con 50 puntos

        await Proyectar(new ProyectarPartidaPublicadaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, Modalidad.Individual));
        await Proyectar(new ProyectarJuegoActivadoCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juego1, 1, TipoJuego.Trivia));
        await Proyectar(new ProyectarPuntajeTriviaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juego1, Guid.NewGuid(), constante, 10, 1000, null));
        await Proyectar(new ProyectarPuntajeTriviaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juego1, Guid.NewGuid(), goleador, 9, 500, null));
        await Proyectar(new ProyectarJuegoActivadoCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juego2, 2, TipoJuego.Trivia));
        await Proyectar(new ProyectarPuntajeTriviaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juego2, Guid.NewGuid(), constante, 10, 1000, null));
        await Proyectar(new ProyectarJuegoActivadoCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juego3, 3, TipoJuego.BusquedaDelTesoro));
        await Proyectar(new ProyectarEtapaBdtGanadaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juego3, Guid.NewGuid(), goleador, 50, 800, null));
        await Proyectar(new ProyectarPartidaFinalizadaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, Ahora));

        var client = _factory.CreateClientAutenticado();
        var response = await client.GetAsync($"/puntuaciones/partidas/{partidaId}/ranking-consolidado");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var entradas = json.RootElement.GetProperty("entradas");
        Assert.Equal(2, entradas.GetArrayLength());
        Assert.Equal(constante, entradas[0].GetProperty("competidorId").GetGuid());
        Assert.Equal(2, entradas[0].GetProperty("juegosGanados").GetInt32());
        Assert.Equal(20, entradas[0].GetProperty("puntosTotales").GetInt32());
        Assert.Equal(goleador, entradas[1].GetProperty("competidorId").GetGuid());
        Assert.Equal(1, entradas[1].GetProperty("juegosGanados").GetInt32());
        Assert.Equal(59, entradas[1].GetProperty("puntosTotales").GetInt32());
    }

    [Fact]
    public async Task Consolidado_de_partida_no_terminada_devuelve_409()
    {
        var partidaId = Guid.NewGuid();
        await Proyectar(new ProyectarPartidaPublicadaCommand(Guid.NewGuid(), Ahora, partidaId, Guid.NewGuid(), Modalidad.Individual));

        var client = _factory.CreateClientAutenticado();
        var response = await client.GetAsync($"/puntuaciones/partidas/{partidaId}/ranking-consolidado");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.True(json.RootElement.TryGetProperty("message", out _));
    }

    [Fact]
    public async Task Consolidado_de_partida_desconocida_devuelve_404()
    {
        var client = _factory.CreateClientAutenticado();

        var response = await client.GetAsync($"/puntuaciones/partidas/{Guid.NewGuid()}/ranking-consolidado");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Scoring_tardio_tras_finalizar_se_refleja_al_releer()
    {
        var partidaId = Guid.NewGuid();
        var sesionId = Guid.NewGuid();
        var juegoId = Guid.NewGuid();
        var competidor = Guid.NewGuid();

        await Proyectar(new ProyectarJuegoActivadoCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juegoId, 1, TipoJuego.Trivia));
        await Proyectar(new ProyectarPuntajeTriviaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juegoId, Guid.NewGuid(), competidor, 10, 1000, null));
        await Proyectar(new ProyectarPartidaFinalizadaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, Ahora));

        var client = _factory.CreateClientAutenticado();
        var antes = await client.GetAsync($"/puntuaciones/partidas/{partidaId}/ranking-consolidado");
        using var jsonAntes = JsonDocument.Parse(await antes.Content.ReadAsStringAsync());
        var puntosAntes = jsonAntes.RootElement.GetProperty("entradas")[0].GetProperty("puntosTotales").GetInt32();

        // Evento de scoring que llega DESPUÉS de PartidaFinalizada (best-effort, sin orden garantizado).
        await Proyectar(new ProyectarPuntajeTriviaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juegoId, Guid.NewGuid(), competidor, 5, 500, null));
        var despues = await client.GetAsync($"/puntuaciones/partidas/{partidaId}/ranking-consolidado");
        using var jsonDespues = JsonDocument.Parse(await despues.Content.ReadAsStringAsync());

        Assert.Equal(10, puntosAntes);
        Assert.Equal(15, jsonDespues.RootElement.GetProperty("entradas")[0].GetProperty("puntosTotales").GetInt32());
    }

    [Fact]
    public async Task Rendimiento_de_equipo_lista_partidas_con_posicion_y_gano()
    {
        var equipo = Guid.NewGuid();
        var rival = Guid.NewGuid();
        var ganada = Guid.NewGuid();
        var perdida = Guid.NewGuid();

        await SembrarPartidaEquipoTerminada(ganada, equipo, rival, 20, 10, new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc));
        await SembrarPartidaEquipoTerminada(perdida, rival, equipo, 30, 5, new DateTime(2026, 7, 4, 12, 0, 0, DateTimeKind.Utc));

        // EquiposController ahora exige rol AND privilegio (Task 5); Administrador trae
        // GestionarEquipos por default.
        var client = _factory.CreateClientConRoles("Administrador", "GestionarEquipos");
        var response = await client.GetAsync($"/puntuaciones/equipos/{equipo}/rendimiento");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(equipo, json.RootElement.GetProperty("equipoId").GetGuid());
        var partidas = json.RootElement.GetProperty("partidas");
        Assert.Equal(2, partidas.GetArrayLength());
        Assert.Equal(perdida, partidas[0].GetProperty("partidaId").GetGuid());
        Assert.Equal(2, partidas[0].GetProperty("posicion").GetInt32());
        Assert.False(partidas[0].GetProperty("gano").GetBoolean());
        Assert.Equal(ganada, partidas[1].GetProperty("partidaId").GetGuid());
        Assert.Equal(1, partidas[1].GetProperty("posicion").GetInt32());
        Assert.True(partidas[1].GetProperty("gano").GetBoolean());
    }

    [Fact]
    public async Task Rendimiento_de_equipo_sin_participaciones_devuelve_lista_vacia()
    {
        var client = _factory.CreateClientConRoles("Administrador", "GestionarEquipos");

        var response = await client.GetAsync($"/puntuaciones/equipos/{Guid.NewGuid()}/rendimiento");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, json.RootElement.GetProperty("partidas").GetArrayLength());
    }
}
