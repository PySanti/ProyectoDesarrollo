namespace Umbral.TriviaGame.Domain.Common.Exceptions;

public sealed class CupoLlenoException : DomainValidationException
{
    public int MaximoJugadores { get; }

    public CupoLlenoException(int maximoJugadores)
        : base($"La partida ha alcanzado el máximo de {maximoJugadores} jugadores.")
    {
        MaximoJugadores = maximoJugadores;
    }
}
