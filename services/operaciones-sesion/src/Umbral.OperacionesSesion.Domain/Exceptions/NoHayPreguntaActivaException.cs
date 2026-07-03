namespace Umbral.OperacionesSesion.Domain.Exceptions;
public sealed class NoHayPreguntaActivaException : Exception
{
    public NoHayPreguntaActivaException(Guid partidaId)
        : base($"No hay una pregunta activa en la partida {partidaId}.") { }
}
