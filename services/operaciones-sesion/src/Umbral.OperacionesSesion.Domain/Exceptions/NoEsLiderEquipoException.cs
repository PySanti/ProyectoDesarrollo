namespace Umbral.OperacionesSesion.Domain.Exceptions;

public sealed class NoEsLiderEquipoException : Exception
{
    public NoEsLiderEquipoException(Guid equipoId)
        : base($"El usuario no es líder del equipo {equipoId}.") { }
}
