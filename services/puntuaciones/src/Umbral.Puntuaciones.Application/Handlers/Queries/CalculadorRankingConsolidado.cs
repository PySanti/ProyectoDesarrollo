using Umbral.Puntuaciones.Application.DTOs;
using Umbral.Puntuaciones.Domain.Entities;

namespace Umbral.Puntuaciones.Application.Handlers.Queries;

// Consolidado de partida (RF-45): juegos ganados DESC, puntos totales DESC, tiempo total ASC.
// Ganador por juego: más puntos, desempate menor tiempo; empate exacto → el juego no otorga victoria.
// Solo necesita los marcadores de la partida: cada grupo por JuegoId define un juego a efectos
// del cálculo (tolerante a juegos no proyectados por pérdida de JuegoActivado, best-effort ADR-0012).
public static class CalculadorRankingConsolidado
{
    public static IReadOnlyList<EntradaRankingConsolidadoDto> Calcular(IEnumerable<Marcador> marcadoresDePartida)
    {
        var marcadores = marcadoresDePartida.ToList();
        if (marcadores.Count == 0)
        {
            return Array.Empty<EntradaRankingConsolidadoDto>();
        }

        var ganadoresPorJuego = new List<Guid>();
        foreach (var juego in marcadores.GroupBy(m => m.JuegoId))
        {
            var ordenados = juego
                .OrderByDescending(m => m.PuntosAcumulados)
                .ThenBy(m => m.TiempoAcumuladoMs)
                .ToList();
            var empateExacto = ordenados.Count > 1
                && ordenados[0].PuntosAcumulados == ordenados[1].PuntosAcumulados
                && ordenados[0].TiempoAcumuladoMs == ordenados[1].TiempoAcumuladoMs;
            if (!empateExacto)
            {
                ganadoresPorJuego.Add(ordenados[0].CompetidorId);
            }
        }

        var agregados = marcadores
            .GroupBy(m => m.CompetidorId)
            .Select(g => new
            {
                CompetidorId = g.Key,
                g.First().TipoCompetidor,
                JuegosGanados = ganadoresPorJuego.Count(id => id == g.Key),
                PuntosTotales = g.Sum(m => m.PuntosAcumulados),
                TiempoTotalMs = g.Sum(m => m.TiempoAcumuladoMs)
            })
            .OrderByDescending(a => a.JuegosGanados)
            .ThenByDescending(a => a.PuntosTotales)
            .ThenBy(a => a.TiempoTotalMs)
            .ToList();

        var entradas = new List<EntradaRankingConsolidadoDto>(agregados.Count);
        for (var i = 0; i < agregados.Count; i++)
        {
            var actual = agregados[i];
            var posicion = i + 1;
            if (i > 0)
            {
                var previo = agregados[i - 1];
                var empateExacto = previo.JuegosGanados == actual.JuegosGanados
                    && previo.PuntosTotales == actual.PuntosTotales
                    && previo.TiempoTotalMs == actual.TiempoTotalMs;
                if (empateExacto)
                {
                    posicion = entradas[i - 1].Posicion;
                }
            }

            entradas.Add(new EntradaRankingConsolidadoDto(
                posicion, actual.CompetidorId, actual.TipoCompetidor,
                actual.JuegosGanados, actual.PuntosTotales, actual.TiempoTotalMs));
        }

        return entradas;
    }
}
