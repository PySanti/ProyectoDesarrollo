using Umbral.IdentityService.Application.Abstractions.Identity;
using Umbral.IdentityService.Application.Abstractions.Notifications;
using Umbral.IdentityService.Application.Abstractions.Persistence;
using Umbral.IdentityService.Application.Abstractions.Security;
using Umbral.IdentityService.Application.Exceptions;
using Umbral.IdentityService.Application.Users.DeactivateUser;
using Umbral.IdentityService.Application.Users.GetUserById;
using Umbral.IdentityService.Application.Users.GetUsers;
using Umbral.IdentityService.Application.Users.UpdateUserGeneralData;
using Umbral.IdentityService.Domain.Entities;
using Umbral.IdentityService.Domain.Enums;

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
        Assert.Equal(user.UsuarioId, response[0].UserId);
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

        var response = await handler.Handle(new GetUserByIdQuery(user.UsuarioId), CancellationToken.None);

        Assert.Equal(user.UsuarioId, response.UserId);
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
            new UpdateUserGeneralDataCommand(user.UsuarioId, "Admin Updated", "updated@umbral.dev"),
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
            handler.Handle(new UpdateUserGeneralDataCommand(user.UsuarioId, "Admin", "other@umbral.dev"), CancellationToken.None));
    }

    [Fact]
    public async Task DeactivateUserCommandHandler_Should_Deactivate_User_When_Found()
    {
        var user = Usuario.Crear("kc-1", "Admin", "admin@umbral.dev", RolUsuario.Administrador);
        var repository = new FakeUsuarioRepository([user]);
        var handler = new DeactivateUserCommandHandler(repository);

        var response = await handler.Handle(new DeactivateUserCommand(user.UsuarioId), CancellationToken.None);

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
    public async Task UpdateUserGeneralDataCommandHandler_Should_Resend_Credentials_When_Email_Changes_And_Temp_Password_Pending()
    {
        var user = Usuario.Crear("kc-1", "Admin", "old@umbral.dev", RolUsuario.Operador);
        var repository = new FakeUsuarioRepository([user]);
        var keycloak = new FakeKeycloakIdentityPort(hasTempPassword: true);
        var generator = new FakeTemporaryPasswordGenerator("New-Temp-7");
        var emailSender = new FakeWelcomeEmailSender();
        var handler = BuildUpdateHandler(repository, keycloak, generator, emailSender);

        await handler.Handle(
            new UpdateUserGeneralDataCommand(user.UsuarioId, "Admin", "new@umbral.dev"),
            CancellationToken.None);

        Assert.NotNull(emailSender.LastMessage);
        Assert.Equal("new@umbral.dev", emailSender.LastMessage!.Email);
        Assert.Equal("New-Temp-7", emailSender.LastMessage.TemporaryPassword);
        Assert.Equal("Operador", emailSender.LastMessage.Role);
        Assert.Contains("new@umbral.dev", keycloak.UpdatedEmails);
        Assert.Contains("New-Temp-7", keycloak.ResetPasswords);
    }

    [Fact]
    public async Task UpdateUserGeneralDataCommandHandler_Should_Not_Resend_When_Temp_Password_Not_Pending()
    {
        var user = Usuario.Crear("kc-1", "Admin", "old@umbral.dev", RolUsuario.Operador);
        var repository = new FakeUsuarioRepository([user]);
        var keycloak = new FakeKeycloakIdentityPort(hasTempPassword: false);
        var emailSender = new FakeWelcomeEmailSender();
        var handler = BuildUpdateHandler(repository, keycloak, emailSender: emailSender);

        await handler.Handle(
            new UpdateUserGeneralDataCommand(user.UsuarioId, "Admin", "new@umbral.dev"),
            CancellationToken.None);

        Assert.Null(emailSender.LastMessage);
        Assert.Empty(keycloak.UpdatedEmails);
        Assert.Empty(keycloak.ResetPasswords);
    }

    [Fact]
    public async Task UpdateUserGeneralDataCommandHandler_Should_Not_Check_Keycloak_When_Email_Unchanged()
    {
        var user = Usuario.Crear("kc-1", "Admin", "same@umbral.dev", RolUsuario.Operador);
        var repository = new FakeUsuarioRepository([user]);
        var keycloak = new FakeKeycloakIdentityPort(hasTempPassword: true);
        var emailSender = new FakeWelcomeEmailSender();
        var handler = BuildUpdateHandler(repository, keycloak, emailSender: emailSender);

        await handler.Handle(
            new UpdateUserGeneralDataCommand(user.UsuarioId, "Admin Renamed", "same@umbral.dev"),
            CancellationToken.None);

        Assert.False(keycloak.HasTempPasswordCalled);
        Assert.Null(emailSender.LastMessage);
        Assert.Equal("Admin Renamed", user.Nombre);
    }

    [Fact]
    public async Task UpdateUserGeneralDataCommandHandler_Should_Revert_When_Email_Delivery_Fails()
    {
        var user = Usuario.Crear("kc-1", "Admin", "old@umbral.dev", RolUsuario.Operador);
        var repository = new FakeUsuarioRepository([user]);
        var keycloak = new FakeKeycloakIdentityPort(hasTempPassword: true);
        var emailSender = new FakeWelcomeEmailSender(throwOnSend: true);
        var handler = BuildUpdateHandler(repository, keycloak, emailSender: emailSender);

        await Assert.ThrowsAsync<EmailDeliveryException>(() => handler.Handle(
            new UpdateUserGeneralDataCommand(user.UsuarioId, "Admin Renamed", "new@umbral.dev"),
            CancellationToken.None));

        // Revert: el usuario local vuelve a su email/nombre previos y Keycloak se restaura.
        Assert.Equal("old@umbral.dev", user.Correo);
        Assert.Equal("Admin", user.Nombre);
        Assert.Equal("old@umbral.dev", keycloak.UpdatedEmails[^1]);
    }

    private static UpdateUserGeneralDataCommandHandler BuildUpdateHandler(
        IUsuarioRepository repository,
        FakeKeycloakIdentityPort? keycloak = null,
        FakeTemporaryPasswordGenerator? generator = null,
        FakeWelcomeEmailSender? emailSender = null)
        => new(
            repository,
            keycloak ?? new FakeKeycloakIdentityPort(),
            generator ?? new FakeTemporaryPasswordGenerator("New-Temp-7"),
            emailSender ?? new FakeWelcomeEmailSender());

    private sealed class FakeKeycloakIdentityPort : IKeycloakIdentityPort
    {
        private readonly bool _hasTempPassword;

        public bool HasTempPasswordCalled { get; private set; }
        public List<string> UpdatedEmails { get; } = [];
        public List<string> ResetPasswords { get; } = [];

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
            ResetPasswords.Add(temporaryPassword);
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

        public Task<Usuario?> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
            => Task.FromResult<Usuario?>(StoredUsers.FirstOrDefault(x => x.UsuarioId == userId));

        public Task<bool> ExistsByEmailAsync(string email, Guid? excludingUserId, CancellationToken cancellationToken)
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
