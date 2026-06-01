namespace Umbral.TeamService.Application.Exceptions;

public sealed class AccessCodeGenerationException : Exception
{
    public AccessCodeGenerationException(string message)
        : base(message)
    {
    }
}
