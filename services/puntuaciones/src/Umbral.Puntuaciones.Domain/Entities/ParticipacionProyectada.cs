using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.Domain.Entities;

// Quién compite en una partida, con independencia de si anotó. Antes de esta proyección el único
// universo de competidores era el de marcadores, y un marcador solo nace al acreditar puntos: quien
// no puntuaba no existía. La alimenta InscripcionAceptada.
public sealed class ParticipacionProyectada
{
    private ParticipacionProyectada(Guid partidaId, Guid competidorId, TipoCompetidor tipoCompetidor)
    {
        PartidaId = partidaId;
        CompetidorId = competidorId;
        TipoCompetidor = tipoCompetidor;
    }

    private ParticipacionProyectada() { } // EF

    public Guid PartidaId { get; private set; }
    public Guid CompetidorId { get; private set; }
    public TipoCompetidor TipoCompetidor { get; private set; }

    public static ParticipacionProyectada Nueva(Guid partidaId, Guid competidorId, TipoCompetidor tipoCompetidor)
        => new(partidaId, competidorId, tipoCompetidor);
}
