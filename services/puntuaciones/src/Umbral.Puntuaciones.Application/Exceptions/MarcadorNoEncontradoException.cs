namespace Umbral.Puntuaciones.Application.Exceptions;

public sealed class MarcadorNoEncontradoException : Exception
{
    public MarcadorNoEncontradoException(Guid juegoId, Guid competidorId)
        : base($"No existe marcador del competidor {competidorId} en el juego {juegoId}.")
    {
    }
}
