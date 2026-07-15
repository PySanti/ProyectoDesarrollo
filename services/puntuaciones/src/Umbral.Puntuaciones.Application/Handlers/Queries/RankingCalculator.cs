using Umbral.Puntuaciones.Application.DTOs;
using Umbral.Puntuaciones.Domain.Entities;
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.Application.Handlers.Queries;

public static class RankingCalculator
{
    // Universo = participaciones ∪ marcadores. Antes eran solo los marcadores, y un marcador solo
    // nace al acreditar puntos: quien no anotaba no aparecía, y al arrancar el juego la tabla salía
    // vacía. Se mantiene la unión (no solo participaciones) porque si se pierde InscripcionAceptada
    // el marcador prueba que jugó.
    private sealed record Fila(Guid CompetidorId, TipoCompetidor Tipo, int Puntos, long TiempoMs, int Unidades);

    public static IReadOnlyList<EntradaRankingDto> Calcular(
        IEnumerable<Marcador> marcadores, IEnumerable<ParticipacionProyectada> participaciones)
    {
        var filas = marcadores
            .Select(m => new Fila(m.CompetidorId, m.TipoCompetidor, m.PuntosAcumulados, m.TiempoAcumuladoMs, m.UnidadesGanadas))
            .ToList();

        // Materializar antes de concatenar: filas se lee dentro del Where.
        var sinMarcador = participaciones
            .Where(p => filas.All(f => f.CompetidorId != p.CompetidorId))
            .Select(p => new Fila(p.CompetidorId, p.TipoCompetidor, 0, 0L, 0))
            .ToList();

        var ordenados = filas
            .Concat(sinMarcador)
            .OrderByDescending(f => f.Puntos)
            .ThenBy(f => f.TiempoMs)
            .ToList();

        var entradas = new List<EntradaRankingDto>(ordenados.Count);
        for (var i = 0; i < ordenados.Count; i++)
        {
            var actual = ordenados[i];
            var posicion = i + 1;
            if (i > 0)
            {
                var previo = ordenados[i - 1];
                var empateExacto = previo.Puntos == actual.Puntos && previo.TiempoMs == actual.TiempoMs;
                if (empateExacto)
                {
                    posicion = entradas[i - 1].Posicion;
                }
            }

            entradas.Add(new EntradaRankingDto(
                posicion, actual.CompetidorId, actual.Tipo, actual.Puntos, actual.TiempoMs, actual.Unidades));
        }

        return entradas;
    }
}
