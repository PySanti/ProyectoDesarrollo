using Umbral.IdentityService.Domain.Entities;
using Umbral.IdentityService.Domain.Enums;
using Umbral.IdentityService.Domain.Exceptions;

namespace Umbral.IdentityService.UnitTests.Domain;

public class UsuarioCambiarRolTests
{
    private static Usuario Crear(RolUsuario rol) =>
        Usuario.Crear(Guid.NewGuid().ToString(), "Nombre", "a@b.com", rol);

    [Fact]
    public void Participante_puede_cambiar_a_Operador()
    {
        var usuario = Crear(RolUsuario.Participante);
        usuario.CambiarRol(RolUsuario.Operador);
        Assert.Equal(RolUsuario.Operador, usuario.Rol);
    }

    [Fact]
    public void Operador_puede_promoverse_a_Administrador()
    {
        var usuario = Crear(RolUsuario.Operador);
        usuario.CambiarRol(RolUsuario.Administrador);
        Assert.Equal(RolUsuario.Administrador, usuario.Rol);
    }

    [Fact]
    public void Rol_de_Administrador_es_inmutable()
    {
        var usuario = Crear(RolUsuario.Administrador);
        Assert.Throws<RolDeAdministradorInmutableException>(() => usuario.CambiarRol(RolUsuario.Operador));
    }

    [Fact]
    public void Mismo_rol_es_noop()
    {
        var usuario = Crear(RolUsuario.Participante);
        usuario.CambiarRol(RolUsuario.Participante);
        Assert.Equal(RolUsuario.Participante, usuario.Rol);
    }
}
