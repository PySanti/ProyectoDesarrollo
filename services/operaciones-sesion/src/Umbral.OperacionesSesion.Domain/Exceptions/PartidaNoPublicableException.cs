namespace Umbral.OperacionesSesion.Domain.Exceptions;

public sealed class PartidaNoPublicableException : Exception
{
    public PartidaNoPublicableException(Guid partidaId)
        : base($"La partida {partidaId} no es publicable: requiere al menos un juego con orden contiguo desde 1.") { }
}
