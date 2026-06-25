namespace Umbral.Partidas.Domain.Exceptions;

public sealed class PartidaSinJuegosException : Exception
{
    public PartidaSinJuegosException(Guid partidaId)
        : base($"La partida {partidaId} no tiene juegos.") { }
}
