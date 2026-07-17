using Umbral.IdentityService.Domain.Enums;
using Umbral.IdentityService.Domain.Exceptions;

namespace Umbral.IdentityService.Domain.Entities;

public sealed class InvitacionEquipo
{
    public Guid InvitacionEquipoId { get; private set; }
    public Guid EquipoId { get; private set; }

    // Subs de OIDC, no UsuarioId local. Ver ParticipanteEquipo.SubjectId.
    public Guid InvitadoSubjectId { get; private set; }
    public Guid InvitadoPorSubjectId { get; private set; }
    public EstadoInvitacion Estado { get; private set; }
    public DateTime FechaCreacionUtc { get; private set; }

    private InvitacionEquipo() { }

    public static InvitacionEquipo Crear(Guid equipoId, Guid invitadoSubjectId, Guid invitadoPorSubjectId)
    {
        if (equipoId == Guid.Empty) throw new ArgumentException("EquipoId requerido", nameof(equipoId));
        if (invitadoSubjectId == Guid.Empty) throw new ArgumentException("InvitadoSubjectId requerido", nameof(invitadoSubjectId));
        if (invitadoPorSubjectId == Guid.Empty) throw new ArgumentException("InvitadoPorSubjectId requerido", nameof(invitadoPorSubjectId));

        return new InvitacionEquipo
        {
            InvitacionEquipoId = Guid.NewGuid(),
            EquipoId = equipoId,
            InvitadoSubjectId = invitadoSubjectId,
            InvitadoPorSubjectId = invitadoPorSubjectId,
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
