namespace Umbral.Puntuaciones.Domain.Exceptions;

public sealed class PuntuacionInvalidaException : Exception
{
    public PuntuacionInvalidaException(string message) : base(message)
    {
    }
}
