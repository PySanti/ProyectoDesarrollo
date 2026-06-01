namespace Umbral.TriviaGame.Domain.Common.Exceptions;

public sealed class PreguntaNoActivaException : DomainValidationException
{
    public PreguntaNoActivaException()
        : base("No hay una pregunta activa en este momento.") { }

    public PreguntaNoActivaException(Guid partidaId)
        : base($"La partida '{partidaId}' no tiene una pregunta activa en este momento.") { }
}
