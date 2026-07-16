using Umbral.IdentityService.Domain.ValueObjects;
using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Application.Exceptions;
using Umbral.IdentityService.Application.Handlers.Commands;
using Umbral.IdentityService.Application.Interfaces;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Domain.Entities;
using Umbral.IdentityService.Domain.Enums;
using Umbral.IdentityService.Domain.Exceptions;

namespace Umbral.IdentityService.UnitTests;

public class CambiarRolUsuarioHandlerTests
{
    private sealed class UsuarioRepositoryFake : IUsuarioRepository
    {
        private readonly Dictionary<UsuarioLocalId, Usuario> _usuarios = new();
        public bool UpdateRecibido;

        public void Agregar(Usuario usuario) => _usuarios[usuario.UsuarioId] = usuario;

        public Task<IReadOnlyList<Usuario>> GetAllAsync(CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<Usuario?> GetByIdAsync(UsuarioLocalId userId, CancellationToken cancellationToken)
            => Task.FromResult(_usuarios.TryGetValue(userId, out var usuario) ? usuario : null);

        public Task<Usuario?> GetByKeycloakIdAsync(Guid keycloakId, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<bool> ExistsByEmailAsync(string email, UsuarioLocalId? excludingUserId, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task AddAsync(Usuario usuario, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task UpdateAsync(Usuario usuario, CancellationToken cancellationToken)
        {
            UpdateRecibido = true;
            _usuarios[usuario.UsuarioId] = usuario;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(Usuario usuario, CancellationToken cancellationToken)
            => throw new NotImplementedException();
    }

    private sealed class EquipoRepositoryFake : IEquipoRepository
    {
        public bool Existe;

        public Task<bool> ExistsActiveTeamByUserIdAsync(Guid userId, CancellationToken cancellationToken)
            => Task.FromResult(Existe);

        public Task<Equipo?> GetActiveByMemberUserIdAsync(Guid userId, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<Equipo?> GetByIdAsync(Guid equipoId, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<IReadOnlyList<Equipo>> GetAllAsync(CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task AddAsync(Equipo equipo, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task UpdateAsync(Equipo equipo, CancellationToken cancellationToken)
            => throw new NotImplementedException();
    }

    private sealed class KeycloakFake : IKeycloakIdentityPort
    {
        public readonly List<(string KeycloakId, string Viejo, string Nuevo)> CambiosDeRol = new();
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
            => throw new NotImplementedException();

        public Task RemoveCompositeFromRoleAsync(string roleName, string compositeRoleName, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task ChangeUserRealmRoleAsync(string keycloakId, string oldRoleName, string newRoleName, CancellationToken cancellationToken)
        {
            if (Lanzar is not null) throw Lanzar;
            CambiosDeRol.Add((keycloakId, oldRoleName, newRoleName));
            return Task.CompletedTask;
        }
    }

    private sealed class PublisherFake : IIdentityEventsPublisher
    {
        public readonly List<RolUsuarioModificadoIntegrationEvent> Eventos = new();

        public Task PublishRolUsuarioModificadoAsync(RolUsuarioModificadoIntegrationEvent e, CancellationToken ct)
        { Eventos.Add(e); return Task.CompletedTask; }

        public Task PublishEquipoCreadoAsync(EquipoCreadoIntegrationEvent e, CancellationToken ct) => Task.CompletedTask;
        public Task PublishInvitacionEquipoCreadaAsync(InvitacionEquipoCreadaIntegrationEvent e, CancellationToken ct) => Task.CompletedTask;
        public Task PublishInvitacionEquipoAceptadaAsync(InvitacionEquipoAceptadaIntegrationEvent e, CancellationToken ct) => Task.CompletedTask;
        public Task PublishInvitacionEquipoRechazadaAsync(InvitacionEquipoRechazadaIntegrationEvent e, CancellationToken ct) => Task.CompletedTask;
        public Task PublishPermisosRolActualizadosAsync(PermisosRolActualizadosIntegrationEvent e, CancellationToken ct) => Task.CompletedTask;
        public Task PublishEquipoEliminadoAsync(EquipoEliminadoIntegrationEvent e, CancellationToken ct) => Task.CompletedTask;
        public Task PublishLiderazgoEquipoModificadoAsync(LiderazgoEquipoModificadoIntegrationEvent e, CancellationToken ct) => Task.CompletedTask;
        public Task PublishEquipoDesactivadoAsync(EquipoDesactivadoIntegrationEvent e, CancellationToken ct) => Task.CompletedTask;
        public Task PublishEquipoReactivadoAsync(EquipoReactivadoIntegrationEvent e, CancellationToken ct) => Task.CompletedTask;
        public Task PublishCredencialTemporalEmitidaAsync(CredencialTemporalEmitidaIntegrationEvent e, CancellationToken ct) => Task.CompletedTask;
    }

    private static (CambiarRolUsuarioCommandHandler Handler, UsuarioRepositoryFake Usuarios, EquipoRepositoryFake Equipos, KeycloakFake Kc, PublisherFake Pub) Crear()
    {
        var usuarios = new UsuarioRepositoryFake();
        var equipos = new EquipoRepositoryFake();
        var kc = new KeycloakFake();
        var pub = new PublisherFake();
        return (new CambiarRolUsuarioCommandHandler(usuarios, equipos, kc, pub, TimeProvider.System), usuarios, equipos, kc, pub);
    }

    private static Usuario CrearUsuario(RolUsuario rol, string? keycloakId = null)
        => Usuario.Crear(keycloakId ?? Guid.NewGuid().ToString(), "Ana", "ana@x.com", rol);

    [Fact]
    public async Task Usuario_inexistente_lanza_UserNotFoundException()
    {
        var (handler, _, _, kc, pub) = Crear();

        await Assert.ThrowsAsync<UserNotFoundException>(
            () => handler.Handle(new CambiarRolUsuarioCommand(Guid.NewGuid(), "Operador"), CancellationToken.None));

        Assert.Empty(kc.CambiosDeRol);
        Assert.Empty(pub.Eventos);
    }

    [Fact]
    public async Task Target_administrador_lanza_RolDeAdministradorInmutableException_sin_tocar_keycloak_ni_repo()
    {
        var (handler, usuarios, _, kc, pub) = Crear();
        var admin = CrearUsuario(RolUsuario.Administrador);
        usuarios.Agregar(admin);

        await Assert.ThrowsAsync<RolDeAdministradorInmutableException>(
            () => handler.Handle(new CambiarRolUsuarioCommand(admin.UsuarioId.Valor, "Operador"), CancellationToken.None));

        Assert.Empty(kc.CambiosDeRol);
        Assert.False(usuarios.UpdateRecibido);
        Assert.Empty(pub.Eventos);
    }

    [Fact]
    public async Task Mismo_rol_es_no_op_y_devuelve_rol_actual()
    {
        var (handler, usuarios, _, kc, pub) = Crear();
        var operador = CrearUsuario(RolUsuario.Operador);
        usuarios.Agregar(operador);

        var response = await handler.Handle(new CambiarRolUsuarioCommand(operador.UsuarioId.Valor, "Operador"), CancellationToken.None);

        Assert.Equal(operador.UsuarioId.Valor, response.UsuarioId);
        Assert.Equal("Operador", response.Rol);
        Assert.Empty(kc.CambiosDeRol);
        Assert.False(usuarios.UpdateRecibido);
        Assert.Empty(pub.Eventos);
    }

    [Fact]
    public async Task Participante_con_equipo_activo_lanza_UsuarioConEquipoActivoException_sin_tocar_keycloak()
    {
        var (handler, usuarios, equipos, kc, _) = Crear();
        var participante = CrearUsuario(RolUsuario.Participante);
        usuarios.Agregar(participante);
        equipos.Existe = true;

        await Assert.ThrowsAsync<UsuarioConEquipoActivoException>(
            () => handler.Handle(new CambiarRolUsuarioCommand(participante.UsuarioId.Valor, "Operador"), CancellationToken.None));

        Assert.Empty(kc.CambiosDeRol);
    }

    [Fact]
    public async Task Participante_a_operador_feliz_llama_keycloak_muta_y_publica_evento()
    {
        var (handler, usuarios, equipos, kc, pub) = Crear();
        var keycloakId = Guid.NewGuid().ToString();
        var participante = CrearUsuario(RolUsuario.Participante, keycloakId);
        usuarios.Agregar(participante);
        equipos.Existe = false;

        var response = await handler.Handle(new CambiarRolUsuarioCommand(participante.UsuarioId.Valor, "Operador"), CancellationToken.None);

        Assert.Equal(new[] { (keycloakId, "Participante", "Operador") }, kc.CambiosDeRol);
        Assert.True(usuarios.UpdateRecibido);
        Assert.Equal(RolUsuario.Operador, participante.Rol);
        Assert.Equal("Operador", response.Rol);
        var evento = Assert.Single(pub.Eventos);
        Assert.Equal("Participante", evento.RolAnterior);
        Assert.Equal("Operador", evento.RolNuevo);
    }

    [Fact]
    public async Task Fallo_de_keycloak_no_muta_ni_persiste_ni_emite_evento()
    {
        var (handler, usuarios, equipos, kc, pub) = Crear();
        var participante = CrearUsuario(RolUsuario.Participante);
        usuarios.Agregar(participante);
        equipos.Existe = false;
        kc.Lanzar = new KeycloakIntegrationException("down");

        await Assert.ThrowsAsync<KeycloakIntegrationException>(
            () => handler.Handle(new CambiarRolUsuarioCommand(participante.UsuarioId.Valor, "Operador"), CancellationToken.None));

        Assert.False(usuarios.UpdateRecibido);
        Assert.Equal(RolUsuario.Participante, participante.Rol);
        Assert.Empty(pub.Eventos);
    }

    [Fact]
    public async Task Promocion_operador_a_administrador_procede()
    {
        var (handler, usuarios, equipos, kc, pub) = Crear();
        var keycloakId = Guid.NewGuid().ToString();
        var operador = CrearUsuario(RolUsuario.Operador, keycloakId);
        usuarios.Agregar(operador);
        equipos.Existe = false;

        var response = await handler.Handle(new CambiarRolUsuarioCommand(operador.UsuarioId.Valor, "Administrador"), CancellationToken.None);

        Assert.Equal(new[] { (keycloakId, "Operador", "Administrador") }, kc.CambiosDeRol);
        Assert.Equal(RolUsuario.Administrador, operador.Rol);
        Assert.Equal("Administrador", response.Rol);
        var evento = Assert.Single(pub.Eventos);
        Assert.Equal("Operador", evento.RolAnterior);
        Assert.Equal("Administrador", evento.RolNuevo);
    }
}
