using Umbral.IdentityService.Domain.Entities;
using Umbral.IdentityService.Domain.Enums;

namespace Umbral.IdentityService.UnitTests;

public sealed class Hu02UsuarioDomainTests
{
    [Fact]
    public void EditarDatosGenerales_Should_Update_Name_And_Normalize_Email()
    {
        var usuario = Usuario.Crear("kc-1", "Nombre Inicial", "Initial@Umbral.Dev", RolUsuario.Administrador);

        usuario.EditarDatosGenerales("  Nombre Editado  ", "  Updated@Umbral.Dev  ");

        Assert.Equal("Nombre Editado", usuario.Nombre);
        Assert.Equal("updated@umbral.dev", usuario.Correo);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EditarDatosGenerales_Should_Throw_When_Name_Is_Invalid(string invalidName)
    {
        var usuario = Usuario.Crear("kc-1", "Nombre Inicial", "initial@umbral.dev", RolUsuario.Administrador);

        Assert.Throws<ArgumentException>(() => usuario.EditarDatosGenerales(invalidName, "mail@umbral.dev"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EditarDatosGenerales_Should_Throw_When_Email_Is_Invalid(string invalidEmail)
    {
        var usuario = Usuario.Crear("kc-1", "Nombre Inicial", "initial@umbral.dev", RolUsuario.Administrador);

        Assert.Throws<ArgumentException>(() => usuario.EditarDatosGenerales("Nombre", invalidEmail));
    }

    [Fact]
    public void Desactivar_Should_Set_Status_To_Desactivado()
    {
        var usuario = Usuario.Crear("kc-1", "Nombre Inicial", "initial@umbral.dev", RolUsuario.Administrador);

        usuario.Desactivar();

        Assert.Equal(EstadoUsuario.Desactivado, usuario.Estado);
    }

    [Fact]
    public void Desactivar_Should_Not_Modify_Role()
    {
        var usuario = Usuario.Crear("kc-1", "Nombre Inicial", "initial@umbral.dev", RolUsuario.Operador);

        usuario.Desactivar();

        Assert.Equal(RolUsuario.Operador, usuario.Rol);
    }
}
