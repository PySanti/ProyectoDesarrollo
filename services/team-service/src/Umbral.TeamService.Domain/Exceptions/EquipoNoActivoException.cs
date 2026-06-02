namespace Umbral.TeamService.Domain.Exceptions;

public sealed class EquipoNoActivoException : InvalidOperationException
{
    public EquipoNoActivoException(Guid equipoId)
        : base($"El equipo {equipoId} no esta activo.")
    {
    }
}
