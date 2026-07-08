using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Application.Handlers.Commands;
using Umbral.IdentityService.Application.Interfaces;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Domain.Enums;

namespace Umbral.IdentityService.UnitTests;

public class ActualizarPermisosRolHandlerTests
{
    private sealed class RepoFake : IPermisosRolRepository
    {
        public readonly Dictionary<RolUsuario, List<PermisoFuncional>> Datos = new();
        public bool EscrituraRecibida;

        public Task<IReadOnlyDictionary<RolUsuario, IReadOnlyList<PermisoFuncional>>> GetMatrizAsync(CancellationToken ct)
            => Task.FromResult<IReadOnlyDictionary<RolUsuario, IReadOnlyList<PermisoFuncional>>>(
                Enum.GetValues<RolUsuario>().ToDictionary(r => r,
                    r => (IReadOnlyList<PermisoFuncional>)(Datos.TryGetValue(r, out var p) ? p.OrderBy(x => x).ToList() : new List<PermisoFuncional>())));

        public Task<IReadOnlyList<PermisoFuncional>> GetByRolAsync(RolUsuario rol, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<PermisoFuncional>>(Datos.TryGetValue(rol, out var p) ? p.OrderBy(x => x).ToList() : new List<PermisoFuncional>());

        public Task ReplaceForRolAsync(RolUsuario rol, IReadOnlyCollection<PermisoFuncional> permisos, CancellationToken ct)
        {
            EscrituraRecibida = true;
            Datos[rol] = permisos.Distinct().ToList();
            return Task.CompletedTask;
        }
    }

    private sealed class KeycloakFake : IKeycloakIdentityPort
    {
        public readonly List<(string Rol, string Permiso)> CompositesAgregados = new();
        public readonly List<(string Rol, string Permiso)> CompositesQuitados = new();
        public Exception? Lanzar;

        public Task<string> CreateUserWithInitialRoleAsync(string name, string email, string initialRole, string temporaryPassword, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task DeleteUserAsync(string keycloakId, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<bool> HasTemporaryPasswordAsync(string keycloakId, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task UpdateEmailAsync(string keycloakId, string email, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task ResetTemporaryPasswordAsync(string keycloakId, string temporaryPassword, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task AddCompositeToRoleAsync(string roleName, string compositeRoleName, CancellationToken cancellationToken)
        {
            if (Lanzar is not null) throw Lanzar;
            CompositesAgregados.Add((roleName, compositeRoleName));
            return Task.CompletedTask;
        }

        public Task RemoveCompositeFromRoleAsync(string roleName, string compositeRoleName, CancellationToken cancellationToken)
        {
            if (Lanzar is not null) throw Lanzar;
            CompositesQuitados.Add((roleName, compositeRoleName));
            return Task.CompletedTask;
        }

        public Task ChangeUserRealmRoleAsync(string keycloakId, string oldRoleName, string newRoleName, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class PublisherFake : IIdentityEventsPublisher
    {
        public readonly List<PermisosRolActualizadosIntegrationEvent> Eventos = new();
        public Task PublishPermisosRolActualizadosAsync(PermisosRolActualizadosIntegrationEvent e, CancellationToken ct)
        { Eventos.Add(e); return Task.CompletedTask; }
        public Task PublishEquipoCreadoAsync(EquipoCreadoIntegrationEvent e, CancellationToken ct) => Task.CompletedTask;
        public Task PublishInvitacionEquipoCreadaAsync(InvitacionEquipoCreadaIntegrationEvent e, CancellationToken ct) => Task.CompletedTask;
        public Task PublishInvitacionEquipoAceptadaAsync(InvitacionEquipoAceptadaIntegrationEvent e, CancellationToken ct) => Task.CompletedTask;
        public Task PublishInvitacionEquipoRechazadaAsync(InvitacionEquipoRechazadaIntegrationEvent e, CancellationToken ct) => Task.CompletedTask;
        public Task PublishRolUsuarioModificadoAsync(RolUsuarioModificadoIntegrationEvent e, CancellationToken ct) => Task.CompletedTask;
    }

    private static (ActualizarPermisosRolCommandHandler Handler, RepoFake Repo, KeycloakFake Kc, PublisherFake Pub) Crear()
    {
        var repo = new RepoFake();
        var kc = new KeycloakFake();
        var pub = new PublisherFake();
        return (new ActualizarPermisosRolCommandHandler(repo, kc, pub, TimeProvider.System), repo, kc, pub);
    }

    [Fact]
    public async Task Diff_mixto_aplica_solo_agregados_y_quitados()
    {
        var (handler, repo, kc, pub) = Crear();
        repo.Datos[RolUsuario.Operador] = new List<PermisoFuncional> { PermisoFuncional.GestionarPartidas };

        var result = await handler.Handle(
            new ActualizarPermisosRolCommand("Operador", new[] { "GestionarEquipos" }), CancellationToken.None);

        Assert.Equal(new[] { ("Operador", "GestionarEquipos") }, kc.CompositesAgregados);
        Assert.Equal(new[] { ("Operador", "GestionarPartidas") }, kc.CompositesQuitados);
        Assert.Equal(new List<PermisoFuncional> { PermisoFuncional.GestionarEquipos }, repo.Datos[RolUsuario.Operador]);
        Assert.Equal("Operador", result.Rol);
        Assert.Equal(new[] { "GestionarEquipos" }, result.Permisos);
        var evento = Assert.Single(pub.Eventos);
        Assert.Equal(new[] { "GestionarEquipos" }, evento.Permisos);
    }

    [Fact]
    public async Task Diff_vacio_no_toca_keycloak_ni_db_ni_evento()
    {
        var (handler, repo, kc, pub) = Crear();
        repo.Datos[RolUsuario.Participante] = new List<PermisoFuncional> { PermisoFuncional.GestionarEquipos, PermisoFuncional.ParticiparEnPartidas };

        await handler.Handle(new ActualizarPermisosRolCommand("Participante",
            new[] { "ParticiparEnPartidas", "GestionarEquipos" }), CancellationToken.None);

        Assert.Empty(kc.CompositesAgregados);
        Assert.Empty(kc.CompositesQuitados);
        Assert.False(repo.EscrituraRecibida);
        Assert.Empty(pub.Eventos);
    }

    [Fact]
    public async Task Fallo_de_keycloak_no_persiste_en_db_ni_emite_evento()
    {
        var (handler, repo, kc, pub) = Crear();
        kc.Lanzar = new Umbral.IdentityService.Application.Exceptions.KeycloakIntegrationException("down");

        await Assert.ThrowsAsync<Umbral.IdentityService.Application.Exceptions.KeycloakIntegrationException>(
            () => handler.Handle(new ActualizarPermisosRolCommand("Operador", new[] { "GestionarEquipos" }), CancellationToken.None));

        Assert.False(repo.EscrituraRecibida);
        Assert.Empty(pub.Eventos);
    }

    [Fact]
    public async Task Duplicados_en_el_body_se_normalizan()
    {
        var (handler, repo, kc, _) = Crear();

        var result = await handler.Handle(new ActualizarPermisosRolCommand("Administrador",
            new[] { "GestionarPartidas", "GestionarPartidas" }), CancellationToken.None);

        Assert.Single(kc.CompositesAgregados);
        Assert.Equal(new[] { "GestionarPartidas" }, result.Permisos);
    }
}
