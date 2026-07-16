namespace Umbral.IdentityService.Domain.Entities;

public sealed class HistorialNombreEquipo
{
    public Guid Id { get; private set; }

    // Sub de OIDC, no UsuarioId local. Ver ParticipanteEquipo.SubjectId.
    public Guid SubjectId { get; private set; }
    public Guid EquipoId { get; private set; }
    public string NombreEquipo { get; private set; }
    public DateTime FechaRegistroUtc { get; private set; }

    private HistorialNombreEquipo()
    {
        NombreEquipo = string.Empty;
    }

    private HistorialNombreEquipo(Guid subjectId, Guid equipoId, string nombreEquipo, DateTime fechaUtc)
    {
        if (subjectId == Guid.Empty) throw new ArgumentException("SubjectId requerido", nameof(subjectId));
        if (equipoId == Guid.Empty) throw new ArgumentException("EquipoId requerido", nameof(equipoId));
        if (string.IsNullOrWhiteSpace(nombreEquipo)) throw new ArgumentException("NombreEquipo requerido", nameof(nombreEquipo));

        Id = Guid.NewGuid();
        SubjectId = subjectId;
        EquipoId = equipoId;
        NombreEquipo = nombreEquipo.Trim();
        FechaRegistroUtc = fechaUtc;
    }

    public static HistorialNombreEquipo Registrar(Guid subjectId, Guid equipoId, string nombreEquipo, DateTime fechaUtc)
        => new(subjectId, equipoId, nombreEquipo, fechaUtc);
}
