using Umbral.BdtGameService.Domain.Enums;

namespace Umbral.BdtGameService.Domain.Entities;

public sealed class TesoroQR
{
    public Guid TesoroId { get; private set; }
    public Guid PartidaId { get; private set; }
    public Guid EtapaId { get; private set; }
    public Guid ExploradorId { get; private set; }
    public string ImagenReferencia { get; private set; }
    public string? QrDecodificado { get; private set; }
    public EstadoProcesamientoTesoroQr EstadoProcesamiento { get; private set; }
    public DateTime FechaEnvioUtc { get; private set; }

    private TesoroQR()
    {
        ImagenReferencia = string.Empty;
    }

    private TesoroQR(
        Guid partidaId,
        Guid etapaId,
        Guid exploradorId,
        string imagenReferencia,
        string? qrDecodificado,
        DateTime fechaEnvioUtc)
    {
        if (partidaId == Guid.Empty)
        {
            throw new ArgumentException("PartidaId requerido", nameof(partidaId));
        }

        if (etapaId == Guid.Empty)
        {
            throw new ArgumentException("EtapaId requerido", nameof(etapaId));
        }

        if (exploradorId == Guid.Empty)
        {
            throw new ArgumentException("ExploradorId requerido", nameof(exploradorId));
        }

        if (string.IsNullOrWhiteSpace(imagenReferencia))
        {
            throw new ArgumentException("ImagenReferencia requerida", nameof(imagenReferencia));
        }

        if (fechaEnvioUtc == default)
        {
            throw new ArgumentException("FechaEnvioUtc requerida", nameof(fechaEnvioUtc));
        }

        TesoroId = Guid.NewGuid();
        PartidaId = partidaId;
        EtapaId = etapaId;
        ExploradorId = exploradorId;
        ImagenReferencia = imagenReferencia.Trim();
        QrDecodificado = string.IsNullOrWhiteSpace(qrDecodificado) ? null : qrDecodificado.Trim();
        EstadoProcesamiento = QrDecodificado is null
            ? EstadoProcesamientoTesoroQr.NoLegible
            : EstadoProcesamientoTesoroQr.Decodificado;
        FechaEnvioUtc = fechaEnvioUtc;
    }

    public static TesoroQR Crear(
        Guid partidaId,
        Guid etapaId,
        Guid exploradorId,
        string imagenReferencia,
        string? qrDecodificado,
        DateTime fechaEnvioUtc)
    {
        return new TesoroQR(partidaId, etapaId, exploradorId, imagenReferencia, qrDecodificado, fechaEnvioUtc);
    }
}
