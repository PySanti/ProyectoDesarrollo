using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Application.DTOs;
using Umbral.IdentityService.Application.Exceptions;
using Umbral.IdentityService.Application.Handlers.Commands;
using Umbral.IdentityService.Application.Interfaces;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Domain.Entities;
using Umbral.IdentityService.Domain.Enums;
using Xunit;

namespace Umbral.IdentityService.UnitTests.Teams;

public sealed class EquipoAdminHandlersTests
{
    // ---------- Crear ----------

    [Fact]
    public async Task Crear_lider_inexistente_lanza_UserNotFoundException()
    {
        var usuarios = new FakeUsuarioRepository();
        var equipos = new FakeEquipoRepository();
        var historial = new FakeHistorialNombreEquipoRepository();
        var publisher = new FakeIdentityEventsPublisher();
        var handler = new CrearEquipoAdminCommandHandler(usuarios, equipos, historial, publisher, TimeProvider.System);

        var liderId = Guid.NewGuid();

        await Assert.ThrowsAsync<UserNotFoundException>(() =>
            handler.Handle(new CrearEquipoAdminCommand("Equipo A", liderId), CancellationToken.None));

        Assert.False(equipos.AddWasCalled);
        Assert.Empty(historial.Registros);
        Assert.False(publisher.EquipoCreadoWasCalled);
    }

    [Fact]
    public async Task Crear_lider_con_equipo_activo_lanza_AlreadyBelongsToActiveTeamException()
    {
        var usuarios = new FakeUsuarioRepository();
        var liderUsuario = Usuario.Crear(Guid.NewGuid().ToString(), "Ana", "ana@x.com", RolUsuario.Participante);
        usuarios.Agregar(liderUsuario);
        var liderId = liderUsuario.UsuarioId;
        var equipos = new FakeEquipoRepository { ExistsActiveTeamByUserIdValue = true };
        var historial = new FakeHistorialNombreEquipoRepository();
        var publisher = new FakeIdentityEventsPublisher();
        var handler = new CrearEquipoAdminCommandHandler(usuarios, equipos, historial, publisher, TimeProvider.System);

        await Assert.ThrowsAsync<AlreadyBelongsToActiveTeamException>(() =>
            handler.Handle(new CrearEquipoAdminCommand("Equipo A", liderId), CancellationToken.None));

        Assert.False(equipos.AddWasCalled);
        Assert.Empty(historial.Registros);
        Assert.False(publisher.EquipoCreadoWasCalled);
    }

    [Fact]
    public async Task Crear_happy_path_crea_equipo_activo_registra_historial_y_publica_evento()
    {
        var usuarios = new FakeUsuarioRepository();
        var liderUsuario = Usuario.Crear(Guid.NewGuid().ToString(), "Ana", "ana@x.com", RolUsuario.Participante);
        usuarios.Agregar(liderUsuario);
        var liderId = liderUsuario.UsuarioId;
        var liderMembershipKey = Guid.Parse(liderUsuario.KeycloakId);
        var equipos = new FakeEquipoRepository { ExistsActiveTeamByUserIdValue = false };
        var historial = new FakeHistorialNombreEquipoRepository();
        var publisher = new FakeIdentityEventsPublisher();
        var handler = new CrearEquipoAdminCommandHandler(usuarios, equipos, historial, publisher, TimeProvider.System);

        var response = await handler.Handle(new CrearEquipoAdminCommand("Equipo A", liderId), CancellationToken.None);

        Assert.Equal("Equipo A", response.NombreEquipo);
        Assert.Equal(EstadoEquipo.Activo.ToString(), response.Estado);
        Assert.Equal(liderMembershipKey, response.LiderUserId);
        Assert.Single(response.Integrantes);
        Assert.True(equipos.AddWasCalled);
        Assert.Single(historial.Registros);
        Assert.Equal(liderMembershipKey, historial.Registros[0].UsuarioId);
        Assert.Equal("Equipo A", historial.Registros[0].NombreEquipo);
        Assert.True(publisher.EquipoCreadoWasCalled);
        Assert.Equal(liderMembershipKey, publisher.EquipoCreadoEvent!.LiderUserId);
    }

