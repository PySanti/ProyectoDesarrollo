namespace Umbral.OperacionesSesion.Domain.Exceptions;

public sealed class CupoLlenoException : Exception
{
    public CupoLlenoException(Guid partidaId)
        : base($"La partida {partidaId} alcanzó el máximo de participación.") { }
}
