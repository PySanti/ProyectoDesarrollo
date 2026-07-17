using Umbral.IdentityService.Domain.ValueObjects;
using Umbral.IdentityService.Application.Interfaces;
using Umbral.IdentityService.Domain.Abstractions.Persistence;
using Umbral.IdentityService.Application.Exceptions;
using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Application.DTOs;
using Umbral.IdentityService.Domain.Entities;

using Umbral.IdentityService.Application.Handlers.Commands;
namespace Umbral.IdentityService.UnitTests;

public sealed class CreateUserHandlerTests
{
    [Fact]
    public async Task Handle_Should_Create_User_When_Request_Is_Valid()
    {
        var repository = new FakeUsuarioRepository();
        var keycloak = new FakeKeycloakIdentityPort("kc-123");
        var passwordGenerator = new FakeTemporaryPasswordGenerator("Temp-Pass-1");
        var publisher = new FakeIdentityEventsPublisher();
        var handler = new CreateUserWithInitialRoleCommandHandler(repository, keycloak, passwordGenerator, publisher);

        var response = await handler.Handle(
            new CreateUserWithInitialRoleCommand("Admin", "admin@umbral.dev", "Administrador"),
            CancellationToken.None);

        Assert.Equal("kc-123", response.KeycloakId);
        Assert.Equal("Administrador", response.Role);
        Assert.Single(repository.StoredUsers);
        Assert.Empty(keycloak.DeletedIds);
    }

    [Fact]
    public async Task Handle_Should_Publish_CredencialTemporalEmitida_With_Generated_Temporary_Password()
    {
        var repository = new FakeUsuarioRepository();
        var keycloak = new FakeKeycloakIdentityPort("kc-123");
        var passwordGenerator = new FakeTemporaryPasswordGenerator("Generated-Temp-9");
        var publisher = new FakeIdentityEventsPublisher();
        var handler = new CreateUserWithInitialRoleCommandHandler(repository, keycloak, passwordGenerator, publisher);

        await handler.Handle(
            new CreateUserWithInitialRoleCommand("Ana", "ANA@umbral.dev", "Participante"),
            CancellationToken.None);

        var evento = Assert.Single(publisher.CredencialesEmitidas);
        Assert.Equal("Ana", evento.Nombre);
        Assert.Equal("ana@umbral.dev", evento.Correo);
        Assert.Equal("Participante", evento.Rol);
        Assert.Equal("Generated-Temp-9", evento.PasswordTemporal);
        // The temporary password is sent to Keycloak too (never persisted locally).
        Assert.Equal("Generated-Temp-9", keycloak.LastTemporaryPassword);
    }

    [Fact]
    public async Task Handle_Should_Not_Compensate_When_Event_Publication_Fails()
    {
        var repository = new FakeUsuarioRepository();
        var keycloak = new FakeKeycloakIdentityPort("kc-999");
        var passwordGenerator = new FakeTemporaryPasswordGenerator("Temp-Pass-1");
        // Mirrors the real publisher contract (RabbitMq/Composite, ADR-0012): a broker/publication
        // failure is swallowed internally and never escapes to the caller.
        var publisher = new FakeIdentityEventsPublisher(throwOnPublishCredencial: true);
        var handler = new CreateUserWithInitialRoleCommandHandler(repository, keycloak, passwordGenerator, publisher);

        var response = await handler.Handle(
            new CreateUserWithInitialRoleCommand("Admin", "admin@umbral.dev", "Administrador"),
            CancellationToken.None);

        // The user creation is a fait accompli: a failed event publication does not compensate it.
        Assert.True(publisher.CredencialPublishAttempted);
        Assert.Empty(publisher.CredencialesEmitidas);
        Assert.Single(repository.StoredUsers);
        Assert.Empty(keycloak.DeletedIds);
        Assert.Equal("Administrador", response.Role);
    }

