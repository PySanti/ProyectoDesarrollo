namespace Umbral.TriviaGame.Domain.Common.Exceptions;

public sealed class PartidaTriviaNotFoundException : DomainValidationException
{
    public Guid PartidaId { get; }

    public PartidaTriviaNotFoundException(Guid partidaId)
        : base($"No se encontró una partida de Trivia con el identificador '{partidaId}'.")
    {
        PartidaId = partidaId;
    }
}
