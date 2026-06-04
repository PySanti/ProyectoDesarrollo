using Umbral.BdtGameService.Domain.Enums;

namespace Umbral.BdtGameService.Domain.Entities;

public sealed class EtapaBDT
{
    public Guid EtapaId { get; private set; }
    public int Orden { get; private set; }
    public string CodigoQREsperado { get; private set; }
    public int TiempoLimiteSegundos { get; private set; }
    public EstadoEtapa Estado { get; private set; }
    public DateTime? IniciadaEnUtc { get; private set; }
    public DateTime? CierraEnUtc { get; private set; }

    private EtapaBDT()
    {
        CodigoQREsperado = string.Empty;
    }

    private EtapaBDT(int orden, string codigoQREsperado, int tiempoLimiteSegundos)
    {
        if (orden <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(orden), "El orden de la etapa debe ser mayor que cero.");
        }

        if (string.IsNullOrWhiteSpace(codigoQREsperado))
        {
            throw new ArgumentException("CodigoQREsperado requerido", nameof(codigoQREsperado));
        }

        if (tiempoLimiteSegundos <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tiempoLimiteSegundos), "El tiempo limite debe ser mayor que cero.");
        }

        EtapaId = Guid.NewGuid();
        Orden = orden;
        CodigoQREsperado = codigoQREsperado.Trim();
        TiempoLimiteSegundos = tiempoLimiteSegundos;
        Estado = EstadoEtapa.Pendiente;
    }

    public static EtapaBDT Crear(int orden, string codigoQREsperado, int tiempoLimiteSegundos)
    {
        return new EtapaBDT(orden, codigoQREsperado, tiempoLimiteSegundos);
    }

    public void Activar(DateTime iniciadaEnUtc)
    {
        if (iniciadaEnUtc == default)
        {
            throw new ArgumentException("Fecha de inicio de etapa requerida.", nameof(iniciadaEnUtc));
        }

        if (Estado != EstadoEtapa.Pendiente)
        {
            throw new InvalidOperationException("Solo una etapa pendiente puede activarse.");
        }

        Estado = EstadoEtapa.Activa;
        IniciadaEnUtc = iniciadaEnUtc;
        CierraEnUtc = iniciadaEnUtc.AddSeconds(TiempoLimiteSegundos);
    }
}
