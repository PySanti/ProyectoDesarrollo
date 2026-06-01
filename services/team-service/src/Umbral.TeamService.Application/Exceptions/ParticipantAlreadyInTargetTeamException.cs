namespace Umbral.TeamService.Application.Exceptions;

public sealed class ParticipantAlreadyInTargetTeamException : Exception
{
    public ParticipantAlreadyInTargetTeamException(Guid teamId, Guid userId)
        : base($"El participante {userId} ya pertenece al equipo {teamId}.")
    {
    }
}
