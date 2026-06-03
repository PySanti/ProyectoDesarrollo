namespace Umbral.TeamService.Application.Exceptions;

public sealed class TransferirLiderazgoConflictException : Exception
{
    public TransferirLiderazgoConflictException(string message)
        : base(message)
    {
    }
}
