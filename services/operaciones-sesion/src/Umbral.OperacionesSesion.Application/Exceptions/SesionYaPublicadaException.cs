namespace Umbral.OperacionesSesion.Application.Exceptions;

public sealed class SesionYaPublicadaException : Exception
{
    public SesionYaPublicadaException(Guid partidaId) : base($"La partida {partidaId} ya fue publicada.") { }
}
