using Umbral.IdentityService.Domain.ValueObjects;
using Umbral.IdentityService.Application.Interfaces;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Application.Exceptions;
using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Application.DTOs;
using Umbral.IdentityService.Application.Queries;
using Umbral.IdentityService.Domain.Entities;
using Umbral.IdentityService.Domain.Enums;

using Umbral.IdentityService.Application.Handlers.Commands;
using Umbral.IdentityService.Application.Handlers.Queries;
namespace Umbral.IdentityService.UnitTests;

public sealed class Hu02HandlersTests
{
    [Fact]
    public async Task GetUsersQueryHandler_Should_Return_Mapped_Users()
    {
        var user = Usuario.Crear("kc-1", "Admin", "admin@umbral.dev", RolUsuario.Administrador);
        var repository = new FakeUsuarioRepository([user]);
        var handler = new GetUsersQueryHandler(repository);

        var response = await handler.Handle(new GetUsersQuery(), CancellationToken.None);

        Assert.Single(response);
        Assert.Equal(user.UsuarioId.Valor, response[0].UserId);
        Assert.Equal("Admin", response[0].Name);
        Assert.Equal("admin@umbral.dev", response[0].Email);
        Assert.Equal("Administrador", response[0].Role);
        Assert.Equal("Activo", response[0].Status);
    }

    [Fact]
    public async Task GetUserByIdQueryHandler_Should_Return_User_Detail_When_Found()
    {
        var user = Usuario.Crear("kc-1", "Admin", "admin@umbral.dev", RolUsuario.Administrador);
        var repository = new FakeUsuarioRepository([user]);
        var handler = new GetUserByIdQueryHandler(repository);

        var response = await handler.Handle(new GetUserByIdQuery(user.UsuarioId.Valor), CancellationToken.None);

        Assert.Equal(user.UsuarioId.Valor, response.UserId);
        Assert.Equal("kc-1", response.KeycloakId);
        Assert.Equal("Admin", response.Name);
        Assert.Equal("admin@umbral.dev", response.Email);
    }

