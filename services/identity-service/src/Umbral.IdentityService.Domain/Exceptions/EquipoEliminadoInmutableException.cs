namespace Umbral.IdentityService.Domain.Exceptions;

public sealed class EquipoEliminadoInmutableException : Exception
{
    public Guid EquipoId { get; }

    public EquipoEliminadoInmutableException(Guid equipoId)
        : base($"El equipo {equipoId} está eliminado y no admite cambios.")
        => EquipoId = equipoId;
}
