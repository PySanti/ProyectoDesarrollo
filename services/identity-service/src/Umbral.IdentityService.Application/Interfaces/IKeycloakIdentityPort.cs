namespace Umbral.IdentityService.Application.Interfaces;

public interface IKeycloakIdentityPort
{
    Task<string> CreateUserWithInitialRoleAsync(
        string name,
        string email,
        string initialRole,
        string temporaryPassword,
        CancellationToken cancellationToken);

    /// <summary>
    /// Elimina un usuario en Keycloak. Se usa para compensar cuando un paso posterior de la
    /// creación falla (persistencia local o envío de correo). Debe ser idempotente: un usuario
    /// inexistente no es un error.
    /// </summary>
    Task DeleteUserAsync(string keycloakId, CancellationToken cancellationToken);

    /// <summary>
    /// Indica si el usuario todavía tiene una contraseña temporal pendiente, es decir, no ha
    /// completado el cambio de contraseña en su primer inicio de sesión (acción requerida
    /// <c>UPDATE_PASSWORD</c> aún presente en Keycloak).
    /// </summary>
    Task<bool> HasTemporaryPasswordAsync(string keycloakId, CancellationToken cancellationToken);

    /// <summary>
    /// Actualiza el correo del usuario en Keycloak (atributo <c>email</c>) para mantenerlo en
    /// sincronía con la edición administrativa.
    /// </summary>
    Task UpdateEmailAsync(string keycloakId, string email, CancellationToken cancellationToken);

    /// <summary>
    /// Restablece la contraseña del usuario a una nueva contraseña temporal (<c>temporary=true</c>),
    /// lo que vuelve a exigir el cambio en el siguiente inicio de sesión.
    /// </summary>
    Task ResetTemporaryPasswordAsync(string keycloakId, string temporaryPassword, CancellationToken cancellationToken);

    /// <summary>
    /// Agrega un rol compuesto (composite) a otro rol. Se usa para construir jerarquías de
    /// permisos funcionales (ej: agregar <c>GestionarPartidas</c> al rol <c>Operador</c>).
    /// Lanza <see cref="KeycloakIntegrationException"/> en fallo (502).
    /// </summary>
    Task AddCompositeToRoleAsync(string roleName, string compositeRoleName, CancellationToken cancellationToken);

    /// <summary>
    /// Quita un rol compuesto (composite) de otro rol. Tolerante a 404 (idempotencia del
    /// camino de reparación tras fallo parcial). Lanza <see cref="KeycloakIntegrationException"/>
    /// en otros fallos (502).
    /// </summary>
    Task RemoveCompositeFromRoleAsync(string roleName, string compositeRoleName, CancellationToken cancellationToken);

    /// <summary>
    /// Cambia el rol realm de un usuario: quita el rol viejo y asigna el nuevo. Tolerante a 404
    /// en la remoción del rol viejo (reintento tras fallo parcial). Lanza
    /// <see cref="KeycloakIntegrationException"/> en fallo (502).
    /// </summary>
    Task ChangeUserRealmRoleAsync(string keycloakId, string oldRoleName, string newRoleName, CancellationToken cancellationToken);
}
