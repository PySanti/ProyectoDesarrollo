namespace Umbral.TeamService.Application.Exceptions;

public sealed class NoActiveTeamForParticipantException : Exception
{
    public NoActiveTeamForParticipantException(Guid userId)
        : base($"El participante {userId} no pertenece a ningun equipo activo.")
    {
    }
}
