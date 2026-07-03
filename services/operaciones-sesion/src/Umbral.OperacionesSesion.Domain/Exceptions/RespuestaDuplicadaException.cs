namespace Umbral.OperacionesSesion.Domain.Exceptions;
public sealed class RespuestaDuplicadaException : Exception
{
    public RespuestaDuplicadaException(Guid participanteId)
        : base($"El participante {participanteId} ya respondió esta pregunta.") { }
}
