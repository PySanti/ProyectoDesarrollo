using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Umbral.OperacionesSesion.Api.Realtime;
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
        s.Inscribir(participanteId, false, 0, new DateTime(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc));
        return s;
    }

    private static readonly DateTime T0 = new(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc);

    private static SesionHub Construir(ISesionPartidaRepositorioFake repo, ClaimsPrincipal user,
        FakeGroupManager groups, FakeClients? clients = null, string connId = "c1")
    {
        var hub = new SesionHub(repo.Repo, new FakeTimeProvider(T0))
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
        var ins = s.PreinscribirEquipo(equipoLocal, true, new[] { participanteId }, false, 0, t0);
        s.ResponderConvocatoria(ins.Convocatorias.Single().Id.Valor, participanteId, true, false, t0);
        equipoId = equipoLocal;
        return s;
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

    [Fact]
    public async Task Desuscribir_quita_al_participante_de_su_grupo()
    {
        var partidaId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();
        var repo = new ISesionPartidaRepositorioFake();
        repo.Inner.Add(SesionDe(partidaId, participanteId));
        var groups = new FakeGroupManager();
        var hub = Construir(repo, Usuario(sub: participanteId.ToString(), rol: "Participante"), groups);

        await hub.SuscribirAPartida(partidaId); // puebla Context.Items[participanteId]
        await hub.DesuscribirDePartida(partidaId);

        Assert.Contains(("c1", SesionRealtimeMessages.GrupoParticipante(participanteId)), groups.Removed);
    }

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

        Assert.DoesNotContain(groups.Added, g => g.Item2.StartsWith("equipo:"));
    }

    // ---- Fakes locales ----

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
        public IClientProxy Caller => throw new NotImplementedException();
        public IClientProxy Others => throw new NotImplementedException();
    }
}