    [Fact]
    public async Task GetUserByIdQueryHandler_Should_Throw_UserNotFoundException_When_Not_Found()
    {
        var repository = new FakeUsuarioRepository();
        var handler = new GetUserByIdQueryHandler(repository);

        await Assert.ThrowsAsync<UserNotFoundException>(() =>
            handler.Handle(new GetUserByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateUserGeneralDataCommandHandler_Should_Update_User_When_Request_Is_Valid()
    {
        var user = Usuario.Crear("kc-1", "Admin", "admin@umbral.dev", RolUsuario.Administrador);
        var repository = new FakeUsuarioRepository([user]);
        var handler = BuildUpdateHandler(repository);

        var response = await handler.Handle(
            new UpdateUserGeneralDataCommand(user.UsuarioId.Valor, "Admin Updated", "updated@umbral.dev"),
            CancellationToken.None);

        Assert.True(repository.UpdateWasCalled);
        Assert.Equal("Admin Updated", user.Nombre);
        Assert.Equal("updated@umbral.dev", user.Correo);
        Assert.Equal("Administrador", response.Role);
        Assert.Equal("Activo", response.Status);
    }

    [Fact]
    public async Task UpdateUserGeneralDataCommandHandler_Should_Throw_UserNotFoundException_When_Not_Found()
    {
        var repository = new FakeUsuarioRepository();
        var handler = BuildUpdateHandler(repository);

        await Assert.ThrowsAsync<UserNotFoundException>(() =>
            handler.Handle(new UpdateUserGeneralDataCommand(Guid.NewGuid(), "Admin", "admin@umbral.dev"), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateUserGeneralDataCommandHandler_Should_Throw_DuplicateEmailException_When_Email_Exists()
    {
        var user = Usuario.Crear("kc-1", "Admin", "admin@umbral.dev", RolUsuario.Administrador);
        var repository = new FakeUsuarioRepository([user], existsByEmail: true);
        var handler = BuildUpdateHandler(repository);

        await Assert.ThrowsAsync<DuplicateEmailException>(() =>
            handler.Handle(new UpdateUserGeneralDataCommand(user.UsuarioId.Valor, "Admin", "other@umbral.dev"), CancellationToken.None));
    }

    [Fact]
    public async Task DeactivateUserCommandHandler_Should_Deactivate_User_When_Found()
    {
        var user = Usuario.Crear("kc-1", "Admin", "admin@umbral.dev", RolUsuario.Administrador);
        var repository = new FakeUsuarioRepository([user]);
        var handler = new DeactivateUserCommandHandler(repository);

        var response = await handler.Handle(new DeactivateUserCommand(user.UsuarioId.Valor), CancellationToken.None);

        Assert.True(repository.UpdateWasCalled);
        Assert.Equal("Desactivado", response.Status);
        Assert.Equal(EstadoUsuario.Desactivado, user.Estado);
    }

    [Fact]
    public async Task DeactivateUserCommandHandler_Should_Throw_UserNotFoundException_When_Not_Found()
    {
        var repository = new FakeUsuarioRepository();
        var handler = new DeactivateUserCommandHandler(repository);

        await Assert.ThrowsAsync<UserNotFoundException>(() =>
            handler.Handle(new DeactivateUserCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateUserGeneralDataCommandHandler_Should_Publish_CredencialTemporalEmitida_When_Email_Changes_And_Temp_Password_Pending()
    {
        var user = Usuario.Crear("kc-1", "Admin", "old@umbral.dev", RolUsuario.Operador);
        var repository = new FakeUsuarioRepository([user]);
        var keycloak = new FakeKeycloakIdentityPort(hasTempPassword: true);
        var generator = new FakeTemporaryPasswordGenerator("New-Temp-7");
        var publisher = new FakeIdentityEventsPublisher();
        var handler = BuildUpdateHandler(repository, keycloak, generator, publisher);

        await handler.Handle(
            new UpdateUserGeneralDataCommand(user.UsuarioId.Valor, "Admin", "new@umbral.dev"),
            CancellationToken.None);

        Assert.NotNull(publisher.LastCredencialEvent);
        Assert.Equal("new@umbral.dev", publisher.LastCredencialEvent!.Correo);
        Assert.Equal("New-Temp-7", publisher.LastCredencialEvent.PasswordTemporal);
        Assert.Equal("Operador", publisher.LastCredencialEvent.Rol);
        Assert.Contains("new@umbral.dev", keycloak.UpdatedEmails);
        Assert.Contains("New-Temp-7", keycloak.ResetPasswords);
    }

    [Fact]
    public async Task UpdateUserGeneralDataCommandHandler_Should_Not_Publish_When_Temp_Password_Not_Pending()
    {
        var user = Usuario.Crear("kc-1", "Admin", "old@umbral.dev", RolUsuario.Operador);
        var repository = new FakeUsuarioRepository([user]);
        var keycloak = new FakeKeycloakIdentityPort(hasTempPassword: false);
        var publisher = new FakeIdentityEventsPublisher();
        var handler = BuildUpdateHandler(repository, keycloak, identityEventsPublisher: publisher);

        await handler.Handle(
            new UpdateUserGeneralDataCommand(user.UsuarioId.Valor, "Admin", "new@umbral.dev"),
            CancellationToken.None);

        Assert.Null(publisher.LastCredencialEvent);
        Assert.Empty(keycloak.UpdatedEmails);
        Assert.Empty(keycloak.ResetPasswords);
    }

    [Fact]
    public async Task UpdateUserGeneralDataCommandHandler_Should_Not_Check_Keycloak_When_Email_Unchanged()
    {
        var user = Usuario.Crear("kc-1", "Admin", "same@umbral.dev", RolUsuario.Operador);
        var repository = new FakeUsuarioRepository([user]);
        var keycloak = new FakeKeycloakIdentityPort(hasTempPassword: true);
        var publisher = new FakeIdentityEventsPublisher();
        var handler = BuildUpdateHandler(repository, keycloak, identityEventsPublisher: publisher);

        await handler.Handle(
            new UpdateUserGeneralDataCommand(user.UsuarioId.Valor, "Admin Renamed", "same@umbral.dev"),
            CancellationToken.None);

        Assert.False(keycloak.HasTempPasswordCalled);
        Assert.Null(publisher.LastCredencialEvent);
        Assert.Equal("Admin Renamed", user.Nombre);
    }

    [Fact]
    public async Task UpdateUserGeneralDataCommandHandler_Should_Revert_When_Keycloak_Password_Reset_Fails()
    {
        var user = Usuario.Crear("kc-1", "Admin", "old@umbral.dev", RolUsuario.Operador);
        var repository = new FakeUsuarioRepository([user]);
        var keycloak = new FakeKeycloakIdentityPort(hasTempPassword: true)
        {
            Lanzar = new KeycloakIntegrationException("reset failed"),
        };
        var publisher = new FakeIdentityEventsPublisher();
        var handler = BuildUpdateHandler(repository, keycloak, identityEventsPublisher: publisher);

        await Assert.ThrowsAsync<KeycloakIntegrationException>(() => handler.Handle(
            new UpdateUserGeneralDataCommand(user.UsuarioId.Valor, "Admin Renamed", "new@umbral.dev"),
            CancellationToken.None));

        // Revert: el usuario local vuelve a su email/nombre previos y Keycloak se restaura.
        Assert.Equal("old@umbral.dev", user.Correo);
        Assert.Equal("Admin", user.Nombre);
        Assert.Equal("old@umbral.dev", keycloak.UpdatedEmails[^1]);
        // El fallo ocurre en Keycloak, antes de llegar a publicar: no se emite el evento.
        Assert.Null(publisher.LastCredencialEvent);
    }

    [Fact]
    public async Task UpdateUserGeneralDataCommandHandler_Should_Not_Compensate_When_Event_Publication_Fails()
    {
        var user = Usuario.Crear("kc-1", "Admin", "old@umbral.dev", RolUsuario.Operador);
        var repository = new FakeUsuarioRepository([user]);
        var keycloak = new FakeKeycloakIdentityPort(hasTempPassword: true);
        var generator = new FakeTemporaryPasswordGenerator("New-Temp-7");
        // Mirrors the real publisher contract (RabbitMq/Composite, ADR-0012): a broker/publication
        // failure is swallowed internally and never escapes to the caller.
        var publisher = new FakeIdentityEventsPublisher(throwOnPublishCredencial: true);
        var handler = BuildUpdateHandler(repository, keycloak, generator, publisher);

        var response = await handler.Handle(
            new UpdateUserGeneralDataCommand(user.UsuarioId.Valor, "Admin Renamed", "new@umbral.dev"),
            CancellationToken.None);

        // The email/name change is a fait accompli: a failed event publication does not revert it.
        Assert.True(publisher.CredencialPublishAttempted);
        Assert.Null(publisher.LastCredencialEvent);
        Assert.Equal("new@umbral.dev", user.Correo);
        Assert.Equal("Admin Renamed", user.Nombre);
        Assert.Equal("new@umbral.dev", response.Email);
        Assert.Contains("new@umbral.dev", keycloak.UpdatedEmails);
        Assert.Contains("New-Temp-7", keycloak.ResetPasswords);
    }

    private static UpdateUserGeneralDataCommandHandler BuildUpdateHandler(
        IUsuarioRepository repository,
        FakeKeycloakIdentityPort? keycloak = null,
        FakeTemporaryPasswordGenerator? generator = null,
        FakeIdentityEventsPublisher? identityEventsPublisher = null)
        => new(
            repository,
            keycloak ?? new FakeKeycloakIdentityPort(),
            generator ?? new FakeTemporaryPasswordGenerator("New-Temp-7"),
            identityEventsPublisher ?? new FakeIdentityEventsPublisher());

    private sealed class FakeKeycloakIdentityPort : IKeycloakIdentityPort
    {
        private readonly bool _hasTempPassword;

        public bool HasTempPasswordCalled { get; private set; }
        public List<string> UpdatedEmails { get; } = [];
        public List<string> ResetPasswords { get; } = [];
        public List<(string RoleName, string CompositeRoleName)> CompositesAgregados { get; } = [];
        public List<(string RoleName, string CompositeRoleName)> CompositesQuitados { get; } = [];
        public List<(string KeycloakId, string OldRoleName, string NewRoleName)> CambiosDeRol { get; } = [];
        public Exception? Lanzar { get; set; }

        public FakeKeycloakIdentityPort(bool hasTempPassword = false)
        {
            _hasTempPassword = hasTempPassword;
        }

        public Task<string> CreateUserWithInitialRoleAsync(string name, string email, string initialRole, string temporaryPassword, CancellationToken cancellationToken)
            => Task.FromResult("kc-1");

        public Task DeleteUserAsync(string keycloakId, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task<bool> HasTemporaryPasswordAsync(string keycloakId, CancellationToken cancellationToken)
        {
            HasTempPasswordCalled = true;
            return Task.FromResult(_hasTempPassword);
        }

        public Task UpdateEmailAsync(string keycloakId, string email, CancellationToken cancellationToken)
        {
            UpdatedEmails.Add(email);
            return Task.CompletedTask;
        }

        public Task ResetTemporaryPasswordAsync(string keycloakId, string temporaryPassword, CancellationToken cancellationToken)
        {
            if (Lanzar is not null)
            {
                throw Lanzar;
            }

            ResetPasswords.Add(temporaryPassword);
            return Task.CompletedTask;
        }

        public Task AddCompositeToRoleAsync(string roleName, string compositeRoleName, CancellationToken cancellationToken)
        {
            if (Lanzar is not null)
            {
                throw Lanzar;
            }

            CompositesAgregados.Add((roleName, compositeRoleName));
            return Task.CompletedTask;
        }

        public Task RemoveCompositeFromRoleAsync(string roleName, string compositeRoleName, CancellationToken cancellationToken)
        {
            if (Lanzar is not null)
            {
                throw Lanzar;
            }

            CompositesQuitados.Add((roleName, compositeRoleName));
            return Task.CompletedTask;
        }

        public Task ChangeUserRealmRoleAsync(string keycloakId, string oldRoleName, string newRoleName, CancellationToken cancellationToken)
        {
            if (Lanzar is not null)
            {
                throw Lanzar;
            }

            CambiosDeRol.Add((keycloakId, oldRoleName, newRoleName));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeTemporaryPasswordGenerator : ITemporaryPasswordGenerator
    {
        private readonly string _password;

        public FakeTemporaryPasswordGenerator(string password)
        {
            _password = password;
        }

        public string Generate() => _password;
    }

    private sealed class FakeIdentityEventsPublisher : IIdentityEventsPublisher
    {
        private readonly bool _throwOnPublishCredencial;

        public CredencialTemporalEmitidaIntegrationEvent? LastCredencialEvent { get; private set; }
        public bool CredencialPublishAttempted { get; private set; }

        public FakeIdentityEventsPublisher(bool throwOnPublishCredencial = false)
        {
            _throwOnPublishCredencial = throwOnPublishCredencial;
        }

        public Task PublishCredencialTemporalEmitidaAsync(CredencialTemporalEmitidaIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        {
            CredencialPublishAttempted = true;
            // Best-effort (ADR-0012): a real implementation swallows the failure internally and
            // never lets it reach the caller — mirrored here instead of throwing.
            if (!_throwOnPublishCredencial)
            {
                LastCredencialEvent = integrationEvent;
            }

            return Task.CompletedTask;
        }

        public Task PublishEquipoCreadoAsync(EquipoCreadoIntegrationEvent integrationEvent, CancellationToken cancellationToken)
            => Task.CompletedTask;

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
            => Task.CompletedTask;

        public Task PublishLiderazgoEquipoModificadoAsync(LiderazgoEquipoModificadoIntegrationEvent integrationEvent, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task PublishEquipoDesactivadoAsync(EquipoDesactivadoIntegrationEvent integrationEvent, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task PublishEquipoReactivadoAsync(EquipoReactivadoIntegrationEvent integrationEvent, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class FakeUsuarioRepository : IUsuarioRepository
    {
        private readonly bool _existsByEmail;
        public List<Usuario> StoredUsers { get; }
        public bool UpdateWasCalled { get; private set; }

        public FakeUsuarioRepository(List<Usuario>? users = null, bool existsByEmail = false)
        {
            StoredUsers = users ?? [];
            _existsByEmail = existsByEmail;
        }

        public Task<IReadOnlyList<Usuario>> GetAllAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Usuario>>(StoredUsers);

        public Task<Usuario?> GetByIdAsync(UsuarioLocalId userId, CancellationToken cancellationToken)
            => Task.FromResult<Usuario?>(StoredUsers.FirstOrDefault(x => x.UsuarioId == userId));

        public Task<Usuario?> GetByKeycloakIdAsync(Guid keycloakId, CancellationToken cancellationToken)
            => Task.FromResult<Usuario?>(StoredUsers.FirstOrDefault(x => x.KeycloakId == keycloakId.ToString()));

        public Task<bool> ExistsByEmailAsync(string email, UsuarioLocalId? excludingUserId, CancellationToken cancellationToken)
            => Task.FromResult(_existsByEmail);

        public Task AddAsync(Usuario usuario, CancellationToken cancellationToken)
        {
            StoredUsers.Add(usuario);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Usuario usuario, CancellationToken cancellationToken)
        {
            UpdateWasCalled = true;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(Usuario usuario, CancellationToken cancellationToken)
        {
            StoredUsers.Remove(usuario);
            return Task.CompletedTask;
        }
    }
}
