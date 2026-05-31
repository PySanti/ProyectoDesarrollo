using Umbral.IdentityService.Application.Abstractions.Identity;
using Umbral.IdentityService.Application.Abstractions.Persistence;
using Umbral.IdentityService.Application.Exceptions;
using Umbral.IdentityService.Application.Users.CreateUser;
using Umbral.IdentityService.Domain.Entities;

namespace Umbral.IdentityService.UnitTests;

public sealed class CreateUserHandlerTests
{
    [Fact]
    public async Task Handle_Should_Create_User_When_Request_Is_Valid()
    {
        var repository = new FakeUsuarioRepository();
        var keycloak = new FakeKeycloakIdentityPort("kc-123");
        var handler = new CreateUserWithInitialRoleCommandHandler(repository, keycloak);

        var response = await handler.Handle(
            new CreateUserWithInitialRoleCommand("Admin", "admin@umbral.dev", "Administrador"),
            CancellationToken.None);

        Assert.Equal("kc-123", response.KeycloakId);
        Assert.Equal("Administrador", response.Role);
        Assert.Single(repository.StoredUsers);
    }

    [Fact]
    public async Task Handle_Should_Throw_DuplicateEmailException_When_Email_Exists()
    {
        var repository = new FakeUsuarioRepository(existsByEmail: true);
        var keycloak = new FakeKeycloakIdentityPort("kc-123");
        var handler = new CreateUserWithInitialRoleCommandHandler(repository, keycloak);

        await Assert.ThrowsAsync<DuplicateEmailException>(() => handler.Handle(
            new CreateUserWithInitialRoleCommand("Admin", "admin@umbral.dev", "Administrador"),
            CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Should_Propagate_KeycloakIntegrationException_When_Keycloak_Fails()
    {
        var repository = new FakeUsuarioRepository();
        var keycloak = new FakeKeycloakIdentityPort(exception: new KeycloakIntegrationException("keycloak failed"));
        var handler = new CreateUserWithInitialRoleCommandHandler(repository, keycloak);

        await Assert.ThrowsAsync<KeycloakIntegrationException>(() => handler.Handle(
            new CreateUserWithInitialRoleCommand("Admin", "admin@umbral.dev", "Administrador"),
            CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Should_Propagate_KeycloakIntegrationException_When_Actor_Is_Unauthorized()
    {
        var repository = new FakeUsuarioRepository();
        var keycloak = new FakeKeycloakIdentityPort(exception: new KeycloakIntegrationException("authenticated user is not administrator"));
        var handler = new CreateUserWithInitialRoleCommandHandler(repository, keycloak);

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

        public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken)
            => Task.FromResult(_existsByEmail);

        public Task AddAsync(Usuario usuario, CancellationToken cancellationToken)
        {
            StoredUsers.Add(usuario);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeKeycloakIdentityPort : IKeycloakIdentityPort
    {
        private readonly string _keycloakId;
        private readonly Exception? _exception;

        public FakeKeycloakIdentityPort(string keycloakId = "kc-default", Exception? exception = null)
        {
            _keycloakId = keycloakId;
            _exception = exception;
        }

        public Task<string> CreateUserWithInitialRoleAsync(string name, string email, string initialRole, CancellationToken cancellationToken)
        {
            if (_exception is not null)
            {
                throw _exception;
            }

            return Task.FromResult(_keycloakId);
        }
    }
}
