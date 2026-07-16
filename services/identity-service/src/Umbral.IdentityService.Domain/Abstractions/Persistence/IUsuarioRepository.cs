using Umbral.IdentityService.Domain.Entities;
using Umbral.IdentityService.Domain.ValueObjects;

namespace Umbral.IdentityService.Domain.Abstractions.Persistence;

public interface IUsuarioRepository
{
    Task<IReadOnlyList<Usuario>> GetAllAsync(CancellationToken cancellationToken);
    Task<Usuario?> GetByIdAsync(UsuarioLocalId userId, CancellationToken cancellationToken);

    /// <summary>
    /// Busca un usuario por su sub de OIDC (el `sub` del JWT). A diferencia de
    /// <see cref="GetByIdAsync"/> (que busca por el <c>UsuarioLocalId</c>), este método debe
    /// usarse siempre que el id disponible provenga del token o del mundo de equipos, que se
    /// indexa por sub (<c>ParticipanteEquipo.SubjectId</c>). Los dos espacios de id no se mezclan:
    /// por eso el local es un tipo propio y este toma un Guid crudo.
    /// </summary>
    Task<Usuario?> GetByKeycloakIdAsync(Guid keycloakId, CancellationToken cancellationToken);

    Task<bool> ExistsByEmailAsync(string email, UsuarioLocalId? excludingUserId, CancellationToken cancellationToken);
    Task AddAsync(Usuario usuario, CancellationToken cancellationToken);
    Task UpdateAsync(Usuario usuario, CancellationToken cancellationToken);

    /// <summary>
    /// Elimina un usuario local. Se usa para compensar la persistencia cuando el envío del
    /// correo de bienvenida falla tras haber creado el usuario.
    /// </summary>
    Task RemoveAsync(Usuario usuario, CancellationToken cancellationToken);
}
