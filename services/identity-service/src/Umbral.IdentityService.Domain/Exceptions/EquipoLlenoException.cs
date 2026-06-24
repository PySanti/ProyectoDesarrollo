namespace Umbral.IdentityService.Domain.Exceptions;

public sealed class EquipoLlenoException : InvalidOperationException
{
    public EquipoLlenoException(Guid equipoId)
        : base($"El equipo '{equipoId}' ha alcanzado el maximo de 5 participantes.")
    {
    }
}
