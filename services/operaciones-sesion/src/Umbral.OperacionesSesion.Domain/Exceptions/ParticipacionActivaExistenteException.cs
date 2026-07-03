namespace Umbral.OperacionesSesion.Domain.Exceptions;

public sealed class ParticipacionActivaExistenteException : Exception
{
    public ParticipacionActivaExistenteException(Guid participanteId)
        : base($"El participante {participanteId} ya tiene una participación activa en otra partida.") { }
}
