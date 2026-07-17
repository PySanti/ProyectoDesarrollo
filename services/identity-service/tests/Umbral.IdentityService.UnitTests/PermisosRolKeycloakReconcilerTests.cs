using Microsoft.Extensions.Logging.Abstractions;
using Umbral.IdentityService.Application.Exceptions;
using Umbral.IdentityService.Application.Interfaces;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Domain.Enums;
using Umbral.IdentityService.Infrastructure.Services.Identity;

namespace Umbral.IdentityService.UnitTests;

/// <summary>
/// La DB es la fuente de la gobernanza; Keycloak su espejo. keycloak-config reaplica el realm
/// declarativo en cada `up` y revierte los composites asignados desde el panel (HU-04): el
/// reconciliador de arranque repara esa deriva.
/// </summary>
public class PermisosRolKeycloakReconcilerTests
{
    private sealed class RepoFake : IPermisosRolRepository
    {
        public readonly Dictionary<RolUsuario, List<PermisoFuncional>> Datos = new();

        public Task<IReadOnlyDictionary<RolUsuario, IReadOnlyList<PermisoFuncional>>> GetMatrizAsync(CancellationToken ct)
            => Task.FromResult<IReadOnlyDictionary<RolUsuario, IReadOnlyList<PermisoFuncional>>>(
                Enum.GetValues<RolUsuario>().ToDictionary(r => r,
                    r => (IReadOnlyList<PermisoFuncional>)(Datos.TryGetValue(r, out var p)
                        ? p.OrderBy(x => x).ToList()
                        : new List<PermisoFuncional>())));

        public Task<IReadOnlyList<PermisoFuncional>> GetByRolAsync(RolUsuario rol, CancellationToken ct)
            => throw new NotImplementedException();

        public Task ReplaceForRolAsync(RolUsuario rol, IReadOnlyCollection<PermisoFuncional> permisos, CancellationToken ct)
            => throw new NotImplementedException();
    }

    private sealed class KeycloakFake : IKeycloakIdentityPort
    {
        public readonly List<(string Rol, string Permiso)> CompositesAgregados = new();
        public readonly List<(string Rol, string Permiso)> CompositesQuitados = new();
        public Exception? Lanzar;

        public Task<string> CreateUserWithInitialRoleAsync(string name, string email, string initialRole, string temporaryPassword, CancellationToken ct)
            => throw new NotImplementedException();
        public Task DeleteUserAsync(string keycloakId, CancellationToken ct) => throw new NotImplementedException();
        public Task<bool> HasTemporaryPasswordAsync(string keycloakId, CancellationToken ct) => throw new NotImplementedException();
        public Task SyncUserProfileAsync(string keycloakId, string nombre, string correo, CancellationToken ct) => throw new NotImplementedException();
        public Task ResetTemporaryPasswordAsync(string keycloakId, string temporaryPassword, CancellationToken ct) => throw new NotImplementedException();
        public Task ChangeUserRealmRoleAsync(string keycloakId, string oldRoleName, string newRoleName, CancellationToken ct) => throw new NotImplementedException();

        public Task AddCompositeToRoleAsync(string roleName, string compositeRoleName, CancellationToken ct)
        {
            if (Lanzar is not null) throw Lanzar;
            CompositesAgregados.Add((roleName, compositeRoleName));
            return Task.CompletedTask;
        }

        public Task RemoveCompositeFromRoleAsync(string roleName, string compositeRoleName, CancellationToken ct)
        {
            if (Lanzar is not null) throw Lanzar;
            CompositesQuitados.Add((roleName, compositeRoleName));
            return Task.CompletedTask;
        }
    }

    private static (PermisosRolKeycloakReconciler Reconciler, RepoFake Repo, KeycloakFake Kc) Crear()
    {
        var repo = new RepoFake();
        var kc = new KeycloakFake();
        return (new PermisosRolKeycloakReconciler(repo, kc, NullLogger<PermisosRolKeycloakReconciler>.Instance), repo, kc);
    }

    [Fact]
    public async Task Reaplica_en_Keycloak_los_permisos_que_la_DB_declara()
    {
        var (reconciler, repo, kc) = Crear();
        // Escenario real: el panel dio GestionarPartidas al Administrador y keycloak-config lo borro.
        repo.Datos[RolUsuario.Administrador] = new List<PermisoFuncional> { PermisoFuncional.GestionarPartidas };

        await reconciler.ReconcileAsync(CancellationToken.None);

        Assert.Contains(("Administrador", "GestionarPartidas"), kc.CompositesAgregados);
    }

    [Fact]
    public async Task Quita_de_Keycloak_los_permisos_que_la_DB_no_declara()
    {
        var (reconciler, repo, kc) = Crear();
        repo.Datos[RolUsuario.Operador] = new List<PermisoFuncional> { PermisoFuncional.GestionarPartidas };

        await reconciler.ReconcileAsync(CancellationToken.None);

        Assert.Contains(("Operador", "GestionarPartidas"), kc.CompositesAgregados);
        Assert.Contains(("Operador", "GestionarEquipos"), kc.CompositesQuitados);
        // Un permiso nunca se agrega y se quita a la vez: el estado afirmado es exacto.
        Assert.DoesNotContain(("Operador", "GestionarPartidas"), kc.CompositesQuitados);
        // ParticiparEnPartidas es fijo del realm y el reconciliador no lo toca (ver
        // Nunca_toca_el_composite_fijo_de_ParticiparEnPartidas).
        Assert.DoesNotContain(("Operador", "ParticiparEnPartidas"), kc.CompositesQuitados);
    }

    [Fact]
    public async Task Cubre_los_tres_roles_y_los_dos_permisos_gobernables()
    {
        var (reconciler, _, kc) = Crear();

        await reconciler.ReconcileAsync(CancellationToken.None);

        // Matriz vacía => los 6 pares gobernables se afirman como ausentes. ParticiparEnPartidas no cuenta.
        Assert.Equal(6, kc.CompositesQuitados.Count);
        Assert.Empty(kc.CompositesAgregados);
    }

    [Fact]
    public async Task Keycloak_caido_no_tumba_el_arranque()
    {
        var (reconciler, repo, kc) = Crear();
        repo.Datos[RolUsuario.Administrador] = new List<PermisoFuncional> { PermisoFuncional.GestionarPartidas };
        kc.Lanzar = new KeycloakIntegrationException("down");

        // Best-effort: registra y sigue. Si lanzara, Identity no arrancaria sin Keycloak.
        await reconciler.ReconcileAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Nunca_toca_el_composite_fijo_de_ParticiparEnPartidas()
    {
        var (reconciler, repo, kc) = Crear();
        // La DB solo gobierna los dos privilegios; ParticiparEnPartidas vive fijo en el realm.
        repo.Datos[RolUsuario.Participante] = new List<PermisoFuncional>();

        await reconciler.ReconcileAsync(CancellationToken.None);

        // Si lo quitara, el rol Participante perdería el permiso y el gameplay del móvil caería entero.
        Assert.DoesNotContain(
            kc.CompositesQuitados,
            par => par.Permiso == nameof(PermisoFuncional.ParticiparEnPartidas));
        Assert.DoesNotContain(
            kc.CompositesAgregados,
            par => par.Permiso == nameof(PermisoFuncional.ParticiparEnPartidas));
    }
}
