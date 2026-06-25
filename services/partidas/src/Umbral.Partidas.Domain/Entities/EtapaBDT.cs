using Umbral.Partidas.Domain.Exceptions;
using Umbral.Partidas.Domain.ValueObjects;

namespace Umbral.Partidas.Domain.Entities;

public sealed class EtapaBDT
{
    public Guid EtapaBDTId { get; private set; }
    public int Orden { get; private set; }
    public string CodigoQREsperado { get; private set; } = string.Empty;
    public PuntajeAsignado PuntajeAsignado { get; private set; }
    public int TiempoLimiteSegundos { get; private set; }

    private EtapaBDT() { } // EF

    internal static EtapaBDT Crear(int orden, string codigoQr, int puntaje, int tiempoLimiteSegundos)
    {
        if (orden < 1)
            throw new EtapaBDTInvalidaException("el orden debe ser mayor o igual a 1.");
        if (string.IsNullOrWhiteSpace(codigoQr))
            throw new EtapaBDTInvalidaException("el codigo QR esperado es requerido.");
        if (tiempoLimiteSegundos <= 0)
            throw new EtapaBDTInvalidaException("el tiempo limite debe ser positivo.");

        PuntajeAsignado puntajeVo;
        try
        {
            puntajeVo = PuntajeAsignado.Crear(puntaje);
        }
        catch (ArgumentException ex)
        {
            throw new EtapaBDTInvalidaException(ex.Message);
        }

        return new EtapaBDT
        {
            EtapaBDTId = Guid.NewGuid(),
            Orden = orden,
            CodigoQREsperado = codigoQr.Trim(),
            PuntajeAsignado = puntajeVo,
            TiempoLimiteSegundos = tiempoLimiteSegundos
        };
    }
}
