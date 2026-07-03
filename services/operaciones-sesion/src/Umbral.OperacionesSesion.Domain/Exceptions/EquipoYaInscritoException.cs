namespace Umbral.OperacionesSesion.Domain.Exceptions;

public sealed class EquipoYaInscritoException : Exception
{
    public EquipoYaInscritoException(Guid equipoId)
        : base($"El equipo {equipoId} ya está inscrito en esta partida.") { }
}
