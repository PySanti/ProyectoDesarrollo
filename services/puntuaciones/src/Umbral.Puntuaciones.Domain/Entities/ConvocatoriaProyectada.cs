namespace Umbral.Puntuaciones.Domain.Entities;

// Vínculo miembro↔equipo↔partida. Hacen falta los dos eventos: ConvocatoriaRespondida sabe quién
// aceptó pero NO de qué equipo, y ConvocatoriaCreada sabe el equipo pero no si aceptó. Se unen por
// ConvocatoriaId.
public sealed class ConvocatoriaProyectada
{
    private ConvocatoriaProyectada(Guid convocatoriaId, Guid partidaId, Guid equipoId, Guid usuarioId)
    {
        ConvocatoriaId = convocatoriaId;
        PartidaId = partidaId;
        EquipoId = equipoId;
        UsuarioId = usuarioId;
        Aceptada = false;
    }

    private ConvocatoriaProyectada() { } // EF

    public Guid ConvocatoriaId { get; private set; }
    public Guid PartidaId { get; private set; }
    public Guid EquipoId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public bool Aceptada { get; private set; }

    public static ConvocatoriaProyectada Nueva(Guid convocatoriaId, Guid partidaId, Guid equipoId, Guid usuarioId)
        => new(convocatoriaId, partidaId, equipoId, usuarioId);

    public void Responder(bool aceptada) => Aceptada = aceptada;
}
