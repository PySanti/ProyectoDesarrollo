namespace Umbral.TriviaGame.Domain.Common.Exceptions;

public sealed class InvalidStateTransitionException : DomainValidationException
{
    public string FromState { get; }
    public string ToState { get; }

    public InvalidStateTransitionException(string fromState, string toState)
        : base($"No se puede cambiar el estado de '{fromState}' a '{toState}'.")
    {
        FromState = fromState;
        ToState = toState;
    }
}
