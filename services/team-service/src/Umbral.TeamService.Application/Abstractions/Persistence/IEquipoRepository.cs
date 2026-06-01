using Umbral.TeamService.Domain.Entities;

namespace Umbral.TeamService.Application.Abstractions.Persistence;

public interface IEquipoRepository
{
    Task<bool> ExistsActiveTeamByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> ExistsByAccessCodeAsync(string code, CancellationToken cancellationToken);
    Task AddAsync(Equipo equipo, CancellationToken cancellationToken);
}
