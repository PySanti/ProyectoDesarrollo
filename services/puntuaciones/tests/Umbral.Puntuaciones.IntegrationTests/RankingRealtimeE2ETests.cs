using System.Text.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Umbral.Puntuaciones.Api.Workers;
using Umbral.Puntuaciones.Application.Commands;
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.IntegrationTests;

public class RankingRealtimeE2ETests : IClassFixture<PuntuacionesWebFactory>
{
    private static readonly DateTime Ahora = DateTime.UtcNow;
    private static readonly TimeSpan Espera = TimeSpan.FromSeconds(10);
    private readonly PuntuacionesWebFactory _factory;

    public RankingRealtimeE2ETests(PuntuacionesWebFactory factory) => _factory = factory;

    // Mismo camino que el worker en producción: proyección + difusión best-effort.
    private async Task ProyectarPorPipeline(object comando)
    {
        var pipeline = _factory.Services.GetRequiredService<ProyeccionPipeline>();
        await pipeline.EjecutarAsync(comando, CancellationToken.None);
    }

    private HubConnection Conectar()
    {
        _ = _factory.Server; // fuerza el arranque del TestServer
        return new HubConnectionBuilder()
            .WithUrl(new Uri(_factory.Server.BaseAddress, "puntuaciones/hubs/ranking"), options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                options.Headers.Add("X-Test-Sub", Guid.NewGuid().ToString());
                options.Transports = HttpTransportType.LongPolling; // el TestServer no soporta WebSocket
            })
            .Build();
    }

    [Fact]
    public async Task Puntaje_trivia_proyectado_difunde_el_ranking_recalculado_al_grupo()
    {
        var partidaId = Guid.NewGuid();
        var sesionId = Guid.NewGuid();
        var juegoId = Guid.NewGuid();
        var competidor = Guid.NewGuid();
        await ProyectarPorPipeline(new ProyectarPartidaPublicadaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, Modalidad.Individual));
        await ProyectarPorPipeline(new ProyectarJuegoActivadoCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juegoId, 1, TipoJuego.Trivia));

        await using var conexion = Conectar();
        var recibido = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        conexion.On<JsonElement>("RankingTriviaActualizado", payload => recibido.TrySetResult(payload));
        await conexion.StartAsync();
        await conexion.InvokeAsync("SuscribirAPartida", partidaId);

        await ProyectarPorPipeline(new ProyectarPuntajeTriviaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juegoId,
            Guid.NewGuid(), competidor, 10, 1500, null));

        var payload = await recibido.Task.WaitAsync(Espera);
        Assert.Equal(juegoId, payload.GetProperty("juegoId").GetGuid());
        Assert.Equal("Trivia", payload.GetProperty("tipoJuego").GetString());
        var entrada = payload.GetProperty("entradas")[0];
        Assert.Equal(competidor, entrada.GetProperty("competidorId").GetGuid());
        Assert.Equal(10, entrada.GetProperty("puntos").GetInt32());
    }

    [Fact]
    public async Task Partida_finalizada_difunde_el_consolidado()
    {
        var partidaId = Guid.NewGuid();
        var sesionId = Guid.NewGuid();
        var juegoId = Guid.NewGuid();
        var competidor = Guid.NewGuid();
        await ProyectarPorPipeline(new ProyectarPartidaPublicadaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, Modalidad.Individual));
        await ProyectarPorPipeline(new ProyectarJuegoActivadoCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juegoId, 1, TipoJuego.BusquedaDelTesoro));
        await ProyectarPorPipeline(new ProyectarEtapaBdtGanadaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, juegoId,
            Guid.NewGuid(), competidor, 25, 4000, null));

        await using var conexion = Conectar();
        var recibido = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        conexion.On<JsonElement>("RankingConsolidadoCalculado", payload => recibido.TrySetResult(payload));
        await conexion.StartAsync();
        await conexion.InvokeAsync("SuscribirAPartida", partidaId);

        await ProyectarPorPipeline(new ProyectarPartidaFinalizadaCommand(Guid.NewGuid(), Ahora, partidaId, sesionId, Ahora));

        var payload = await recibido.Task.WaitAsync(Espera);
        Assert.Equal(partidaId, payload.GetProperty("partidaId").GetGuid());
        var entrada = payload.GetProperty("entradas")[0];
        Assert.Equal(competidor, entrada.GetProperty("competidorId").GetGuid());
        Assert.Equal(1, entrada.GetProperty("juegosGanados").GetInt32());
        Assert.Equal(25, entrada.GetProperty("puntosTotales").GetInt32());
    }

    [Fact]
    public async Task Suscribirse_a_partida_no_proyectada_lanza_HubException()
    {
        await using var conexion = Conectar();
        await conexion.StartAsync();

        await Assert.ThrowsAsync<Microsoft.AspNetCore.SignalR.HubException>(
            () => conexion.InvokeAsync("SuscribirAPartida", Guid.NewGuid()));
    }
}
