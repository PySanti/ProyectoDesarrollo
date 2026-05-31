using Umbral.IdentityService.Domain.Entities;

namespace Umbral.IdentityService.Application.Abstractions.Persistence;

public interface IUsuarioRepository
{
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken);
    Task AddAsync(Usuario usuario, CancellationToken cancellationToken);
}
