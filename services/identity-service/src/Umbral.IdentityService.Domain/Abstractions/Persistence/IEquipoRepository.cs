using Umbral.IdentityService.Domain.Entities;

namespace Umbral.IdentityService.Domain.Abstractions.Persistence;

public interface IEquipoRepository
{
    Task<bool> ExistsActiveTeamByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<Equipo?> GetActiveByMemberUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<Equipo?> GetByIdAsync(Guid equipoId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Equipo>> GetAllAsync(CancellationToken cancellationToken);
    Task AddAsync(Equipo equipo, CancellationToken cancellationToken);
    Task UpdateAsync(Equipo equipo, CancellationToken cancellationToken);
}
