namespace Umbral.Partidas.Domain.Exceptions;

public sealed class OrdenJuegosNoContiguoException : Exception
{
    public OrdenJuegosNoContiguoException(Guid partidaId)
        : base($"El orden de los juegos de la partida {partidaId} debe ser una secuencia contigua desde 1.") { }
}
