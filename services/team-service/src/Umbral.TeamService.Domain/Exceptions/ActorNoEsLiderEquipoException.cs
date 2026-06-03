namespace Umbral.TeamService.Domain.Exceptions;

public sealed class ActorNoEsLiderEquipoException : Exception
{
    public ActorNoEsLiderEquipoException(Guid usuarioId)
        : base($"El participante {usuarioId} no es el lider actual del equipo.")
    {
    }
}
