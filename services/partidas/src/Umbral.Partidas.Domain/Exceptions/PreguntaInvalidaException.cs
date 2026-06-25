namespace Umbral.Partidas.Domain.Exceptions;
public sealed class PreguntaInvalidaException : Exception
{
    public PreguntaInvalidaException(string motivo)
        : base($"Pregunta invalida: {motivo}") { }
}
