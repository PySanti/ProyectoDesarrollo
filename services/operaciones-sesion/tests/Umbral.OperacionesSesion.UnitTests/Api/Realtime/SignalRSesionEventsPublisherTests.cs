using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Umbral.OperacionesSesion.Api.Realtime;
using Umbral.OperacionesSesion.Application.Interfaces;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Api.Realtime;

public class SignalRSesionEventsPublisherTests
{
    private static readonly DateTime T0 = new(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc);

    private static (SignalRSesionEventsPublisher pub, FakeHubClients clients) Build()
    {
        var clients = new FakeHubClients();
        var ctx = new FakeHubContext(clients);
        return (new SignalRSesionEventsPublisher(ctx), clients);
    }

    [Fact]
    public async Task JuegoActivado_difunde_al_grupo_con_payload()
    {
        var (pub, clients) = Build();
        var partidaId = Guid.NewGuid();
        var juegoId = Guid.NewGuid();

        await pub.PublicarJuegoActivadoAsync(
            new JuegoActivadoEvent(partidaId, Guid.NewGuid(), juegoId, 2, "Trivia"), CancellationToken.None);

        Assert.Equal(SesionRealtimeMessages.GrupoPartida(partidaId), clients.LastGroup);
        Assert.Equal(SesionRealtimeMessages.JuegoActivado, clients.Proxy.Method);
        var payload = Assert.IsType<JuegoActivadoPayload>(clients.Proxy.Args![0]);
        Assert.Equal(partidaId, payload.PartidaId);
        Assert.Equal(juegoId, payload.JuegoId);
        Assert.Equal(2, payload.Orden);
        Assert.Equal("Trivia", payload.TipoJuego);
    }

    [Fact]
    public async Task PreguntaActivada_deriva_fechaLimite_de_activacion_mas_tiempo()
    {
        var (pub, clients) = Build();
        var partidaId = Guid.NewGuid();

        await pub.PublicarPreguntaTriviaActivadaAsync(
            new PreguntaTriviaActivadaEvent(partidaId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, 30, T0),
            CancellationToken.None);

        Assert.Equal(SesionRealtimeMessages.PreguntaActivada, clients.Proxy.Method);
        Assert.Equal(SesionRealtimeMessages.GrupoPartida(partidaId), clients.LastGroup);
        var payload = Assert.IsType<PreguntaActivadaPayload>(clients.Proxy.Args![0]);
        Assert.Equal(T0.AddSeconds(30), payload.FechaLimiteUtc);
        Assert.Equal(1, payload.Orden);
    }

    [Fact]
    public async Task EtapaActivada_deriva_fechaLimite()
    {
        var (pub, clients) = Build();
        var partidaId = Guid.NewGuid();
        await pub.PublicarEtapaBDTActivadaAsync(
            new EtapaBDTActivadaEvent(partidaId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, 45, T0),
            CancellationToken.None);

        Assert.Equal(SesionRealtimeMessages.EtapaActivada, clients.Proxy.Method);
        Assert.Equal(SesionRealtimeMessages.GrupoPartida(partidaId), clients.LastGroup);
        var payload = Assert.IsType<EtapaActivadaPayload>(clients.Proxy.Args![0]);
        Assert.Equal(T0.AddSeconds(45), payload.FechaLimiteUtc);
    }

    [Fact]
    public async Task EtapaGanada_difunde_sin_puntaje()
    {
        var (pub, clients) = Build();
        var partidaId = Guid.NewGuid();
        var juegoId = Guid.NewGuid();
        var etapaId = Guid.NewGuid();
        await pub.PublicarEtapaBDTGanadaAsync(
            new EtapaBDTGanadaEvent(partidaId, Guid.NewGuid(), juegoId, etapaId, Guid.NewGuid(), 100, 1234),
            CancellationToken.None);

        Assert.Equal(SesionRealtimeMessages.EtapaGanada, clients.Proxy.Method);
        Assert.Equal(SesionRealtimeMessages.GrupoPartida(partidaId), clients.LastGroup);
        var payload = Assert.IsType<EtapaGanadaPayload>(clients.Proxy.Args![0]);
        // payload no expone Puntaje: la sola existencia del tipo lo garantiza en compilación
        Assert.Equal(partidaId, payload.PartidaId);
        Assert.Equal(juegoId, payload.JuegoId);
        Assert.Equal(etapaId, payload.EtapaId);
    }

    [Fact]
    public async Task PartidaFinalizada_difunde()
    {
        var (pub, clients) = Build();
        var partidaId = Guid.NewGuid();
        await pub.PublicarPartidaFinalizadaAsync(
            new PartidaFinalizadaEvent(partidaId, Guid.NewGuid(), T0), CancellationToken.None);

        Assert.Equal(SesionRealtimeMessages.PartidaFinalizada, clients.Proxy.Method);
        Assert.Equal(SesionRealtimeMessages.GrupoPartida(partidaId), clients.LastGroup);
        var payload = Assert.IsType<PartidaFinalizadaPayload>(clients.Proxy.Args![0]);
        Assert.Equal(partidaId, payload.PartidaId);
    }

