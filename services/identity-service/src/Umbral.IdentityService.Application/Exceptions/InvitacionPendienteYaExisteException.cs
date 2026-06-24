namespace Umbral.IdentityService.Application.Exceptions;

public sealed class InvitacionPendienteYaExisteException : Exception
{
    public InvitacionPendienteYaExisteException(Guid equipoId, Guid invitadoUserId)
        : base($"Ya existe una invitacion pendiente del equipo '{equipoId}' para el usuario '{invitadoUserId}'.")
    {
    }
}
