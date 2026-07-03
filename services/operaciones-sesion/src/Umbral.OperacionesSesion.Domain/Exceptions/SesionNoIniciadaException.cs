namespace Umbral.OperacionesSesion.Domain.Exceptions;

public sealed class SesionNoIniciadaException : Exception
{
    public SesionNoIniciadaException(Guid partidaId)
        : base($"La sesión de la partida {partidaId} no está iniciada.") { }
}
