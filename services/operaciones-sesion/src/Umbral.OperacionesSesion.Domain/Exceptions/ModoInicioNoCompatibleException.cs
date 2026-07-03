namespace Umbral.OperacionesSesion.Domain.Exceptions;

public sealed class ModoInicioNoCompatibleException : Exception
{
    public ModoInicioNoCompatibleException(Guid partidaId)
        : base($"El modo de inicio de la partida {partidaId} no permite esta acción.") { }
}
