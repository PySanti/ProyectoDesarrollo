using Umbral.IdentityService.Application.Users.CreateUser;
using Umbral.IdentityService.Domain.Entities;
using Umbral.IdentityService.Domain.Enums;

namespace Umbral.IdentityService.UnitTests;

public sealed class CreateUserValidatorAndDomainTests
{
    [Fact]
    public void Validator_Should_Fail_When_Email_Is_Invalid()
    {
        var validator = new CreateUserWithInitialRoleCommandValidator();
        var command = new CreateUserWithInitialRoleCommand("Admin", "invalid-email", "Administrador");

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validator_Should_Fail_When_InitialRole_Is_Not_Allowed()
    {
        var validator = new CreateUserWithInitialRoleCommandValidator();
        var command = new CreateUserWithInitialRoleCommand("Admin", "admin@umbral.dev", "Guest");

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Usuario_Crear_Should_Set_Active_Initial_State()
    {
        var usuario = Usuario.Crear("kc-1", "Admin", "admin@umbral.dev", RolUsuario.Administrador);

        Assert.Equal(EstadoUsuario.Activo, usuario.Estado);
        Assert.Equal("kc-1", usuario.KeycloakId);
        Assert.Equal("admin@umbral.dev", usuario.Correo);
    }
}
