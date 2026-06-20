namespace Umbral.IdentityService.Application.Abstractions.Notifications;

/// <summary>
/// Envía el correo de bienvenida con la contraseña temporal a un usuario recién creado.
/// La implementación de infraestructura debe lanzar <c>EmailDeliveryException</c> ante
/// cualquier fallo para que el handler compense la creación.
/// </summary>
public interface IUserWelcomeEmailSender
{
    Task SendWelcomeEmailAsync(UserWelcomeEmailMessage message, CancellationToken cancellationToken);
}

public sealed record UserWelcomeEmailMessage(
    string Name,
    string Email,
    string Role,
    string TemporaryPassword);
