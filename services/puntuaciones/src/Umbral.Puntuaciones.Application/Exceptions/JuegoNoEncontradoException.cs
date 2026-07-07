namespace Umbral.Puntuaciones.Application.Exceptions;

public sealed class JuegoNoEncontradoException : Exception
{
    public JuegoNoEncontradoException(Guid juegoId)
        : base($"No se encontró el juego {juegoId} en las proyecciones de Puntuaciones.")
    {
    }
}
