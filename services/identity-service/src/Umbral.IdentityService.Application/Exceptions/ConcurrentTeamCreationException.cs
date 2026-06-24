namespace Umbral.IdentityService.Application.Exceptions;

public sealed class ConcurrentTeamCreationException : Exception
{
    public ConcurrentTeamCreationException(Guid userId)
        : base($"No fue posible crear el equipo porque el participante {userId} ya quedo asociado a un equipo activo en una operacion concurrente.")
    {
    }
}
