using Umbral.IdentityService.Domain.Entities;

namespace Umbral.IdentityService.Application.Abstractions.Persistence;

public interface IUsuarioRepository
{
    Task<IReadOnlyList<Usuario>> GetAllAsync(CancellationToken cancellationToken);
    Task<Usuario?> GetByIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> ExistsByEmailAsync(string email, Guid? excludingUserId, CancellationToken cancellationToken);
    Task AddAsync(Usuario usuario, CancellationToken cancellationToken);
    Task UpdateAsync(Usuario usuario, CancellationToken cancellationToken);
}
