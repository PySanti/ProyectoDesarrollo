namespace Umbral.Partidas.Application.Exceptions;

public sealed class PartidaNoEncontradaException : Exception
{
    public PartidaNoEncontradaException(Guid partidaId)
        : base($"No existe la partida {partidaId}.") { }
}
