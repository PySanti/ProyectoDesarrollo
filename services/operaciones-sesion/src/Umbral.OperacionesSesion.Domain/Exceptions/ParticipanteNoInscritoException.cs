namespace Umbral.OperacionesSesion.Domain.Exceptions;
public sealed class ParticipanteNoInscritoException : Exception
{
    public ParticipanteNoInscritoException(Guid participanteId)
        : base($"El participante {participanteId} no tiene inscripción activa en esta partida.") { }
}
