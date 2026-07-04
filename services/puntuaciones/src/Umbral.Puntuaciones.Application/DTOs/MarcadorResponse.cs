using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.Application.DTOs;

public sealed record MarcadorResponse(
    Guid CompetidorId, TipoCompetidor TipoCompetidor,
    int Puntos, long TiempoAcumuladoMs, int UnidadesGanadas, int Posicion);
