using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Umbral.OperacionesSesion.Api.Realtime;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Domain.Entities;
using Umbral.OperacionesSesion.Domain.Enums;
using Umbral.OperacionesSesion.Domain.ValueObjects;
using Umbral.OperacionesSesion.UnitTests.Application.Fakes;
using Xunit;

namespace Umbral.OperacionesSesion.UnitTests.Api.Realtime;

public class SesionHubTests
{
    private static ClaimsPrincipal Usuario(string? sub, string? rol)
    {
        var claims = new List<Claim>();
        if (sub is not null) claims.Add(new Claim("sub", sub));
        if (rol is not null) claims.Add(new Claim("roles", rol));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test", "name", "roles"));
    }

    private static SesionPartida SesionDe(Guid partidaId, Guid participanteId)
    {
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, Array.Empty<PreguntaSnapshot>());
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Individual, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        var s = SesionPartida.Publicar(partidaId, snap);
        var fecha = new DateTime(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc);
        var insc = s.Inscribir(participanteId, false, 0, fecha);
        s.AceptarInscripcion(insc.Id.Valor, 0, fecha); // HU-19: aceptar para inscripción activa
        return s;
    }

    private static readonly DateTime T0 = new(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc);

    private static SesionHub Construir(ISesionPartidaRepositorioFake repo, ClaimsPrincipal user,
        FakeGroupManager groups, FakeClients? clients = null, string connId = "c1",
        FakeSesionEventsPublisher? eventos = null, ISender? sender = null)
    {
        var hub = new SesionHub(repo.Repo, new FakeTimeProvider(T0), eventos ?? new FakeSesionEventsPublisher(),
            sender ?? new SenderDeConvocatorias(Array.Empty<ConvocatoriaPendienteDto>()),
            NullLogger<SesionHub>.Instance)
        {
            Context = new FakeHubCallerContext(user, connId),
            Groups = groups,
            Clients = clients ?? new FakeClients()
        };
        return hub;
    }

    private static SesionPartida SesionEquipoDe(Guid partidaId, Guid participanteId, out Guid equipoId)
    {
        var equipoLocal = Guid.NewGuid();
        var juego = new JuegoResumen(Guid.NewGuid(), 1, TipoJuego.Trivia, Array.Empty<PreguntaSnapshot>());
        var snap = new ConfiguracionSnapshot("Copa", Modalidad.Equipo, ModoInicioPartida.Manual, null, 1, 5, new[] { juego });
        var s = SesionPartida.Publicar(partidaId, snap);
        var t0 = new DateTime(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc);
        var ins = s.PreinscribirEquipo(equipoLocal, true, participanteId, new[] { participanteId }, false, 0, t0);
        s.AceptarInscripcion(ins.Id.Valor, 0, t0); // HU-19: aceptar crea las convocatorias
        s.ResponderConvocatoria(ins.Convocatorias.Single().Id.Valor, participanteId, true, false, t0);
        equipoId = equipoLocal;
        return s;
    }

    [Fact]
    public async Task Al_conectar_el_participante_entra_a_su_canal_personal()
    {
        var participanteId = Guid.NewGuid();
        var repo = new ISesionPartidaRepositorioFake();
        var groups = new FakeGroupManager();
        var hub = Construir(repo, Usuario(sub: participanteId.ToString(), rol: "Participante"), groups);

        await hub.OnConnectedAsync();

        // Sin partida de por medio: el canal personal es tuyo por ser quien eres.
        Assert.Contains(("c1", SesionRealtimeMessages.GrupoParticipante(participanteId)), groups.Added);
    }

    [Fact]
    public async Task Al_conectar_el_operador_no_entra_a_canal_personal()
    {
        var repo = new ISesionPartidaRepositorioFake();
        var groups = new FakeGroupManager();
        var hub = Construir(repo, Usuario(sub: Guid.NewGuid().ToString(), rol: "Operador"), groups);

        await hub.OnConnectedAsync();

        Assert.Empty(groups.Added);
    }

    [Fact]
    public async Task Desuscribir_de_partida_no_saca_del_canal_personal()
    {
        var partidaId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();
        var repo = new ISesionPartidaRepositorioFake();
        repo.Inner.Add(SesionDe(partidaId, participanteId));
        var groups = new FakeGroupManager();
        var hub = Construir(repo, Usuario(sub: participanteId.ToString(), rol: "Participante"), groups);
        await hub.SuscribirAPartida(partidaId);

        await hub.DesuscribirDePartida(partidaId);

        // Salir de una partida no puede dejarte sordo a tus convocatorias.
        Assert.DoesNotContain(("c1", SesionRealtimeMessages.GrupoParticipante(participanteId)), groups.Removed);
        Assert.Contains(("c1", SesionRealtimeMessages.GrupoPartida(partidaId)), groups.Removed);
    }

    [Fact]
    public async Task Operador_se_une_al_grupo_sin_consultar_repo()
    {
        var partidaId = Guid.NewGuid();
        var repo = new ISesionPartidaRepositorioFake(); // repo vacío
        var groups = new FakeGroupManager();
        var hub = Construir(repo, Usuario(sub: null, rol: "Operador"), groups);

        await hub.SuscribirAPartida(partidaId);

        Assert.Contains(("c1", SesionRealtimeMessages.GrupoPartida(partidaId)), groups.Added);
    }

    [Fact]
    public async Task Administrador_se_une_al_grupo_sin_consultar_repo()
    {
        // El admin monitorea las operaciones en modo lectura (CLAUDE.md): no tiene inscripción,
        // pero debe entrar al grupo para recibir PreguntaActivada/etc. como el operador.
        var partidaId = Guid.NewGuid();
        var repo = new ISesionPartidaRepositorioFake(); // repo vacío: el admin no está inscrito
        var groups = new FakeGroupManager();
        var hub = Construir(repo, Usuario(sub: null, rol: "Administrador"), groups);

        await hub.SuscribirAPartida(partidaId);

        Assert.Contains(("c1", SesionRealtimeMessages.GrupoPartida(partidaId)), groups.Added);
    }

    [Fact]
    public async Task Con_gestionar_partidas_se_une_al_grupo_sin_consultar_repo()
    {
        // HU-04: un Participante con el privilegio GestionarPartidas opera desde la web; tampoco
        // tiene inscripción, pero opera la sesión y debe entrar al grupo.
        var partidaId = Guid.NewGuid();
        var repo = new ISesionPartidaRepositorioFake(); // repo vacío
        var groups = new FakeGroupManager();
        var hub = Construir(repo, Usuario(sub: null, rol: "GestionarPartidas"), groups);

        await hub.SuscribirAPartida(partidaId);

        Assert.Contains(("c1", SesionRealtimeMessages.GrupoPartida(partidaId)), groups.Added);
    }

    [Fact]
    public async Task Inscrito_se_une_al_grupo()
    {
        var partidaId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();
        var repo = new ISesionPartidaRepositorioFake();
        repo.Inner.Add(SesionDe(partidaId, participanteId));
        var groups = new FakeGroupManager();
        var hub = Construir(repo, Usuario(sub: participanteId.ToString(), rol: "Participante"), groups);

        await hub.SuscribirAPartida(partidaId);

        Assert.Contains(("c1", SesionRealtimeMessages.GrupoPartida(partidaId)), groups.Added);
    }

    [Fact]
    public async Task No_inscrito_lanza_HubException_y_no_une()
    {
        var partidaId = Guid.NewGuid();
        var repo = new ISesionPartidaRepositorioFake(); // repo vacío => GetByParticipanteActivoAsync null
        var groups = new FakeGroupManager();
        var hub = Construir(repo, Usuario(sub: Guid.NewGuid().ToString(), rol: "Participante"), groups);

        await Assert.ThrowsAsync<HubException>(() => hub.SuscribirAPartida(partidaId));
        Assert.Empty(groups.Added);
    }

    [Fact]
    public async Task Sin_sub_lanza_HubException()
    {
        var repo = new ISesionPartidaRepositorioFake();
        var groups = new FakeGroupManager();
        var hub = Construir(repo, Usuario(sub: null, rol: "Participante"), groups);

        await Assert.ThrowsAsync<HubException>(() => hub.SuscribirAPartida(Guid.NewGuid()));
    }

    [Fact]
    public async Task Desuscribir_quita_del_grupo()
    {
        var partidaId = Guid.NewGuid();
        var repo = new ISesionPartidaRepositorioFake();
        var groups = new FakeGroupManager();
        var hub = Construir(repo, Usuario(sub: Guid.NewGuid().ToString(), rol: "Operador"), groups);

        await hub.DesuscribirDePartida(partidaId);

        Assert.Contains(("c1", SesionRealtimeMessages.GrupoPartida(partidaId)), groups.Removed);
    }

    [Fact]
    public async Task Inscrito_en_otra_partida_lanza_HubException()
    {
        var partidaA = Guid.NewGuid();
        var partidaB = Guid.NewGuid();
        var participanteId = Guid.NewGuid();
        var repo = new ISesionPartidaRepositorioFake();
        repo.Inner.Add(SesionDe(partidaA, participanteId));
        var groups = new FakeGroupManager();
        var hub = Construir(repo, Usuario(sub: participanteId.ToString(), rol: "Participante"), groups);

        await Assert.ThrowsAsync<HubException>(() => hub.SuscribirAPartida(partidaB));
        Assert.Empty(groups.Added);
    }

    [Fact]
    public async Task Operador_tambien_se_une_al_grupo_operador()
    {
        var partidaId = Guid.NewGuid();
        var repo = new ISesionPartidaRepositorioFake();
        var groups = new FakeGroupManager();
        var hub = Construir(repo, Usuario(sub: null, rol: "Operador"), groups);

        await hub.SuscribirAPartida(partidaId);

        Assert.Contains(("c1", SesionRealtimeMessages.GrupoPartida(partidaId)), groups.Added);
        Assert.Contains(("c1", SesionRealtimeMessages.GrupoOperadorPartida(partidaId)), groups.Added);
    }

    [Fact]
    public async Task Inscrito_no_se_une_al_grupo_operador()
    {
        var partidaId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();
        var repo = new ISesionPartidaRepositorioFake();
        repo.Inner.Add(SesionDe(partidaId, participanteId));
        var groups = new FakeGroupManager();
        var hub = Construir(repo, Usuario(sub: participanteId.ToString(), rol: "Participante"), groups);

        await hub.SuscribirAPartida(partidaId);

        Assert.Contains(("c1", SesionRealtimeMessages.GrupoPartida(partidaId)), groups.Added);
        Assert.DoesNotContain(("c1", SesionRealtimeMessages.GrupoOperadorPartida(partidaId)), groups.Added);
    }

    [Fact]
    public async Task EnviarUbicacion_difunde_al_grupo_operador_con_payload()
    {
        var partidaId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();
        var repo = new ISesionPartidaRepositorioFake();
        repo.Inner.Add(SesionDe(partidaId, participanteId));
        var groups = new FakeGroupManager();
        var clients = new FakeClients();
        var hub = Construir(repo, Usuario(sub: participanteId.ToString(), rol: "Participante"), groups, clients);
        await hub.SuscribirAPartida(partidaId);

        await hub.EnviarUbicacion(10.5, -66.9);

        var proxy = clients.Grupos[SesionRealtimeMessages.GrupoOperadorPartida(partidaId)];
        var (metodo, args) = Assert.Single(proxy.Sent);
        Assert.Equal(SesionRealtimeMessages.UbicacionActualizada, metodo);
        var payload = Assert.IsType<UbicacionParticipantePayload>(args[0]);
        Assert.Equal(partidaId, payload.PartidaId);
        Assert.Equal(participanteId, payload.ParticipanteId);
        Assert.Equal(10.5, payload.Latitud);
        Assert.Equal(-66.9, payload.Longitud);
        Assert.Equal(new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc), payload.TimestampUtc);
    }

    [Fact]
    public async Task EnviarUbicacion_dispara_el_seam_de_eventos_ademas_del_relay()
    {
        var partidaId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();
        var repo = new ISesionPartidaRepositorioFake();
        repo.Inner.Add(SesionDe(partidaId, participanteId));
        var groups = new FakeGroupManager();
        var eventos = new FakeSesionEventsPublisher();
        var hub = Construir(repo, Usuario(sub: participanteId.ToString(), rol: "Participante"), groups, eventos: eventos);
        await hub.SuscribirAPartida(partidaId);

        await hub.EnviarUbicacion(10.5, -66.9);

        var evento = Assert.Single(eventos.UbicacionesActualizadas);
        Assert.Equal(partidaId, evento.PartidaId);
        Assert.Equal(participanteId, evento.ParticipanteId);
        Assert.Equal(10.5, evento.Latitud);
        Assert.Equal(-66.9, evento.Longitud);
        Assert.Equal(T0, evento.Instante);
    }

    [Theory]
    [InlineData(91, 0)]
    [InlineData(-91, 0)]
    [InlineData(0, 181)]
    [InlineData(0, -181)]
    public async Task EnviarUbicacion_coordenadas_fuera_de_rango_lanza_y_no_difunde(double lat, double lng)
    {
        var partidaId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();
        var repo = new ISesionPartidaRepositorioFake();
        repo.Inner.Add(SesionDe(partidaId, participanteId));
        var groups = new FakeGroupManager();
        var clients = new FakeClients();
        var hub = Construir(repo, Usuario(sub: participanteId.ToString(), rol: "Participante"), groups, clients);
        await hub.SuscribirAPartida(partidaId);

        await Assert.ThrowsAsync<HubException>(() => hub.EnviarUbicacion(lat, lng));
        Assert.Empty(clients.Grupos);
    }

    [Fact]
    public async Task EnviarUbicacion_sin_suscripcion_lanza()
    {
        var repo = new ISesionPartidaRepositorioFake();
        var groups = new FakeGroupManager();
        var clients = new FakeClients();
        var hub = Construir(repo, Usuario(sub: Guid.NewGuid().ToString(), rol: "Participante"), groups, clients);

        await Assert.ThrowsAsync<HubException>(() => hub.EnviarUbicacion(1.0, 1.0));
        Assert.Empty(clients.Grupos);
    }

    [Fact]
    public async Task EnviarUbicacion_operador_lanza()
    {
        var partidaId = Guid.NewGuid();
        var repo = new ISesionPartidaRepositorioFake();
        var groups = new FakeGroupManager();
        var clients = new FakeClients();
        var hub = Construir(repo, Usuario(sub: null, rol: "Operador"), groups, clients);
        await hub.SuscribirAPartida(partidaId);

        await Assert.ThrowsAsync<HubException>(() => hub.EnviarUbicacion(1.0, 1.0));
        Assert.Empty(clients.Grupos);
    }

    [Fact]
    public async Task Inscrito_se_une_al_grupo_participante()
    {
        var partidaId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();
        var repo = new ISesionPartidaRepositorioFake();
        repo.Inner.Add(SesionDe(partidaId, participanteId));
        var groups = new FakeGroupManager();
        var hub = Construir(repo, Usuario(sub: participanteId.ToString(), rol: "Participante"), groups);

        await hub.SuscribirAPartida(partidaId);

        Assert.Contains(("c1", SesionRealtimeMessages.GrupoParticipante(participanteId)), groups.Added);
    }

    [Fact]
    public async Task Operador_no_se_une_a_ningun_grupo_participante()
    {
        var partidaId = Guid.NewGuid();
        var repo = new ISesionPartidaRepositorioFake();
        var groups = new FakeGroupManager();
        var hub = Construir(repo, Usuario(sub: null, rol: "Operador"), groups);

        await hub.SuscribirAPartida(partidaId);

        Assert.DoesNotContain(groups.Added, x => x.Group.StartsWith("participante:"));
    }

    // Desuscribir_quita_al_participante_de_su_grupo se retiró aquí: afirmaba que salir de una partida
    // saca del canal personal, y eso es justo lo que este slice deroga (el canal es de la identidad,
    // no de la partida). Lo sustituye Desuscribir_de_partida_no_saca_del_canal_personal, que además
    // comprueba que del grupo de la partida sí se sale.

    [Fact]
    public async Task Convocado_aceptado_se_une_al_grupo_de_su_equipo()
    {
        var partidaId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();
        var repo = new ISesionPartidaRepositorioFake();
        repo.Inner.Add(SesionEquipoDe(partidaId, participanteId, out var equipoId));
        var groups = new FakeGroupManager();
        var hub = Construir(repo, Usuario(sub: participanteId.ToString(), rol: "Participante"), groups);

        await hub.SuscribirAPartida(partidaId);

        Assert.Contains(("c1", SesionRealtimeMessages.GrupoEquipo(equipoId)), groups.Added);
    }

    [Fact]
    public async Task Desuscribir_remueve_el_grupo_de_equipo()
    {
        var partidaId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();
        var repo = new ISesionPartidaRepositorioFake();
        repo.Inner.Add(SesionEquipoDe(partidaId, participanteId, out var equipoId));
        var groups = new FakeGroupManager();
        var hub = Construir(repo, Usuario(sub: participanteId.ToString(), rol: "Participante"), groups);
        await hub.SuscribirAPartida(partidaId);

        await hub.DesuscribirDePartida(partidaId);

        Assert.Contains(("c1", SesionRealtimeMessages.GrupoEquipo(equipoId)), groups.Removed);
    }

    [Fact]
    public async Task Inscrito_individual_no_se_une_a_grupo_de_equipo()
    {
        var partidaId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();
        var repo = new ISesionPartidaRepositorioFake();
        repo.Inner.Add(SesionDe(partidaId, participanteId));
        var groups = new FakeGroupManager();
        var hub = Construir(repo, Usuario(sub: participanteId.ToString(), rol: "Participante"), groups);

        await hub.SuscribirAPartida(partidaId);

        Assert.DoesNotContain(groups.Added, g => g.Group.StartsWith("equipo:"));
    }

    [Fact]
    public async Task Al_conectar_reemite_las_convocatorias_pendientes_al_caller()
    {
        var usuario = Guid.NewGuid();
        var primeraConvocatoria = new ConvocatoriaPendienteDto(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), T0, "Copa");
        var segundaConvocatoria = new ConvocatoriaPendienteDto(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), T0, "Liga");
        var pendientes = new[] { primeraConvocatoria, segundaConvocatoria };
        var clients = new FakeClients();
        var sender = new SenderDeConvocatorias(pendientes);
        var hub = Construir(new ISesionPartidaRepositorioFake(), Usuario(sub: usuario.ToString(), rol: "Participante"),
            new FakeGroupManager(), clients, sender: sender);

        await hub.OnConnectedAsync();

        Assert.Equal(2, clients.MensajesAlCaller.Count(m => m.Metodo == SesionRealtimeMessages.ConvocatoriaCreada));
        Assert.Equal(1, sender.Invocaciones);

        var primerMensaje = clients.MensajesAlCaller.First(m => m.Metodo == SesionRealtimeMessages.ConvocatoriaCreada);
        var primerPayload = Assert.IsType<ConvocatoriaCreadaPayload>(primerMensaje.Args[0]);
        Assert.Equal(primeraConvocatoria.PartidaId, primerPayload.PartidaId);
        Assert.Equal(primeraConvocatoria.EquipoId, primerPayload.EquipoId);
        Assert.Equal(primeraConvocatoria.ConvocatoriaId, primerPayload.ConvocatoriaId);
        Assert.Equal(usuario, primerPayload.UsuarioId);
    }

    [Fact]
    public async Task Al_conectar_sin_pendientes_no_emite_nada()
    {
        var clients = new FakeClients();
        var hub = Construir(new ISesionPartidaRepositorioFake(), Usuario(sub: Guid.NewGuid().ToString(), rol: "Participante"),
            new FakeGroupManager(), clients, sender: new SenderDeConvocatorias(Array.Empty<ConvocatoriaPendienteDto>()));

        await hub.OnConnectedAsync();

        Assert.Empty(clients.MensajesAlCaller);
    }

    [Fact]
    public async Task Operador_al_conectar_no_dispara_query_ni_mensajes()
    {
        var clients = new FakeClients();
        var sender = new SenderDeConvocatorias(Array.Empty<ConvocatoriaPendienteDto>()) { Lanza = true };
        var hub = Construir(new ISesionPartidaRepositorioFake(), Usuario(sub: Guid.NewGuid().ToString(), rol: "Operador"),
            new FakeGroupManager(), clients, sender: sender);

        await hub.OnConnectedAsync(); // si consultara, SenderDeConvocatorias lanzaría

        Assert.Empty(clients.MensajesAlCaller);
        Assert.Equal(0, sender.Invocaciones);
    }

    [Fact]
    public async Task Fallo_de_la_query_no_tumba_la_conexion()
    {
        var sender = new SenderDeConvocatorias(Array.Empty<ConvocatoriaPendienteDto>()) { Lanza = true };
        var hub = Construir(new ISesionPartidaRepositorioFake(), Usuario(sub: Guid.NewGuid().ToString(), rol: "Participante"),
            new FakeGroupManager(), new FakeClients(), sender: sender);

        var ex = await Record.ExceptionAsync(() => hub.OnConnectedAsync());

        Assert.Null(ex);
    }

    // ---- Fakes locales ----

    private sealed class SenderDeConvocatorias : ISender
    {
        private readonly IReadOnlyList<ConvocatoriaPendienteDto> _pendientes;
        public bool Lanza { get; init; }
        public int Invocaciones { get; private set; }
        public SenderDeConvocatorias(IReadOnlyList<ConvocatoriaPendienteDto> pendientes)
            => _pendientes = pendientes;

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            Invocaciones++;
            if (Lanza) throw new InvalidOperationException("query rota");
            return Task.FromResult((TResponse)(object)_pendientes);
        }
        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
            => throw new NotSupportedException();
        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class ISesionPartidaRepositorioFake
    {
        public FakeSesionPartidaRepository Inner { get; } = new();
        public Umbral.OperacionesSesion.Domain.Abstractions.Persistence.ISesionPartidaRepository Repo => Inner;
    }

    private sealed class FakeGroupManager : IGroupManager
    {
        public List<(string Conn, string Group)> Added { get; } = new();
        public List<(string Conn, string Group)> Removed { get; } = new();
        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        { Added.Add((connectionId, groupName)); return Task.CompletedTask; }
        public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        { Removed.Add((connectionId, groupName)); return Task.CompletedTask; }
    }

    private sealed class FakeHubCallerContext : HubCallerContext
    {
        private readonly ClaimsPrincipal _user;
        private readonly string _connId;
        private readonly Dictionary<object, object?> _items = new();
        public FakeHubCallerContext(ClaimsPrincipal user, string connId) { _user = user; _connId = connId; }
        public override string ConnectionId => _connId;
        public override string? UserIdentifier => null;
        public override ClaimsPrincipal? User => _user;
        public override IDictionary<object, object?> Items => _items;
        public override Microsoft.AspNetCore.Http.Features.IFeatureCollection Features => throw new NotImplementedException();
        public override CancellationToken ConnectionAborted => CancellationToken.None;
        public override void Abort() => throw new NotImplementedException();
    }

    private sealed class FakeClientProxy : IClientProxy
    {
        public List<(string Method, object?[] Args)> Sent { get; } = new();
        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
        { Sent.Add((method, args)); return Task.CompletedTask; }
    }

    private sealed class FakeClients : IHubCallerClients
    {
        public Dictionary<string, FakeClientProxy> Grupos { get; } = new();
        public List<(string Metodo, object?[] Args)> MensajesAlCaller { get; } = new();
        public IClientProxy Group(string groupName)
        {
            if (!Grupos.TryGetValue(groupName, out var p)) { p = new FakeClientProxy(); Grupos[groupName] = p; }
            return p;
        }
        public IClientProxy All => throw new NotImplementedException();
        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => throw new NotImplementedException();
        public IClientProxy Client(string connectionId) => throw new NotImplementedException();
        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => throw new NotImplementedException();
        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => throw new NotImplementedException();
        public IClientProxy Groups(IReadOnlyList<string> groupNames) => throw new NotImplementedException();
        public IClientProxy OthersInGroup(string groupName) => throw new NotImplementedException();
        public IClientProxy User(string userId) => throw new NotImplementedException();
        public IClientProxy Users(IReadOnlyList<string> userIds) => throw new NotImplementedException();
        public IClientProxy Caller => new FakeCallerProxy(MensajesAlCaller);
        public IClientProxy Others => throw new NotImplementedException();

        private sealed class FakeCallerProxy : IClientProxy
        {
            private readonly List<(string Metodo, object?[] Args)> _mensajes;
            public FakeCallerProxy(List<(string Metodo, object?[] Args)> mensajes) => _mensajes = mensajes;
            public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
            { _mensajes.Add((method, args)); return Task.CompletedTask; }
        }
    }
}
