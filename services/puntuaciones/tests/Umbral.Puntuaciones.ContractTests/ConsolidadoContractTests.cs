using System.Net;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Umbral.Puntuaciones.Application.Commands;
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.ContractTests;

public class ConsolidadoContractTests : IClassFixture<PuntuacionesWebFactory>
{
    private readonly PuntuacionesWebFactory _factory;

    public ConsolidadoContractTests(PuntuacionesWebFactory factory) => _factory = factory;

    private async Task Proyectar(IBaseRequest comando)
    {
        using var scope = _factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        await sender.Send(comando);
    }

    [Fact]
    public async Task Consolidado_body_matches_contract()
    {
        var partidaId = Guid.NewGuid();
        var sesionId = Guid.NewGuid();
        var juegoId = Guid.NewGuid();
        var competidorId = Guid.NewGuid();
        await Proyectar(new ProyectarPartidaPublicadaCommand(Guid.NewGuid(), DateTime.UtcNow, partidaId, sesionId, Modalidad.Individual));
        await Proyectar(new ProyectarJuegoActivadoCommand(Guid.NewGuid(), DateTime.UtcNow, partidaId, sesionId, juegoId, 1, TipoJuego.Trivia));
        await Proyectar(new ProyectarPuntajeTriviaCommand(Guid.NewGuid(), DateTime.UtcNow, partidaId, sesionId, juegoId, Guid.NewGuid(), competidorId, 10, 1500, null));
        await Proyectar(new ProyectarPartidaFinalizadaCommand(Guid.NewGuid(), DateTime.UtcNow, partidaId, sesionId, DateTime.UtcNow));

        var client = _factory.CreateClientAutenticado();
        var response = await client.GetAsync($"/puntuaciones/partidas/{partidaId}/ranking-consolidado");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var root = json.RootElement;
        Assert.Equal(partidaId, root.GetProperty("partidaId").GetGuid());
        Assert.True(root.TryGetProperty("generadoEn", out _));
        var entrada = root.GetProperty("entradas")[0];
        Assert.Equal(1, entrada.GetProperty("posicion").GetInt32());
        Assert.Equal(competidorId, entrada.GetProperty("competidorId").GetGuid());
        Assert.Equal("Participante", entrada.GetProperty("tipoCompetidor").GetString());
        Assert.Equal(1, entrada.GetProperty("juegosGanados").GetInt32());
        Assert.Equal(10, entrada.GetProperty("puntosTotales").GetInt32());
        Assert.Equal(1500, entrada.GetProperty("tiempoTotalMs").GetInt64());
    }

    [Fact]
    public async Task Consolidado_409_devuelve_message_json()
    {
        var partidaId = Guid.NewGuid();
        await Proyectar(new ProyectarPartidaPublicadaCommand(Guid.NewGuid(), DateTime.UtcNow, partidaId, Guid.NewGuid(), Modalidad.Individual));

        var client = _factory.CreateClientAutenticado();
        var response = await client.GetAsync($"/puntuaciones/partidas/{partidaId}/ranking-consolidado");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.True(json.RootElement.TryGetProperty("message", out _));
    }

    [Fact]
    public async Task Rendimiento_body_matches_contract()
    {
        var equipoId = Guid.NewGuid();
        var partidaId = Guid.NewGuid();
        var sesionId = Guid.NewGuid();
        var juegoId = Guid.NewGuid();
        await Proyectar(new ProyectarPartidaPublicadaCommand(Guid.NewGuid(), DateTime.UtcNow, partidaId, sesionId, Modalidad.Equipo));
        await Proyectar(new ProyectarJuegoActivadoCommand(Guid.NewGuid(), DateTime.UtcNow, partidaId, sesionId, juegoId, 1, TipoJuego.BusquedaDelTesoro));
        await Proyectar(new ProyectarEtapaBdtGanadaCommand(Guid.NewGuid(), DateTime.UtcNow, partidaId, sesionId, juegoId, Guid.NewGuid(), Guid.NewGuid(), 25, 4000, equipoId));
        await Proyectar(new ProyectarPartidaFinalizadaCommand(Guid.NewGuid(), DateTime.UtcNow, partidaId, sesionId, DateTime.UtcNow));

        var client = _factory.CreateClientAutenticado();
        var response = await client.GetAsync($"/puntuaciones/equipos/{equipoId}/rendimiento");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var root = json.RootElement;
        Assert.Equal(equipoId, root.GetProperty("equipoId").GetGuid());
        var partida = root.GetProperty("partidas")[0];
        Assert.Equal(partidaId, partida.GetProperty("partidaId").GetGuid());
        Assert.True(partida.TryGetProperty("fechaFin", out _));
        Assert.Equal(1, partida.GetProperty("posicion").GetInt32());
        Assert.True(partida.GetProperty("gano").GetBoolean());
    }
}
