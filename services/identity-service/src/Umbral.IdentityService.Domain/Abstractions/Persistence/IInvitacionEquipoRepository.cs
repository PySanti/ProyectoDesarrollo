using Umbral.IdentityService.Domain.Entities;

namespace Umbral.IdentityService.Domain.Abstractions.Persistence;

public interface IInvitacionEquipoRepository
{
    Task AddAsync(InvitacionEquipo invitacion, CancellationToken ct);
    Task UpdateAsync(InvitacionEquipo invitacion, CancellationToken ct);
    Task<InvitacionEquipo?> GetByIdAsync(Guid invitacionId, CancellationToken ct);
    Task<IReadOnlyList<InvitacionEquipo>> GetPendientesByInvitadoAsync(Guid invitadoUserId, CancellationToken ct);
    Task<bool> ExistsPendienteAsync(Guid equipoId, Guid invitadoUserId, CancellationToken ct);
    Task<IReadOnlyCollection<Guid>> GetInvitadoUserIdsPendientesByEquipoAsync(Guid equipoId, CancellationToken ct);
    Task DeletePendientesByEquipoAsync(Guid equipoId, CancellationToken ct);
}
