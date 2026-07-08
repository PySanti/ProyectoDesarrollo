namespace Umbral.IdentityService.Domain.Entities;

public sealed class ParticipacionActivaEquipo
{
    public Guid EquipoId { get; private set; }
    public Guid PartidaId { get; private set; }
    public DateTime FechaRegistroUtc { get; private set; }

    private ParticipacionActivaEquipo()
    {
    }

    private ParticipacionActivaEquipo(Guid equipoId, Guid partidaId, DateTime fechaUtc)
    {
        if (equipoId == Guid.Empty) throw new ArgumentException("EquipoId requerido", nameof(equipoId));
        if (partidaId == Guid.Empty) throw new ArgumentException("PartidaId requerido", nameof(partidaId));

        EquipoId = equipoId;
        PartidaId = partidaId;
        FechaRegistroUtc = fechaUtc;
    }

    public static ParticipacionActivaEquipo Registrar(Guid equipoId, Guid partidaId, DateTime fechaUtc)
        => new(equipoId, partidaId, fechaUtc);
}
