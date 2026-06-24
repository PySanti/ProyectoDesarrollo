namespace Umbral.IdentityService.Domain.Exceptions;

public sealed class NuevoLiderNoPerteneceAlEquipoException : Exception
{
    public NuevoLiderNoPerteneceAlEquipoException(Guid usuarioId)
        : base($"El nuevo lider {usuarioId} no pertenece al equipo.")
    {
    }
}
