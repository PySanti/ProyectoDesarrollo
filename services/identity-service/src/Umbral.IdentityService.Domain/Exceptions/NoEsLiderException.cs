namespace Umbral.IdentityService.Domain.Exceptions;

public sealed class NoEsLiderException : InvalidOperationException
{
    public NoEsLiderException(Guid actorUserId)
        : base($"El usuario '{actorUserId}' no es lider del equipo.")
    {
    }
}
