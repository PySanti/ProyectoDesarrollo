namespace Umbral.OperacionesSesion.Domain.Exceptions;

public sealed class PartidaNoCancelableException : Exception
{
    public PartidaNoCancelableException(Guid partidaId, string estado)
        : base($"La partida {partidaId} no puede cancelarse en estado {estado}.") { }
}
