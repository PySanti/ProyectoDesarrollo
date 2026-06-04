namespace Umbral.TriviaGame.Domain.Common.Exceptions;

public sealed class ModoInicioAutomaticoException : DomainValidationException
{
    public ModoInicioAutomaticoException()
        : base("No se puede iniciar manualmente una partida configurada solo con inicio automático.") { }
}