namespace Umbral.IdentityService.Application.Exceptions;

public sealed class TransferirLiderazgoConflictException : Exception
{
    public TransferirLiderazgoConflictException(string message)
        : base(message)
    {
    }
}
