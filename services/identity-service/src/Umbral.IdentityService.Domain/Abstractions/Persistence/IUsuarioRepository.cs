using Umbral.IdentityService.Domain.Entities;

namespace Umbral.IdentityService.Domain.Abstractions.Persistence;

public interface IUsuarioRepository
{
    Task<IReadOnlyList<Usuario>> GetAllAsync(CancellationToken cancellationToken);
    Task<Usuario?> GetByIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> ExistsByEmailAsync(string email, Guid? excludingUserId, CancellationToken cancellationToken);
    Task AddAsync(Usuario usuario, CancellationToken cancellationToken);
    Task UpdateAsync(Usuario usuario, CancellationToken cancellationToken);

    /// <summary>
    /// Elimina un usuario local. Se usa para compensar la persistencia cuando el envío del
    /// correo de bienvenida falla tras haber creado el usuario.
    /// </summary>
    Task RemoveAsync(Usuario usuario, CancellationToken cancellationToken);
}
