namespace Umbral.OperacionesSesion.Application.Exceptions;

public sealed class PartidasConfigInaccesibleException : Exception
{
    public PartidasConfigInaccesibleException(Guid partidaId, Exception? inner = null)
        : base($"El servicio Partidas no respondió la configuración de la partida {partidaId}.", inner) { }
}
