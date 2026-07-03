namespace Umbral.OperacionesSesion.Application.Exceptions;

public sealed class SesionNoEncontradaException : Exception
{
    public SesionNoEncontradaException(Guid partidaId)
        : base($"No existe una sesión publicada para la partida {partidaId}.") { }
}
