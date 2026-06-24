namespace Umbral.IdentityService.Domain.Exceptions;

public sealed class LiderDebeTransferirLiderazgoException : Exception
{
    public LiderDebeTransferirLiderazgoException(Guid usuarioId)
        : base($"El lider {usuarioId} debe transferir el liderazgo antes de salir del equipo.")
    {
    }
}
