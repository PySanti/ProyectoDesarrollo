namespace Umbral.IdentityService.Application.Exceptions;

public sealed class UniqueMembershipConflictException : Exception
{
    public UniqueMembershipConflictException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
