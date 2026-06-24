namespace Umbral.IdentityService.Application.Exceptions;

public sealed class InvitacionNoEncontradaException : Exception
{
    public InvitacionNoEncontradaException(Guid invitacionId)
        : base($"La invitacion '{invitacionId}' no fue encontrada.")
    {
    }
}
