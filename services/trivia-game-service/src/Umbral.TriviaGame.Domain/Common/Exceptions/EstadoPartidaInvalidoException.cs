namespace Umbral.TriviaGame.Domain.Common.Exceptions;

public sealed class EstadoPartidaInvalidoException : DomainValidationException
{
    public Guid PartidaId { get; }
    public string Mensaje { get; }

    public EstadoPartidaInvalidoException(Guid partidaId, string mensaje)
        : base($"Estado inválido de la partida '{partidaId}': {mensaje}")
    {
        PartidaId = partidaId;
        Mensaje = mensaje;
    }
}
