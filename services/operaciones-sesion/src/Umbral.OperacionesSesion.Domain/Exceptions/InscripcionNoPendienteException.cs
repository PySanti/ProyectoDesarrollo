namespace Umbral.OperacionesSesion.Domain.Exceptions;

public sealed class InscripcionNoPendienteException : Exception
{
    public InscripcionNoPendienteException(Guid inscripcionId)
        : base($"La inscripción {inscripcionId} no está pendiente de aprobación.") { }
}
