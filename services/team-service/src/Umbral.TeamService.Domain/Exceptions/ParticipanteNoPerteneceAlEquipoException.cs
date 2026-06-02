namespace Umbral.TeamService.Domain.Exceptions;

public sealed class ParticipanteNoPerteneceAlEquipoException : Exception
{
    public ParticipanteNoPerteneceAlEquipoException(Guid usuarioId)
        : base($"El participante {usuarioId} no pertenece al equipo.")
    {
    }
}
