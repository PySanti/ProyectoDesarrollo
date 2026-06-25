namespace Umbral.Partidas.Domain.Exceptions;
public sealed class JuegoTriviaSinPreguntasException : Exception
{
    public JuegoTriviaSinPreguntasException()
        : base("Un JuegoTrivia debe tener al menos una pregunta.") { }
}
