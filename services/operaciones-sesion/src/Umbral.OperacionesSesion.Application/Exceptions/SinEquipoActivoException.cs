namespace Umbral.OperacionesSesion.Application.Exceptions;

public sealed class SinEquipoActivoException : Exception
{
    public SinEquipoActivoException(Guid usuarioId)
        : base($"El usuario {usuarioId} no tiene un equipo activo.") { }
}
