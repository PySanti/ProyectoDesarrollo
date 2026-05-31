namespace Umbral.TriviaGame.Domain.Common.Exceptions;

public sealed class JugadorYaInscritoException : DomainValidationException
{
    public string UsuarioId { get; }

    public JugadorYaInscritoException(string usuarioId)
        : base($"El usuario '{usuarioId}' ya se encuentra inscrito en esta partida.")
    {
        UsuarioId = usuarioId;
    }
}
