namespace Umbral.IdentityService.Application.Exceptions;

public sealed class EquipoConParticipacionActivaException : Exception
{
    public Guid EquipoId { get; }

    public EquipoConParticipacionActivaException(Guid equipoId)
        : base($"El equipo {equipoId} participa en una partida en Lobby/Iniciada y no puede eliminarse.")
        => EquipoId = equipoId;
}
