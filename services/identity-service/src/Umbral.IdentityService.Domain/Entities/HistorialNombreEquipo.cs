namespace Umbral.IdentityService.Domain.Entities;

public sealed class HistorialNombreEquipo
{
    public Guid Id { get; private set; }
    public Guid UsuarioId { get; private set; }
    public Guid EquipoId { get; private set; }
    public string NombreEquipo { get; private set; }
    public DateTime FechaRegistroUtc { get; private set; }

    private HistorialNombreEquipo()
    {
        NombreEquipo = string.Empty;
    }

    private HistorialNombreEquipo(Guid usuarioId, Guid equipoId, string nombreEquipo, DateTime fechaUtc)
    {
        if (usuarioId == Guid.Empty) throw new ArgumentException("UsuarioId requerido", nameof(usuarioId));
        if (equipoId == Guid.Empty) throw new ArgumentException("EquipoId requerido", nameof(equipoId));
        if (string.IsNullOrWhiteSpace(nombreEquipo)) throw new ArgumentException("NombreEquipo requerido", nameof(nombreEquipo));

        Id = Guid.NewGuid();
        UsuarioId = usuarioId;
        EquipoId = equipoId;
        NombreEquipo = nombreEquipo.Trim();
        FechaRegistroUtc = fechaUtc;
    }

    public static HistorialNombreEquipo Registrar(Guid usuarioId, Guid equipoId, string nombreEquipo, DateTime fechaUtc)
        => new(usuarioId, equipoId, nombreEquipo, fechaUtc);
}
