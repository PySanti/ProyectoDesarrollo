namespace Umbral.TeamService.Application.Exceptions;

public sealed class TeamNotFoundByAccessCodeException : Exception
{
    public TeamNotFoundByAccessCodeException(string accessCode)
        : base($"No existe un equipo activo para el codigo de acceso '{accessCode}'.")
    {
    }
}
