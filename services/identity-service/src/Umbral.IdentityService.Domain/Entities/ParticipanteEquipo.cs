namespace Umbral.IdentityService.Domain.Entities;

public sealed class ParticipanteEquipo
{
    public Guid ParticipanteEquipoId { get; private set; }

    // El sub de OIDC (hoy Keycloak), no el UsuarioId local de la tabla usuarios: son dos Guid
    // sin relacion. Con este id llega el actor en el token.
    public Guid SubjectId { get; private set; }
    public DateTime FechaUnionUtc { get; private set; }
    public bool EsLider { get; private set; }

    private ParticipanteEquipo()
    {
    }

    private ParticipanteEquipo(Guid subjectId, bool esLider)
    {
        if (subjectId == Guid.Empty)
        {
            throw new ArgumentException("SubjectId requerido", nameof(subjectId));
        }

        ParticipanteEquipoId = Guid.NewGuid();
        SubjectId = subjectId;
        FechaUnionUtc = DateTime.UtcNow;
        EsLider = esLider;
    }

    public static ParticipanteEquipo CrearCreador(Guid subjectId)
    {
        return new ParticipanteEquipo(subjectId, true);
    }

    public static ParticipanteEquipo CrearIntegrante(Guid subjectId)
    {
        return new ParticipanteEquipo(subjectId, false);
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
