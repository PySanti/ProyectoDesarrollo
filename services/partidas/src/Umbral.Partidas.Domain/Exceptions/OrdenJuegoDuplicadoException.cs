namespace Umbral.Partidas.Domain.Exceptions;

public sealed class OrdenJuegoDuplicadoException : Exception
{
    public OrdenJuegoDuplicadoException(int orden)
        : base($"Ya existe un juego con el orden {orden} en la partida.") { }
}
