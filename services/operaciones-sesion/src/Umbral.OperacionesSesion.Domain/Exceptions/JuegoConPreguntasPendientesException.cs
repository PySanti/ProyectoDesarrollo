namespace Umbral.OperacionesSesion.Domain.Exceptions;
public sealed class JuegoConPreguntasPendientesException : Exception
{
    public JuegoConPreguntasPendientesException(Guid partidaId)
        : base($"El juego Trivia de la partida {partidaId} aún tiene preguntas abiertas.") { }
}