    [Fact]
    public async Task Crear_resuelve_clave_de_membresia_del_lider_por_KeycloakId_no_por_UsuarioId_local()
    {
        // El JWT `sub` (y por ende toda clave de membresía/evento/historial de equipos) vive en el
        // espacio de KeycloakId, NO en el espacio de Usuario.UsuarioId local. El admin envía el
        // UsuarioId local (el que expone el directorio de usuarios); el handler debe resolverlo
        // contra el usuario y usar su KeycloakId como clave de membresía.
        var usuarios = new FakeUsuarioRepository();
        var keycloakId = Guid.NewGuid().ToString();
        var liderUsuario = Usuario.Crear(keycloakId, "Beto", "beto@x.com", RolUsuario.Participante);
        usuarios.Agregar(liderUsuario);

        var liderUserIdLocal = liderUsuario.UsuarioId;
        var liderMembershipKey = Guid.Parse(liderUsuario.KeycloakId);
        Assert.NotEqual(liderUserIdLocal, liderMembershipKey);

        var equipos = new FakeEquipoRepository { ExistsActiveTeamByUserIdValue = false };
        var historial = new FakeHistorialNombreEquipoRepository();
        var publisher = new FakeIdentityEventsPublisher();
        var handler = new CrearEquipoAdminCommandHandler(usuarios, equipos, historial, publisher, TimeProvider.System);

        var response = await handler.Handle(new CrearEquipoAdminCommand("Equipo B", liderUserIdLocal), CancellationToken.None);

        Assert.Equal(liderMembershipKey, response.LiderUserId);
        Assert.NotEqual(liderUserIdLocal, response.LiderUserId);

        Assert.Single(historial.Registros);
        Assert.Equal(liderMembershipKey, historial.Registros[0].UsuarioId);
        Assert.NotEqual(liderUserIdLocal, historial.Registros[0].UsuarioId);

        Assert.True(publisher.EquipoCreadoWasCalled);
        Assert.Equal(liderMembershipKey, publisher.EquipoCreadoEvent!.LiderUserId);
        Assert.NotEqual(liderUserIdLocal, publisher.EquipoCreadoEvent.LiderUserId);
    }

    // ---------- Renombrar ----------

    [Fact]
    public async Task Renombrar_equipo_inexistente_lanza_EquipoNoEncontradoException()
    {
        var equipos = new FakeEquipoRepository { TeamToReturn = null };
        var historial = new FakeHistorialNombreEquipoRepository();
        var handler = new RenombrarEquipoAdminCommandHandler(equipos, historial, TimeProvider.System);

        await Assert.ThrowsAsync<EquipoNoEncontradoException>(() =>
            handler.Handle(new RenombrarEquipoAdminCommand(Guid.NewGuid(), "Nuevo Nombre"), CancellationToken.None));

        Assert.Empty(historial.Registros);
    }

    [Fact]
    public async Task Renombrar_happy_path_cambia_nombre_y_registra_historial_por_integrante()
    {
        var lider = Guid.NewGuid();
        var miembro = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Nombre Viejo", lider);
        equipo.AgregarParticipante(miembro);
        var equipos = new FakeEquipoRepository { TeamToReturn = equipo };
        var historial = new FakeHistorialNombreEquipoRepository();
        var handler = new RenombrarEquipoAdminCommandHandler(equipos, historial, TimeProvider.System);

        var response = await handler.Handle(new RenombrarEquipoAdminCommand(equipo.EquipoId, "Nombre Nuevo"), CancellationToken.None);

        Assert.Equal("Nombre Nuevo", response.NombreEquipo);
        Assert.Equal("Nombre Nuevo", equipo.NombreEquipo);
        Assert.True(equipos.UpdateWasCalled);
        Assert.Equal(2, historial.Registros.Count);
        Assert.All(historial.Registros, r => Assert.Equal("Nombre Nuevo", r.NombreEquipo));
        Assert.Contains(historial.Registros, r => r.UsuarioId == lider);
        Assert.Contains(historial.Registros, r => r.UsuarioId == miembro);
    }

    // ---------- Reasignar liderazgo ----------

