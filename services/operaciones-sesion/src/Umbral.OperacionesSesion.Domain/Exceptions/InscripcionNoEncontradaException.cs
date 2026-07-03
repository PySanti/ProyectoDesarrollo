namespace Umbral.OperacionesSesion.Domain.Exceptions;

public sealed class InscripcionNoEncontradaException : Exception
{
    public InscripcionNoEncontradaException(Guid participanteId)
        : base($"El participante {participanteId} no tiene una inscripción activa en esta partida.") { }

    private InscripcionNoEncontradaException(string message) : base(message) { }

    public static InscripcionNoEncontradaException ParaEquipo(Guid equipoId) =>
        new($"El equipo {equipoId} no tiene una inscripción activa en esta partida.");
}
