namespace Umbral.IdentityService.Application.Exceptions;

public sealed class UsuarioConEquipoActivoException : Exception
{
    public UsuarioConEquipoActivoException(Guid usuarioId)
        : base($"El usuario {usuarioId} tiene un equipo activo; debe salir o transferir el liderazgo antes del cambio de rol.")
    {
    }
}
