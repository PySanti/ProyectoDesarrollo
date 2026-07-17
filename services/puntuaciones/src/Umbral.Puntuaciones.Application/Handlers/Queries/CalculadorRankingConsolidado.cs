using Umbral.Puntuaciones.Application.DTOs;
using Umbral.Puntuaciones.Domain.Entities;
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.Application.Handlers.Queries;

// Consolidado de partida (RF-45): juegos ganados DESC, puntos totales DESC, tiempo total ASC.
// Ganador por juego: más puntos, desempate menor tiempo; empate exacto → el juego no otorga victoria.
// Cada grupo por JuegoId define un juego a efectos del cálculo (tolerante a juegos no proyectados
// por pérdida de JuegoActivado, best-effort ADR-0012).
// Universo = participaciones ∪ marcadores: quien participó y no anotó entra con ceros.
public static class CalculadorRankingConsolidado
{
    private sealed record Agregado(
        Guid CompetidorId, TipoCompetidor TipoCompetidor, int JuegosGanados, int PuntosTotales, long TiempoTotalMs);

    public static IReadOnlyList<EntradaRankingConsolidadoDto> Calcular(
        IEnumerable<Marcador> marcadoresDePartida, IEnumerable<ParticipacionProyectada> participaciones)
    {
        var marcadores = marcadoresDePartida.ToList();
        var participes = participaciones.ToList();
        // Con participaciones y sin marcadores la respuesta correcta es todos a 0, no lista vacía.
        if (marcadores.Count == 0 && participes.Count == 0)
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
            .Select(g => new Agregado(
                g.Key,
                g.First().TipoCompetidor,
                ganadoresPorJuego.Count(id => id == g.Key),
                g.Sum(m => m.PuntosAcumulados),
                g.Sum(m => m.TiempoAcumuladoMs)))
            .ToList();

        // Participó y no anotó: entra con ceros. Materializar antes del AddRange — el Where lee
        // `agregados`, y añadirle elementos mientras se enumera lo rompería.
        var sinMarcador = participes
            .Where(p => agregados.All(a => a.CompetidorId != p.CompetidorId))
            .Select(p => new Agregado(p.CompetidorId, p.TipoCompetidor, 0, 0, 0L))
            .ToList();
        agregados.AddRange(sinMarcador);

        var clasificacion = agregados
            .OrderByDescending(a => a.JuegosGanados)
            .ThenByDescending(a => a.PuntosTotales)
            .ThenBy(a => a.TiempoTotalMs)
            .ToList();

        var entradas = new List<EntradaRankingConsolidadoDto>(clasificacion.Count);
        for (var i = 0; i < clasificacion.Count; i++)
        {
            var actual = clasificacion[i];
            var posicion = i + 1;
            if (i > 0)
            {
                var previo = clasificacion[i - 1];
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
