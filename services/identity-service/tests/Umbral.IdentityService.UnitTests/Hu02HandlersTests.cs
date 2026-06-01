using Umbral.IdentityService.Application.Abstractions.Persistence;
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
        var handler = new UpdateUserGeneralDataCommandHandler(repository);

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
        var handler = new UpdateUserGeneralDataCommandHandler(repository);

        await Assert.ThrowsAsync<UserNotFoundException>(() =>
            handler.Handle(new UpdateUserGeneralDataCommand(Guid.NewGuid(), "Admin", "admin@umbral.dev"), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateUserGeneralDataCommandHandler_Should_Throw_DuplicateEmailException_When_Email_Exists()
    {
        var user = Usuario.Crear("kc-1", "Admin", "admin@umbral.dev", RolUsuario.Administrador);
        var repository = new FakeUsuarioRepository([user], existsByEmail: true);
        var handler = new UpdateUserGeneralDataCommandHandler(repository);

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
    }
}
