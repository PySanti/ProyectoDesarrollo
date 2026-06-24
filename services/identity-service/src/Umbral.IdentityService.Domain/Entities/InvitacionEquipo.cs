using Umbral.IdentityService.Domain.Enums;
using Umbral.IdentityService.Domain.Exceptions;

namespace Umbral.IdentityService.Domain.Entities;

public sealed class InvitacionEquipo
{
    public Guid InvitacionEquipoId { get; private set; }
    public Guid EquipoId { get; private set; }
    public Guid InvitadoUserId { get; private set; }
    public Guid InvitadoPorUserId { get; private set; }
    public EstadoInvitacion Estado { get; private set; }
    public DateTime FechaCreacionUtc { get; private set; }

    private InvitacionEquipo() { }

    public static InvitacionEquipo Crear(Guid equipoId, Guid invitadoUserId, Guid invitadoPorUserId)
    {
        if (equipoId == Guid.Empty) throw new ArgumentException("EquipoId requerido", nameof(equipoId));
        if (invitadoUserId == Guid.Empty) throw new ArgumentException("InvitadoUserId requerido", nameof(invitadoUserId));
        if (invitadoPorUserId == Guid.Empty) throw new ArgumentException("InvitadoPorUserId requerido", nameof(invitadoPorUserId));

        return new InvitacionEquipo
        {
            InvitacionEquipoId = Guid.NewGuid(),
            EquipoId = equipoId,
            InvitadoUserId = invitadoUserId,
            InvitadoPorUserId = invitadoPorUserId,
            Estado = EstadoInvitacion.Pendiente,
            FechaCreacionUtc = DateTime.UtcNow,
        };
    }

    public void Aceptar()
    {
        if (Estado != EstadoInvitacion.Pendiente)
            throw new InvitacionNoPendienteException(InvitacionEquipoId);

        Estado = EstadoInvitacion.Aceptada;
    }

    public void Rechazar()
    {
        if (Estado != EstadoInvitacion.Pendiente)
            throw new InvitacionNoPendienteException(InvitacionEquipoId);

        Estado = EstadoInvitacion.Rechazada;
    }
}
