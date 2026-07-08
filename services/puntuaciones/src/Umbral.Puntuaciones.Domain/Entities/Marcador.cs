using Umbral.Puntuaciones.Domain.Enums;
using Umbral.Puntuaciones.Domain.Exceptions;

namespace Umbral.Puntuaciones.Domain.Entities;

// Acumulado de un competidor (participante o equipo) en un juego.
// La acumulación es conmutativa: el orden de llegada de eventos no altera el total.
public sealed class Marcador
{
    private Marcador(Guid juegoId, Guid competidorId, Guid partidaId, TipoCompetidor tipoCompetidor)
    {
        JuegoId = juegoId;
        CompetidorId = competidorId;
        PartidaId = partidaId;
        TipoCompetidor = tipoCompetidor;
    }

    public Guid JuegoId { get; private set; }
    public Guid CompetidorId { get; private set; }
    public Guid PartidaId { get; private set; }
    public TipoCompetidor TipoCompetidor { get; private set; }
    public int PuntosAcumulados { get; private set; }
    public long TiempoAcumuladoMs { get; private set; }
    public int UnidadesGanadas { get; private set; }

    public static Marcador Nuevo(Guid juegoId, Guid competidorId, Guid partidaId, TipoCompetidor tipoCompetidor)
        => new(juegoId, competidorId, partidaId, tipoCompetidor);

    public void Acreditar(int puntos, long tiempoMs)
    {
        if (puntos < 0)
        {
            throw new PuntuacionInvalidaException("El puntaje acreditado no puede ser negativo.");
        }
        if (tiempoMs < 0)
        {
            throw new PuntuacionInvalidaException("El tiempo acreditado no puede ser negativo.");
        }

        PuntosAcumulados += puntos;
        TiempoAcumuladoMs += tiempoMs;
        UnidadesGanadas += 1;
    }
}
