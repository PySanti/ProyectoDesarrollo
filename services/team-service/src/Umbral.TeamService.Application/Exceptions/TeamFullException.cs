namespace Umbral.TeamService.Application.Exceptions;

public sealed class TeamFullException : Exception
{
    public TeamFullException(Guid teamId)
        : base($"El equipo {teamId} ya alcanzo el maximo de 5 integrantes.")
    {
    }
}
