namespace Umbral.IdentityService.Domain.Entities;

public sealed class ParticipanteEquipo
{
    public Guid ParticipanteEquipoId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public DateTime FechaUnionUtc { get; private set; }
    public bool EsLider { get; private set; }

    private ParticipanteEquipo()
    {
    }

    private ParticipanteEquipo(Guid usuarioId, bool esLider)
    {
        if (usuarioId == Guid.Empty)
        {
            throw new ArgumentException("UsuarioId requerido", nameof(usuarioId));
        }

        ParticipanteEquipoId = Guid.NewGuid();
        UsuarioId = usuarioId;
        FechaUnionUtc = DateTime.UtcNow;
        EsLider = esLider;
    }

    public static ParticipanteEquipo CrearCreador(Guid usuarioId)
    {
        return new ParticipanteEquipo(usuarioId, true);
    }

    public static ParticipanteEquipo CrearIntegrante(Guid usuarioId)
    {
        return new ParticipanteEquipo(usuarioId, false);
    }

    public void MarcarComoLider()
    {
        EsLider = true;
    }

    public void QuitarLiderazgo()
    {
        EsLider = false;
    }
}
