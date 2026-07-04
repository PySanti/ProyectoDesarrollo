using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.Application.DTOs;

public sealed record EntradaRankingDto(
    int Posicion, Guid CompetidorId, TipoCompetidor TipoCompetidor,
    int Puntos, long TiempoAcumuladoMs, int UnidadesGanadas);

public sealed record RankingJuegoResponse(
    Guid JuegoId, TipoJuego TipoJuego, DateTime GeneradoEn, IReadOnlyList<EntradaRankingDto> Entradas);
