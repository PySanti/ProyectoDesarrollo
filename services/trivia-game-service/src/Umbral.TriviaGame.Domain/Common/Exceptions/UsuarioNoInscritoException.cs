namespace Umbral.TriviaGame.Domain.Common.Exceptions;

public sealed class UsuarioNoInscritoException : DomainValidationException
{
    public string UsuarioId { get; }
    public Guid PartidaId { get; }

    public UsuarioNoInscritoException(string usuarioId, Guid partidaId)
        : base($"El usuario '{usuarioId}' no está inscrito en la partida '{partidaId}'.")
    {
        UsuarioId = usuarioId;
        PartidaId = partidaId;
    }
}
