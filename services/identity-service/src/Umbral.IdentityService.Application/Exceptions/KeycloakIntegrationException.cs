namespace Umbral.IdentityService.Application.Exceptions;

public sealed class KeycloakIntegrationException : Exception
{
    public KeycloakIntegrationException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
