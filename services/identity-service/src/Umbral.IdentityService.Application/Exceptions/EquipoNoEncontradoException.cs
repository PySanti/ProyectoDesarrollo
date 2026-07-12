namespace Umbral.IdentityService.Application.Exceptions;

public sealed class EquipoNoEncontradoException : Exception
{
    public Guid EquipoId { get; }

    public EquipoNoEncontradoException(Guid equipoId)
        : base($"El equipo {equipoId} no fue encontrado.")
        => EquipoId = equipoId;
}
