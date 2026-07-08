namespace Umbral.Puntuaciones.Application.Exceptions;

public sealed class PartidaNoEncontradaException : Exception
{
    public PartidaNoEncontradaException(Guid partidaId)
        : base($"No se encontró la partida {partidaId} en las proyecciones de Puntuaciones.")
    {
    }
}
