namespace Umbral.IdentityService.Application.Exceptions;

/// <summary>
/// Se lanza cuando no es posible entregar el correo de bienvenida al usuario creado.
/// El handler la trata como fallo de la operación y compensa la creación (Keycloak + local).
/// </summary>
public sealed class EmailDeliveryException : Exception
{
    public EmailDeliveryException(string message) : base(message)
    {
    }

    public EmailDeliveryException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
