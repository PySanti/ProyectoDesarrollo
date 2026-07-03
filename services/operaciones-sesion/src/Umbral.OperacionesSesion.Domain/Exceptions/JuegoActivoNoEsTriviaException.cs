namespace Umbral.OperacionesSesion.Domain.Exceptions;
public sealed class JuegoActivoNoEsTriviaException : Exception
{
    public JuegoActivoNoEsTriviaException(Guid partidaId)
        : base($"El juego activo de la partida {partidaId} no es de tipo Trivia.") { }
}
