namespace Umbral.TriviaGame.Domain.Common.Exceptions;

public sealed class RespuestaTardiaException : DomainValidationException
{
    public int TimeLimitSegundos { get; }

    public RespuestaTardiaException(int timeLimitSegundos)
        : base($"El tiempo límite de {timeLimitSegundos} segundos para responder ha expirado.")
    {
        TimeLimitSegundos = timeLimitSegundos;
    }
}
