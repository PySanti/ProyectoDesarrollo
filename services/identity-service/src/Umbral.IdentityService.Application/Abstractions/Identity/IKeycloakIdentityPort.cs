namespace Umbral.IdentityService.Application.Abstractions.Identity;

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
}
