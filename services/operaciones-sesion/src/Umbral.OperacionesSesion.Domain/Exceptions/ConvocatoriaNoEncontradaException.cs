namespace Umbral.OperacionesSesion.Domain.Exceptions;

public sealed class ConvocatoriaNoEncontradaException : Exception
{
    public ConvocatoriaNoEncontradaException(Guid convocatoriaId)
        : base($"No existe una convocatoria pendiente {convocatoriaId} para este usuario.") { }
}
