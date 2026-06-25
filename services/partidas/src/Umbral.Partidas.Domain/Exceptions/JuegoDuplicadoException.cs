namespace Umbral.Partidas.Domain.Exceptions;

public sealed class JuegoDuplicadoException : Exception
{
    public JuegoDuplicadoException(Guid juegoId)
        : base($"El juego {juegoId} ya pertenece a la partida.") { }
}
