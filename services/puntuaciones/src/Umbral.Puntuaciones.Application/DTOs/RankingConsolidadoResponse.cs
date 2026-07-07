using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.Application.DTOs;

public sealed record EntradaRankingConsolidadoDto(
    int Posicion, Guid CompetidorId, TipoCompetidor TipoCompetidor,
    int JuegosGanados, int PuntosTotales, long TiempoTotalMs);

public sealed record RankingConsolidadoResponse(
    Guid PartidaId, DateTime GeneradoEn, IReadOnlyList<EntradaRankingConsolidadoDto> Entradas);
