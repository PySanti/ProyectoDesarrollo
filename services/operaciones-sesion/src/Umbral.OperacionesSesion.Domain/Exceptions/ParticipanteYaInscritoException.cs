namespace Umbral.OperacionesSesion.Domain.Exceptions;

public sealed class ParticipanteYaInscritoException : Exception
{
    public ParticipanteYaInscritoException(Guid participanteId)
        : base($"El participante {participanteId} ya está inscrito activamente en esta partida.") { }
}
