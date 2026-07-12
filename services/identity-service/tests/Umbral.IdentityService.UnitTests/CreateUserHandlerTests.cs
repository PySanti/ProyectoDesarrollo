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
        var emailSender = new FakeWelcomeEmailSender();
        var handler = new CreateUserWithInitialRoleCommandHandler(repository, keycloak, passwordGenerator, emailSender);

        var response = await handler.Handle(
            new CreateUserWithInitialRoleCommand("Admin", "admin@umbral.dev", "Administrador"),
            CancellationToken.None);

        Assert.Equal("kc-123", response.KeycloakId);
        Assert.Equal("Administrador", response.Role);
        Assert.Single(repository.StoredUsers);
        Assert.Empty(keycloak.DeletedIds);
    }

    [Fact]
    public async Task Handle_Should_Send_Welcome_Email_With_Generated_Temporary_Password()
    {
        var repository = new FakeUsuarioRepository();
        var keycloak = new FakeKeycloakIdentityPort("kc-123");
        var passwordGenerator = new FakeTemporaryPasswordGenerator("Generated-Temp-9");
        var emailSender = new FakeWelcomeEmailSender();
        var handler = new CreateUserWithInitialRoleCommandHandler(repository, keycloak, passwordGenerator, emailSender);

        await handler.Handle(
            new CreateUserWithInitialRoleCommand("Ana", "ANA@umbral.dev", "Participante"),
            CancellationToken.None);

        Assert.NotNull(emailSender.LastMessage);
        Assert.Equal("ana@umbral.dev", emailSender.LastMessage!.Email);
        Assert.Equal("Participante", emailSender.LastMessage.Role);
        Assert.Equal("Generated-Temp-9", emailSender.LastMessage.TemporaryPassword);
        // The temporary password is sent to Keycloak too (never persisted locally).
        Assert.Equal("Generated-Temp-9", keycloak.LastTemporaryPassword);
    }

    [Fact]
    public async Task Handle_Should_Compensate_When_Email_Delivery_Fails()
    {
        var repository = new FakeUsuarioRepository();
        var keycloak = new FakeKeycloakIdentityPort("kc-999");
        var passwordGenerator = new FakeTemporaryPasswordGenerator("Temp-Pass-1");
        var emailSender = new FakeWelcomeEmailSender(throwOnSend: true);
        var handler = new CreateUserWithInitialRoleCommandHandler(repository, keycloak, passwordGenerator, emailSender);

        await Assert.ThrowsAsync<EmailDeliveryException>(() => handler.Handle(
            new CreateUserWithInitialRoleCommand("Admin", "admin@umbral.dev", "Administrador"),
            CancellationToken.None));

        // Compensation: local persistence rolled back and Keycloak user deleted.
        Assert.Empty(repository.StoredUsers);
        Assert.Contains("kc-999", keycloak.DeletedIds);
    }

    [Fact]
    public async Task Handle_Should_Throw_DuplicateEmailException_When_Email_Exists()
    {
        var repository = new FakeUsuarioRepository(existsByEmail: true);
        var keycloak = new FakeKeycloakIdentityPort("kc-123");
        var handler = new CreateUserWithInitialRoleCommandHandler(
            repository, keycloak, new FakeTemporaryPasswordGenerator("Temp-Pass-1"), new FakeWelcomeEmailSender());

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
            repository, keycloak, new FakeTemporaryPasswordGenerator("Temp-Pass-1"), new FakeWelcomeEmailSender());

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
            repository, keycloak, new FakeTemporaryPasswordGenerator("Temp-Pass-1"), new FakeWelcomeEmailSender());

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

        public Task<Usuario?> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
            => Task.FromResult<Usuario?>(StoredUsers.FirstOrDefault(u => u.UsuarioId == userId));

        public Task<Usuario?> GetByKeycloakIdAsync(Guid keycloakId, CancellationToken cancellationToken)
            => Task.FromResult<Usuario?>(StoredUsers.FirstOrDefault(u => u.KeycloakId == keycloakId.ToString()));

        public Task<bool> ExistsByEmailAsync(string email, Guid? excludingUserId, CancellationToken cancellationToken)
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

    private sealed class FakeWelcomeEmailSender : IUserWelcomeEmailSender
    {
        private readonly bool _throwOnSend;

        public UserWelcomeEmailMessage? LastMessage { get; private set; }

        public FakeWelcomeEmailSender(bool throwOnSend = false)
        {
            _throwOnSend = throwOnSend;
        }

        public Task SendWelcomeEmailAsync(UserWelcomeEmailMessage message, CancellationToken cancellationToken)
        {
            if (_throwOnSend)
            {
                throw new EmailDeliveryException("forced email failure for test");
            }

            LastMessage = message;
            return Task.CompletedTask;
        }
    }
}