    [Fact]
    public async Task Eventos_scoring_adjacentes_no_difunden()
    {
        var (pub, clients) = Build();

        await pub.PublicarRespuestaTriviaValidadaAsync(
            new RespuestaTriviaValidadaEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), true, T0),
            CancellationToken.None);
        await pub.PublicarPuntajeTriviaIncrementadoAsync(
            new PuntajeTriviaIncrementadoEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 10, 500),
            CancellationToken.None);
        await pub.PublicarTesoroQRValidadoAsync(
            new TesoroQRValidadoEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Correcto", T0),
            CancellationToken.None);

        Assert.Null(clients.LastGroup);     // nunca se pidió grupo
        Assert.Null(clients.Proxy.Method);  // nunca se envió
    }

    [Fact]
    public async Task PistaEnviada_difunde_solo_al_grupo_del_participante_destino()
    {
        var (pub, clients) = Build();
        var partidaId = Guid.NewGuid();
        var juegoId = Guid.NewGuid();
        var destino = Guid.NewGuid();

        await pub.PublicarPistaEnviadaAsync(
            new PistaEnviadaEvent(partidaId, Guid.NewGuid(), juegoId, destino, "Mira bajo el faro", T0),
            CancellationToken.None);

        Assert.Equal(SesionRealtimeMessages.GrupoParticipante(destino), clients.LastGroup); // NO GrupoPartida
        Assert.Equal(SesionRealtimeMessages.PistaEnviada, clients.Proxy.Method);
        var payload = Assert.IsType<PistaEnviadaPayload>(clients.Proxy.Args![0]);
        Assert.Equal(partidaId, payload.PartidaId);
        Assert.Equal(juegoId, payload.JuegoId);
        Assert.Equal(destino, payload.ParticipanteDestinoId);
        Assert.Equal("Mira bajo el faro", payload.Texto);
        Assert.Equal(T0, payload.TimestampUtc);
    }

    [Fact]
    public async Task PistaEnviada_con_equipo_destino_difunde_solo_al_grupo_del_equipo()
    {
        var (pub, clients) = Build();
        var partidaId = Guid.NewGuid();
        var juegoId = Guid.NewGuid();
        var equipo = Guid.NewGuid();

        await pub.PublicarPistaEnviadaAsync(
            new PistaEnviadaEvent(partidaId, Guid.NewGuid(), juegoId, null, "Al norte del patio", T0, equipo),
            CancellationToken.None);

        Assert.Equal(SesionRealtimeMessages.GrupoEquipo(equipo), clients.LastGroup);
        Assert.Equal(SesionRealtimeMessages.PistaEnviada, clients.Proxy.Method);
        var payload = Assert.IsType<PistaEnviadaPayload>(clients.Proxy.Args![0]);
        Assert.Equal(equipo, payload.EquipoDestinoId);
        Assert.Null(payload.ParticipanteDestinoId);
        Assert.Equal("Al norte del patio", payload.Texto);
    }

    [Fact]
    public async Task ConvocatoriaCreada_difunde_solo_al_grupo_del_convocado()
    {
        var (pub, clients) = Build();
        var partidaId = Guid.NewGuid();
        var equipoId = Guid.NewGuid();
        var convocatoriaId = Guid.NewGuid();
        var convocado = Guid.NewGuid();

        await pub.PublicarConvocatoriaCreadaAsync(
            new ConvocatoriaCreadaEvent(partidaId, Guid.NewGuid(), convocatoriaId, equipoId, convocado),
            CancellationToken.None);

        Assert.Equal(SesionRealtimeMessages.GrupoParticipante(convocado), clients.LastGroup); // NO GrupoPartida
        Assert.Equal(SesionRealtimeMessages.ConvocatoriaCreada, clients.Proxy.Method);
        var payload = Assert.IsType<ConvocatoriaCreadaPayload>(clients.Proxy.Args![0]);
        Assert.Equal(partidaId, payload.PartidaId);
        Assert.Equal(equipoId, payload.EquipoId);
        Assert.Equal(convocatoriaId, payload.ConvocatoriaId);
        Assert.Equal(convocado, payload.UsuarioId);
    }

    [Fact]
    public async Task ConvocatoriaRespondida_no_difunde()
    {
        var (pub, clients) = Build();

        await pub.PublicarConvocatoriaRespondidaAsync(
            new ConvocatoriaRespondidaEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Aceptada"),
            CancellationToken.None);

        Assert.Null(clients.LastGroup); // sin difusión
    }

    [Fact]
    public async Task Ubicacion_no_difunde()
    {
        var (pub, clients) = Build();

        await pub.PublicarUbicacionActualizadaAsync(
            new UbicacionActualizadaEvent(Guid.NewGuid(), Guid.NewGuid(), 10.5, -66.9, T0),
            CancellationToken.None);

        Assert.Null(clients.LastGroup); // sin difusión
    }

    // ---- Fakes locales ----

    private sealed class FakeHubContext : IHubContext<SesionHub>
    {
        public FakeHubContext(IHubClients clients) => Clients = clients;
        public IHubClients Clients { get; }
        public IGroupManager Groups => throw new NotImplementedException();
    }

    private sealed class FakeHubClients : IHubClients
    {
        public string? LastGroup { get; private set; }
        public FakeClientProxy Proxy { get; } = new();
        public IClientProxy Group(string groupName) { LastGroup = groupName; return Proxy; }

        public IClientProxy All => throw new NotImplementedException();
        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => throw new NotImplementedException();
        public IClientProxy Client(string connectionId) => throw new NotImplementedException();
        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => throw new NotImplementedException();
        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => throw new NotImplementedException();
        public IClientProxy Groups(IReadOnlyList<string> groupNames) => throw new NotImplementedException();
        public IClientProxy User(string userId) => throw new NotImplementedException();
        public IClientProxy Users(IReadOnlyList<string> userIds) => throw new NotImplementedException();
    }

    private sealed class FakeClientProxy : IClientProxy
    {
        public string? Method { get; private set; }
        public object?[]? Args { get; private set; }
        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
        { Method = method; Args = args; return Task.CompletedTask; }
    }
}
