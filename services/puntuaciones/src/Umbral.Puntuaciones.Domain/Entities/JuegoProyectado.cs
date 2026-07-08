using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.Domain.Entities;

// Registro informativo de un juego activado (fuente: JuegoActivado).
public sealed class JuegoProyectado
{
    private JuegoProyectado(Guid juegoId, Guid partidaId, int orden, TipoJuego tipoJuego)
    {
        JuegoId = juegoId;
        PartidaId = partidaId;
        Orden = orden;
        TipoJuego = tipoJuego;
    }

    public Guid JuegoId { get; private set; }
    public Guid PartidaId { get; private set; }
    public int Orden { get; private set; }
    public TipoJuego TipoJuego { get; private set; }

    public static JuegoProyectado Desde(Guid juegoId, Guid partidaId, int orden, TipoJuego tipoJuego)
        => new(juegoId, partidaId, orden, tipoJuego);
}
