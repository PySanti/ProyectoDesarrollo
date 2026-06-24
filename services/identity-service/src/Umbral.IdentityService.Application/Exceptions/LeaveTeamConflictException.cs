namespace Umbral.IdentityService.Application.Exceptions;

public sealed class LeaveTeamConflictException : Exception
{
    public LeaveTeamConflictException(string message)
        : base(message)
    {
    }
}