    [Fact]
    public async Task Reasignar_equipo_inexistente_lanza_EquipoNoEncontradoException()
    {
        var equipos = new FakeEquipoRepository { TeamToReturn = null };
        var publisher = new FakeIdentityEventsPublisher();
        var notifier = new FakeTeamLifecycleNotifier();
        var handler = new ReasignarLiderazgoAdminCommandHandler(equipos, publisher, notifier, TimeProvider.System);

        await Assert.ThrowsAsync<EquipoNoEncontradoException>(() =>
            handler.Handle(new ReasignarLiderazgoAdminCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Reasignar_nuevo_lider_no_integrante_lanza_TransferirLiderazgoConflictException()
    {
        var lider = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", lider);
        var equipos = new FakeEquipoRepository { TeamToReturn = equipo };
        var publisher = new FakeIdentityEventsPublisher();
        var notifier = new FakeTeamLifecycleNotifier();
        var handler = new ReasignarLiderazgoAdminCommandHandler(equipos, publisher, notifier, TimeProvider.System);

        await Assert.ThrowsAsync<TransferirLiderazgoConflictException>(() =>
            handler.Handle(new ReasignarLiderazgoAdminCommand(equipo.EquipoId, Guid.NewGuid()), CancellationToken.None));

        Assert.False(equipos.UpdateWasCalled);
        Assert.False(publisher.LiderazgoModificadoWasCalled);
        Assert.False(notifier.NotificarLiderazgoWasCalled);
    }

    [Fact]
    public async Task Reasignar_happy_path_publica_evento_y_notifica_a_ambos_lideres()
    {
        var lider = Guid.NewGuid();
        var nuevoLider = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", lider);
        equipo.AgregarParticipante(nuevoLider);
        var equipos = new FakeEquipoRepository { TeamToReturn = equipo };
        var publisher = new FakeIdentityEventsPublisher();
        var notifier = new FakeTeamLifecycleNotifier();
        var handler = new ReasignarLiderazgoAdminCommandHandler(equipos, publisher, notifier, TimeProvider.System);

        var response = await handler.Handle(new ReasignarLiderazgoAdminCommand(equipo.EquipoId, nuevoLider), CancellationToken.None);

        Assert.Equal(nuevoLider, response.LiderUserId);
        Assert.True(equipos.UpdateWasCalled);
        Assert.True(publisher.LiderazgoModificadoWasCalled);
        Assert.Equal(lider, publisher.LiderazgoModificadoEvent!.LiderAnteriorUserId);
        Assert.Equal(nuevoLider, publisher.LiderazgoModificadoEvent.NuevoLiderUserId);
        Assert.Equal("Admin", publisher.LiderazgoModificadoEvent.Origen);
        Assert.True(notifier.NotificarLiderazgoWasCalled);
        Assert.Equal(lider, notifier.LiderAnteriorNotificado);
        Assert.Equal(nuevoLider, notifier.NuevoLiderNotificado);
    }

    // ---------- Cambiar estado ----------

    [Fact]
    public async Task CambiarEstado_equipo_inexistente_lanza_EquipoNoEncontradoException()
    {
        var equipos = new FakeEquipoRepository { TeamToReturn = null };
        var publisher = new FakeIdentityEventsPublisher();
        var handler = new CambiarEstadoEquipoAdminCommandHandler(equipos, publisher, TimeProvider.System);

        await Assert.ThrowsAsync<EquipoNoEncontradoException>(() =>
            handler.Handle(new CambiarEstadoEquipoAdminCommand(Guid.NewGuid(), "Desactivado"), CancellationToken.None));
    }

    [Fact]
    public async Task CambiarEstado_a_Desactivado_cambia_estado_y_publica_EquipoDesactivado()
    {
        var lider = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", lider);
        var equipos = new FakeEquipoRepository { TeamToReturn = equipo };
        var publisher = new FakeIdentityEventsPublisher();
        var handler = new CambiarEstadoEquipoAdminCommandHandler(equipos, publisher, TimeProvider.System);

        var response = await handler.Handle(new CambiarEstadoEquipoAdminCommand(equipo.EquipoId, "Desactivado"), CancellationToken.None);

        Assert.Equal(EstadoEquipo.Desactivado.ToString(), response.Estado);
        Assert.Equal(EstadoEquipo.Desactivado, equipo.Estado);
        Assert.True(equipos.UpdateWasCalled);
        Assert.True(publisher.EquipoDesactivadoWasCalled);
        Assert.False(publisher.EquipoReactivadoWasCalled);
    }

    [Fact]
    public async Task CambiarEstado_a_Activo_reactiva_y_publica_EquipoReactivado()
    {
        var lider = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", lider);
        equipo.Desactivar();
        var equipos = new FakeEquipoRepository { TeamToReturn = equipo };
        var publisher = new FakeIdentityEventsPublisher();
        var handler = new CambiarEstadoEquipoAdminCommandHandler(equipos, publisher, TimeProvider.System);

        var response = await handler.Handle(new CambiarEstadoEquipoAdminCommand(equipo.EquipoId, "Activo"), CancellationToken.None);

        Assert.Equal(EstadoEquipo.Activo.ToString(), response.Estado);
        Assert.Equal(EstadoEquipo.Activo, equipo.Estado);
        Assert.True(publisher.EquipoReactivadoWasCalled);
        Assert.False(publisher.EquipoDesactivadoWasCalled);
    }

    // ---------- Eliminar ----------

    [Fact]
    public async Task Eliminar_equipo_inexistente_lanza_EquipoNoEncontradoException()
    {
        var equipos = new FakeEquipoRepository { TeamToReturn = null };
        var invitaciones = new FakeInvitacionEquipoRepository();
        var participaciones = new FakeParticipacionActivaEquipoRepository();
        var publisher = new FakeIdentityEventsPublisher();
        var notifier = new FakeTeamLifecycleNotifier();
        var handler = new EliminarEquipoAdminCommandHandler(equipos, invitaciones, participaciones, publisher, notifier);

        await Assert.ThrowsAsync<EquipoNoEncontradoException>(() =>
            handler.Handle(new EliminarEquipoAdminCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Eliminar_con_participacion_activa_lanza_EquipoConParticipacionActivaException()
    {
        var lider = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", lider);
        var equipos = new FakeEquipoRepository { TeamToReturn = equipo };
        var invitaciones = new FakeInvitacionEquipoRepository();
        var participaciones = new FakeParticipacionActivaEquipoRepository { ExistsByEquipoValue = true };
        var publisher = new FakeIdentityEventsPublisher();
        var notifier = new FakeTeamLifecycleNotifier();
        var handler = new EliminarEquipoAdminCommandHandler(equipos, invitaciones, participaciones, publisher, notifier);

        await Assert.ThrowsAsync<EquipoConParticipacionActivaException>(() =>
            handler.Handle(new EliminarEquipoAdminCommand(equipo.EquipoId), CancellationToken.None));

        Assert.False(equipos.UpdateWasCalled);
        Assert.False(invitaciones.DeletePendientesByEquipoWasCalled);
        Assert.False(publisher.EquipoEliminadoWasCalled);
        Assert.Equal(EstadoEquipo.Activo, equipo.Estado);
    }

    [Fact]
    public async Task Eliminar_happy_path_marca_eliminado_borra_invitaciones_y_publica_evento_Origen_Admin()
    {
        var lider = Guid.NewGuid();
        var miembro = Guid.NewGuid();
        var equipo = Equipo.CrearPorParticipante("Equipo A", lider);
        equipo.AgregarParticipante(miembro);
        var equipos = new FakeEquipoRepository { TeamToReturn = equipo };
        var invitaciones = new FakeInvitacionEquipoRepository();
        var participaciones = new FakeParticipacionActivaEquipoRepository { ExistsByEquipoValue = false };
        var publisher = new FakeIdentityEventsPublisher();
        var notifier = new FakeTeamLifecycleNotifier { OutcomeAotDevolver = new TeamNotificationOutcome(2, 1, 1) };
        var handler = new EliminarEquipoAdminCommandHandler(equipos, invitaciones, participaciones, publisher, notifier);

        var response = await handler.Handle(new EliminarEquipoAdminCommand(equipo.EquipoId), CancellationToken.None);

        Assert.Equal(EstadoEquipo.Eliminado, equipo.Estado);
        Assert.True(equipos.UpdateWasCalled);
        Assert.True(invitaciones.DeletePendientesByEquipoWasCalled);
        Assert.True(publisher.EquipoEliminadoWasCalled);
        Assert.Equal("Admin", publisher.EquipoEliminadoEvent!.Origen);
        Assert.Equal(2, publisher.EquipoEliminadoEvent.Miembros.Count);
        Assert.Contains(lider, publisher.EquipoEliminadoEvent.Miembros);
        Assert.Contains(miembro, publisher.EquipoEliminadoEvent.Miembros);
        Assert.True(notifier.NotificarEliminadoWasCalled);
        // La respuesta refleja el desenlace de la notificación best-effort.
        Assert.Equal(equipo.EquipoId, response.EquipoId);
        Assert.Equal(2, response.IntegrantesTotal);
        Assert.Equal(1, response.IntegrantesNotificados);
        Assert.False(response.ServidorCorreoRespondio);
    }

    // ---------- Fakes compartidos ----------

    private sealed class FakeUsuarioRepository : IUsuarioRepository
    {
        private readonly Dictionary<Guid, Usuario> _usuarios = new();

        public void Agregar(Usuario usuario) => _usuarios[usuario.UsuarioId] = usuario;

        public Task<IReadOnlyList<Usuario>> GetAllAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Usuario>>(_usuarios.Values.ToList());

        public Task<Usuario?> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
            => Task.FromResult(_usuarios.TryGetValue(userId, out var usuario) ? usuario : null);

        public Task<Usuario?> GetByKeycloakIdAsync(Guid keycloakId, CancellationToken cancellationToken)
            => Task.FromResult(_usuarios.Values.FirstOrDefault(u => u.KeycloakId == keycloakId.ToString()));

        public Task<bool> ExistsByEmailAsync(string email, Guid? excludingUserId, CancellationToken cancellationToken)
            => Task.FromResult(false);

        public Task AddAsync(Usuario usuario, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task UpdateAsync(Usuario usuario, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task RemoveAsync(Usuario usuario, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeEquipoRepository : IEquipoRepository
    {
        public Equipo? TeamToReturn { get; set; }
        public bool ExistsActiveTeamByUserIdValue { get; set; }
        public bool AddWasCalled { get; private set; }
        public bool UpdateWasCalled { get; private set; }

        public Task<bool> ExistsActiveTeamByUserIdAsync(Guid userId, CancellationToken cancellationToken)
            => Task.FromResult(ExistsActiveTeamByUserIdValue);

        public Task<Equipo?> GetActiveByMemberUserIdAsync(Guid userId, CancellationToken cancellationToken)
            => Task.FromResult<Equipo?>(null);

        public Task<Equipo?> GetByIdAsync(Guid equipoId, CancellationToken cancellationToken)
            => Task.FromResult(TeamToReturn?.EquipoId == equipoId ? TeamToReturn : null);

        public Task<IReadOnlyList<Equipo>> GetAllAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Equipo>>(TeamToReturn is null ? Array.Empty<Equipo>() : new[] { TeamToReturn });

        public Task AddAsync(Equipo equipo, CancellationToken cancellationToken)
        {
            AddWasCalled = true;
            TeamToReturn = equipo;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Equipo equipo, CancellationToken cancellationToken)
        {
            UpdateWasCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeHistorialNombreEquipoRepository : IHistorialNombreEquipoRepository
    {
        public List<HistorialNombreEquipo> Registros { get; } = new();

        public Task AddRangeAsync(IEnumerable<HistorialNombreEquipo> registros, CancellationToken cancellationToken)
        {
            Registros.AddRange(registros);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<HistorialNombreEquipo>> GetByUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<HistorialNombreEquipo>>(Registros.Where(x => x.UsuarioId == usuarioId).ToList());

        public Task<bool> AnyAsync(CancellationToken cancellationToken) => Task.FromResult(Registros.Count > 0);
    }

    private sealed class FakeInvitacionEquipoRepository : IInvitacionEquipoRepository
    {
        public Task<IReadOnlyCollection<Guid>> GetInvitadoUserIdsPendientesByEquipoAsync(Guid equipoId, CancellationToken ct)
            => Task.FromResult<IReadOnlyCollection<Guid>>(Array.Empty<Guid>());

        public bool DeletePendientesByEquipoWasCalled { get; private set; }

        public Task AddAsync(InvitacionEquipo invitacion, CancellationToken ct) => Task.CompletedTask;

        public Task UpdateAsync(InvitacionEquipo invitacion, CancellationToken ct) => Task.CompletedTask;

        public Task<InvitacionEquipo?> GetByIdAsync(Guid invitacionId, CancellationToken ct)
            => Task.FromResult<InvitacionEquipo?>(null);

        public Task<IReadOnlyList<InvitacionEquipo>> GetPendientesByInvitadoAsync(Guid invitadoUserId, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<InvitacionEquipo>>(new List<InvitacionEquipo>());

        public Task<bool> ExistsPendienteAsync(Guid equipoId, Guid invitadoUserId, CancellationToken ct)
            => Task.FromResult(false);

        public Task DeletePendientesByEquipoAsync(Guid equipoId, CancellationToken ct)
        {
            DeletePendientesByEquipoWasCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeParticipacionActivaEquipoRepository : IParticipacionActivaEquipoRepository
    {
        public bool ExistsByEquipoValue { get; set; }

        public Task UpsertAsync(Guid equipoId, Guid partidaId, DateTime fechaUtc, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task RemoveByPartidaAsync(Guid partidaId, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task RemoveAsync(Guid equipoId, Guid partidaId, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<bool> ExistsByEquipoAsync(Guid equipoId, CancellationToken cancellationToken)
            => Task.FromResult(ExistsByEquipoValue);
    }

    private sealed class FakeIdentityEventsPublisher : IIdentityEventsPublisher
    {
        public bool EquipoCreadoWasCalled { get; private set; }
        public EquipoCreadoIntegrationEvent? EquipoCreadoEvent { get; private set; }
        public bool LiderazgoModificadoWasCalled { get; private set; }
        public LiderazgoEquipoModificadoIntegrationEvent? LiderazgoModificadoEvent { get; private set; }
        public bool EquipoDesactivadoWasCalled { get; private set; }
        public bool EquipoReactivadoWasCalled { get; private set; }
        public bool EquipoEliminadoWasCalled { get; private set; }
        public EquipoEliminadoIntegrationEvent? EquipoEliminadoEvent { get; private set; }

        public Task PublishEquipoCreadoAsync(EquipoCreadoIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        {
            EquipoCreadoWasCalled = true;
            EquipoCreadoEvent = integrationEvent;
            return Task.CompletedTask;
        }

        public Task PublishInvitacionEquipoCreadaAsync(InvitacionEquipoCreadaIntegrationEvent integrationEvent, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task PublishInvitacionEquipoAceptadaAsync(InvitacionEquipoAceptadaIntegrationEvent integrationEvent, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task PublishInvitacionEquipoRechazadaAsync(InvitacionEquipoRechazadaIntegrationEvent integrationEvent, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task PublishRolUsuarioModificadoAsync(RolUsuarioModificadoIntegrationEvent integrationEvent, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task PublishPermisosRolActualizadosAsync(PermisosRolActualizadosIntegrationEvent integrationEvent, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task PublishEquipoEliminadoAsync(EquipoEliminadoIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        {
            EquipoEliminadoWasCalled = true;
            EquipoEliminadoEvent = integrationEvent;
            return Task.CompletedTask;
        }

        public Task PublishLiderazgoEquipoModificadoAsync(LiderazgoEquipoModificadoIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        {
            LiderazgoModificadoWasCalled = true;
            LiderazgoModificadoEvent = integrationEvent;
            return Task.CompletedTask;
        }

        public Task PublishEquipoDesactivadoAsync(EquipoDesactivadoIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        {
            EquipoDesactivadoWasCalled = true;
            return Task.CompletedTask;
        }

        public Task PublishEquipoReactivadoAsync(EquipoReactivadoIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        {
            EquipoReactivadoWasCalled = true;
            return Task.CompletedTask;
        }

        public Task PublishCredencialTemporalEmitidaAsync(CredencialTemporalEmitidaIntegrationEvent integrationEvent, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class FakeTeamLifecycleNotifier : ITeamLifecycleNotifier
    {
        public bool NotificarEliminadoWasCalled { get; private set; }
        public bool NotificarLiderazgoWasCalled { get; private set; }
        public Guid? LiderAnteriorNotificado { get; private set; }
        public Guid? NuevoLiderNotificado { get; private set; }
        public TeamNotificationOutcome OutcomeAotDevolver { get; set; } = new(0, 0, 0);

        public Task<TeamNotificationOutcome> NotificarEquipoEliminadoAsync(string nombreEquipo, IReadOnlyList<Guid> miembros, CancellationToken ct)
        {
            NotificarEliminadoWasCalled = true;
            return Task.FromResult(OutcomeAotDevolver);
        }

        public Task NotificarLiderazgoModificadoAsync(Guid liderAnteriorUserId, Guid nuevoLiderUserId, CancellationToken ct)
        {
            NotificarLiderazgoWasCalled = true;
            LiderAnteriorNotificado = liderAnteriorUserId;
            NuevoLiderNotificado = nuevoLiderUserId;
            return Task.CompletedTask;
        }
    }
}
