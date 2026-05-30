namespace Umbral.TriviaGame.Domain.Common;

public sealed class DomainValidationException : Exception
{
    public DomainValidationException(string message)
        : base(message)
    {
    }
}
