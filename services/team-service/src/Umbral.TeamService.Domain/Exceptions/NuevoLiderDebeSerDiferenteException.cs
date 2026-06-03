namespace Umbral.TeamService.Domain.Exceptions;

public sealed class NuevoLiderDebeSerDiferenteException : Exception
{
    public NuevoLiderDebeSerDiferenteException(Guid usuarioId)
        : base($"El nuevo lider {usuarioId} debe ser diferente al lider actual.")
    {
    }
}
