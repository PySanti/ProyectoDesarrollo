using Umbral.OperacionesSesion.Domain.Enums;

namespace Umbral.OperacionesSesion.Domain.Results;

public sealed record ResultadoAvancePregunta(
    Guid JuegoId,
    Guid PreguntaCerradaId,
    int PreguntaCerradaOrden,
    MotivoCierrePregunta MotivoCierre,
    Guid? PreguntaActivadaId,
    int? PreguntaActivadaOrden,
    int? TiempoLimiteActivadaSegundos,
    DateTime? FechaActivacionActivada,
    bool SinMasPreguntas);
