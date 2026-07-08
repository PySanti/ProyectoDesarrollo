using System.Net;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Umbral.Puntuaciones.Application.Commands;
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.IntegrationTests;

public class HistorialPartidasE2ETests : IClassFixture<PuntuacionesWebFactory>
{
    private readonly PuntuacionesWebFactory _factory;
    private static readonly DateTime Ahora = new(2026, 7, 6, 12, 0, 0, DateTimeKind.Utc);

    public HistorialPartidasE2ETests(PuntuacionesWebFactory factory) => _factory = factory;

    private async Task Proyectar(IBaseRequest comando)
    {
        using var scope = _factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        await sender.Send(comando);
    }

    [Fact]
    public async Task Individual_de_punta_a_punta_con_puntos_posicion_y_juegos()
    {
        var participanteId = Guid.NewGuid();
        var rival = Guid.NewGuid();
        var partidaId = Guid.NewGuid();
        var sesionId = Guid.NewGuid();
        var juegoId = Guid.NewGuid();

        await Proyectar(new ProyectarPartidaPublicadaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, Modalidad.Individual));
        await Proyectar(new ProyectarJuegoActivadoCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juegoId, 1, TipoJuego.Trivia));
        await Proyectar(new ProyectarPuntajeTriviaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juegoId, Guid.NewGuid(), participanteId, 20, 1000, null));
        await Proyectar(new ProyectarPuntajeTriviaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juegoId, Guid.NewGuid(), rival, 10, 900, null));
        await Proyectar(new ProyectarPartidaFinalizadaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, Ahora));

        var client = _factory.CreateClientAutenticado();
        var response = await client.GetAsync($"/puntuaciones/participantes/{participanteId}/historial-partidas");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var partida = json.RootElement.GetProperty("partidas")[0];
        Assert.Equal(partidaId, partida.GetProperty("partidaId").GetGuid());
        Assert.Equal("Individual", partida.GetProperty("modalidad").GetString());
        Assert.Equal(20, partida.GetProperty("puntosTotales").GetInt32());
        Assert.Equal(1, partida.GetProperty("posicion").GetInt32());
        Assert.True(partida.GetProperty("gano").GetBoolean());
        Assert.Equal(JsonValueKind.Null, partida.GetProperty("equipoId").ValueKind);
        var juego = partida.GetProperty("juegos")[0];
        Assert.Equal("Trivia", juego.GetProperty("tipoJuego").GetString());
        Assert.Equal(20, juego.GetProperty("puntos").GetInt32());
    }

    [Fact]
    public async Task Equipo_de_punta_a_punta_resuelto_del_historial()
    {
        var participanteId = Guid.NewGuid();
        var equipoId = Guid.NewGuid();
        var rival = Guid.NewGuid();
        var partidaId = Guid.NewGuid();
        var sesionId = Guid.NewGuid();
        var juegoId = Guid.NewGuid();

        await Proyectar(new ProyectarPartidaPublicadaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, Modalidad.Equipo));
        await Proyectar(new ProyectarJuegoActivadoCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juegoId, 1, TipoJuego.BusquedaDelTesoro));
        await Proyectar(new ProyectarEtapaBdtGanadaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juegoId, Guid.NewGuid(), participanteId, 30, 1000, equipoId));
        await Proyectar(new ProyectarEtapaBdtGanadaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juegoId, Guid.NewGuid(), Guid.NewGuid(), 10, 900, rival));
        await Proyectar(new ProyectarPartidaFinalizadaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, Ahora));
        // La membresía HU-27 sale del historial, no de los marcadores: registrar la acción autorada.
        await Proyectar(new ProyectarEventoHistorialCommand(
            Guid.NewGuid(), "EtapaBDTGanada", Ahora, partidaId, juegoId, participanteId, equipoId, """{"puntaje":30}"""));

        var client = _factory.CreateClientAutenticado();
        var response = await client.GetAsync($"/puntuaciones/participantes/{participanteId}/historial-partidas");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var partida = json.RootElement.GetProperty("partidas")[0];
        Assert.Equal("Equipo", partida.GetProperty("modalidad").GetString());
        Assert.Equal(equipoId, partida.GetProperty("equipoId").GetGuid());
        Assert.Equal(30, partida.GetProperty("puntosTotales").GetInt32());
        Assert.Equal(1, partida.GetProperty("posicion").GetInt32());
        Assert.True(partida.GetProperty("gano").GetBoolean());
    }
}
