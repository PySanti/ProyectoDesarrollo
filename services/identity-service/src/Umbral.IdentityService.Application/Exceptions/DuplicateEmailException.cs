namespace Umbral.IdentityService.Application.Exceptions;

public sealed class DuplicateEmailException : Exception
{
    public DuplicateEmailException(string email)
        : base($"Email '{email}' already exists")
    {
    }
}
