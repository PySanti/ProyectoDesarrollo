using Umbral.Partidas.Domain.Enums;
using Umbral.Partidas.Domain.ValueObjects;

namespace Umbral.Partidas.Domain.Entities;

public sealed class JuegoReferencia
{
    public JuegoId JuegoId { get; private set; }
    public int Orden { get; private set; }
    public TipoJuego TipoJuego { get; private set; }

    private JuegoReferencia() { } // EF

    internal JuegoReferencia(JuegoId juegoId, int orden, TipoJuego tipoJuego)
    {
        JuegoId = juegoId;
        Orden = orden;
        TipoJuego = tipoJuego;
    }
}