    [Fact]
    public async Task Handle_Should_Throw_DuplicateEmailException_When_Email_Exists()
    {
        var repository = new FakeUsuarioRepository(existsByEmail: true);
        var keycloak = new FakeKeycloakIdentityPort("kc-123");
        var handler = new CreateUserWithInitialRoleCommandHandler(
            repository, keycloak, new FakeTemporaryPasswordGenerator("Temp-Pass-1"), new FakeIdentityEventsPublisher());

        await Assert.ThrowsAsync<DuplicateEmailException>(() => handler.Handle(
            new CreateUserWithInitialRoleCommand("Admin", "admin@umbral.dev", "Administrador"),
            CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Should_Propagate_KeycloakIntegrationException_When_Keycloak_Fails()
    {
        var repository = new FakeUsuarioRepository();
        var keycloak = new FakeKeycloakIdentityPort(exception: new KeycloakIntegrationException("keycloak failed"));
        var handler = new CreateUserWithInitialRoleCommandHandler(
            repository, keycloak, new FakeTemporaryPasswordGenerator("Temp-Pass-1"), new FakeIdentityEventsPublisher());

        await Assert.ThrowsAsync<KeycloakIntegrationException>(() => handler.Handle(
            new CreateUserWithInitialRoleCommand("Admin", "admin@umbral.dev", "Administrador"),
            CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Should_Propagate_KeycloakIntegrationException_When_Actor_Is_Unauthorized()
    {
        var repository = new FakeUsuarioRepository();
        var keycloak = new FakeKeycloakIdentityPort(exception: new KeycloakIntegrationException("authenticated user is not administrator"));
        var handler = new CreateUserWithInitialRoleCommandHandler(
            repository, keycloak, new FakeTemporaryPasswordGenerator("Temp-Pass-1"), new FakeIdentityEventsPublisher());

        await Assert.ThrowsAsync<KeycloakIntegrationException>(() => handler.Handle(
            new CreateUserWithInitialRoleCommand("Operator", "operator@umbral.dev", "Operador"),
            CancellationToken.None));
    }

    private sealed class FakeUsuarioRepository : IUsuarioRepository
    {
        private readonly bool _existsByEmail;
        public List<Usuario> StoredUsers { get; } = [];

        public FakeUsuarioRepository(bool existsByEmail = false)
        {
            _existsByEmail = existsByEmail;
        }

        public Task<IReadOnlyList<Usuario>> GetAllAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Usuario>>(StoredUsers);

        public Task<Usuario?> GetByIdAsync(UsuarioLocalId userId, CancellationToken cancellationToken)
            => Task.FromResult<Usuario?>(StoredUsers.FirstOrDefault(u => u.UsuarioId == userId));

        public Task<Usuario?> GetByKeycloakIdAsync(Guid keycloakId, CancellationToken cancellationToken)
            => Task.FromResult<Usuario?>(StoredUsers.FirstOrDefault(u => u.KeycloakId == keycloakId.ToString()));

        public Task<bool> ExistsByEmailAsync(string email, UsuarioLocalId? excludingUserId, CancellationToken cancellationToken)
            => Task.FromResult(_existsByEmail);

        public Task AddAsync(Usuario usuario, CancellationToken cancellationToken)
        {
            StoredUsers.Add(usuario);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Usuario usuario, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task RemoveAsync(Usuario usuario, CancellationToken cancellationToken)
        {
            StoredUsers.Remove(usuario);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeKeycloakIdentityPort : IKeycloakIdentityPort
    {
        private readonly string _keycloakId;
        private readonly Exception? _exception;

        public string? LastTemporaryPassword { get; private set; }
        public List<string> DeletedIds { get; } = [];
        public List<(string RoleName, string CompositeRoleName)> CompositesAgregados { get; } = [];
        public List<(string RoleName, string CompositeRoleName)> CompositesQuitados { get; } = [];
        public List<(string KeycloakId, string OldRoleName, string NewRoleName)> CambiosDeRol { get; } = [];
        public Exception? Lanzar { get; set; }

        public FakeKeycloakIdentityPort(string keycloakId = "kc-default", Exception? exception = null)
        {
            _keycloakId = keycloakId;
            _exception = exception;
        }

        public Task<string> CreateUserWithInitialRoleAsync(string name, string email, string initialRole, string temporaryPassword, CancellationToken cancellationToken)
        {
            if (_exception is not null)
            {
                throw _exception;
            }

            LastTemporaryPassword = temporaryPassword;
            return Task.FromResult(_keycloakId);
        }

        public Task DeleteUserAsync(string keycloakId, CancellationToken cancellationToken)
        {
            DeletedIds.Add(keycloakId);
            return Task.CompletedTask;
        }

        public Task<bool> HasTemporaryPasswordAsync(string keycloakId, CancellationToken cancellationToken)
            => Task.FromResult(false);

        public Task UpdateEmailAsync(string keycloakId, string email, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task ResetTemporaryPasswordAsync(string keycloakId, string temporaryPassword, CancellationToken cancellationToken)
            => Task.CompletedTask;

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

        public List<CredencialTemporalEmitidaIntegrationEvent> CredencialesEmitidas { get; } = [];
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
                CredencialesEmitidas.Add(integrationEvent);
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
}
