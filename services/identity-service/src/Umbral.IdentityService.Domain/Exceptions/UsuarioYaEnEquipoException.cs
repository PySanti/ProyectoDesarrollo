namespace Umbral.IdentityService.Domain.Exceptions;

public sealed class UsuarioYaEnEquipoException : InvalidOperationException
{
    public UsuarioYaEnEquipoException(Guid userId)
        : base($"El usuario '{userId}' ya pertenece a un equipo activo.")
    {
    }
}
