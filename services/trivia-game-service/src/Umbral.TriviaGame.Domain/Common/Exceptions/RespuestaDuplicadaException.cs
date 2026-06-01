namespace Umbral.TriviaGame.Domain.Common.Exceptions;

public sealed class RespuestaDuplicadaException : DomainValidationException
{
    public string UsuarioId { get; }
    public Guid PreguntaId { get; }

    public RespuestaDuplicadaException(string usuarioId, Guid preguntaId)
        : base($"El usuario '{usuarioId}' ya respondió la pregunta '{preguntaId}'.")
    {
        UsuarioId = usuarioId;
        PreguntaId = preguntaId;
    }
}
