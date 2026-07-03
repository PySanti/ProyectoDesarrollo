using Umbral.OperacionesSesion.Domain.Enums;

namespace Umbral.OperacionesSesion.Domain.Results;

public sealed record ResultadoAvanceEtapa(
    Guid JuegoId,
    Guid EtapaCerradaId,
    int EtapaCerradaOrden,
    MotivoCierreEtapa MotivoCierre,
    Guid? EtapaActivadaId,
    int? EtapaActivadaOrden,
    int? TiempoLimiteActivadaSegundos,
    DateTime? FechaActivacionActivada,
    bool SinMasEtapas);
