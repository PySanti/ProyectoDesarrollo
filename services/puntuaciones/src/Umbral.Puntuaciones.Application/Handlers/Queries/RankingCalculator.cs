using Umbral.Puntuaciones.Application.DTOs;
using Umbral.Puntuaciones.Domain.Entities;

namespace Umbral.Puntuaciones.Application.Handlers.Queries;

public static class RankingCalculator
{
    public static IReadOnlyList<EntradaRankingDto> Calcular(IEnumerable<Marcador> marcadores)
    {
        var ordenados = marcadores
            .OrderByDescending(m => m.PuntosAcumulados)
            .ThenBy(m => m.TiempoAcumuladoMs)
            .ToList();

        var entradas = new List<EntradaRankingDto>(ordenados.Count);
        for (var i = 0; i < ordenados.Count; i++)
        {
            var actual = ordenados[i];
            var posicion = i + 1;
            if (i > 0)
            {
                var previo = ordenados[i - 1];
                var empateExacto = previo.PuntosAcumulados == actual.PuntosAcumulados
                    && previo.TiempoAcumuladoMs == actual.TiempoAcumuladoMs;
                if (empateExacto)
                {
                    posicion = entradas[i - 1].Posicion;
                }
            }

            entradas.Add(new EntradaRankingDto(
                posicion, actual.CompetidorId, actual.TipoCompetidor,
                actual.PuntosAcumulados, actual.TiempoAcumuladoMs, actual.UnidadesGanadas));
        }

        return entradas;
    }
}
