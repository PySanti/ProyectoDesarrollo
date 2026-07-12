using Umbral.IdentityService.Domain.Entities;

namespace Umbral.IdentityService.Domain.Abstractions.Persistence;

public interface IUsuarioRepository
{
    Task<IReadOnlyList<Usuario>> GetAllAsync(CancellationToken cancellationToken);
    Task<Usuario?> GetByIdAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Busca un usuario por su identificador de Keycloak (el `sub` del JWT). A diferencia de
    /// <see cref="GetByIdAsync"/> (que busca por el <c>UsuarioId</c> local), este método debe
    /// usarse siempre que el id disponible provenga del espacio de membresía/JWT — por ejemplo,
    /// los miembros de un <c>Equipo</c>, que se indexan por KeycloakId (ver
    /// <c>Equipo.EliminarPorLider</c>/<c>EliminarPorAdmin</c>/<c>ReasignarLiderazgoPorAdmin</c>).
    /// </summary>
    Task<Usuario?> GetByKeycloakIdAsync(Guid keycloakId, CancellationToken cancellationToken);

    Task<bool> ExistsByEmailAsync(string email, Guid? excludingUserId, CancellationToken cancellationToken);
    Task AddAsync(Usuario usuario, CancellationToken cancellationToken);
    Task UpdateAsync(Usuario usuario, CancellationToken cancellationToken);

    /// <summary>
    /// Elimina un usuario local. Se usa para compensar la persistencia cuando el envío del
    /// correo de bienvenida falla tras haber creado el usuario.
    /// </summary>
    Task RemoveAsync(Usuario usuario, CancellationToken cancellationToken);
}
