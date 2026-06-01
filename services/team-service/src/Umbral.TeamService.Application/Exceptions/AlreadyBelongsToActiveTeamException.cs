namespace Umbral.TeamService.Application.Exceptions;

public sealed class AlreadyBelongsToActiveTeamException : Exception
{
    public AlreadyBelongsToActiveTeamException(Guid userId)
        : base($"El participante {userId} ya pertenece a un equipo activo.")
    {
    }
}
