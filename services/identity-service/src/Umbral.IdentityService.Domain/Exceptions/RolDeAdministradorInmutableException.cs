namespace Umbral.IdentityService.Domain.Exceptions;

public sealed class RolDeAdministradorInmutableException : Exception
{
    public RolDeAdministradorInmutableException()
        : base("El rol de un Administrador no puede modificarse (BR-R04).")
    {
    }
}
