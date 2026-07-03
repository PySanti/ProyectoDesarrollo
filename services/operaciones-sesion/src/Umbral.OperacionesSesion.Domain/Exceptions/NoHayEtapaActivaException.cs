namespace Umbral.OperacionesSesion.Domain.Exceptions;
public sealed class NoHayEtapaActivaException : Exception
{
    public NoHayEtapaActivaException(Guid partidaId)
        : base($"No hay una etapa activa en la partida {partidaId}.") { }
}
