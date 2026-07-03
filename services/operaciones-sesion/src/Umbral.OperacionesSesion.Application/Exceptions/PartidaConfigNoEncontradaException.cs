namespace Umbral.OperacionesSesion.Application.Exceptions;

public sealed class PartidaConfigNoEncontradaException : Exception
{
    public PartidaConfigNoEncontradaException(Guid partidaId)
        : base($"No existe configuración para la partida {partidaId}.") { }
}
