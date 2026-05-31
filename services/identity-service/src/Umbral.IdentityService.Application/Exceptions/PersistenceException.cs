namespace Umbral.IdentityService.Application.Exceptions;

public sealed class PersistenceException : Exception
{
    public PersistenceException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
