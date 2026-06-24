namespace Umbral.IdentityService.Domain.Exceptions;

public sealed class InvitacionNoPendienteException : InvalidOperationException
{
    public InvitacionNoPendienteException(Guid invitacionId)
        : base($"La invitacion '{invitacionId}' no esta pendiente.")
    {
    }
}
