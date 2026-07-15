using System;
using Umbral.Puntuaciones.Application.Handlers.Queries;
using Umbral.Puntuaciones.Domain.Entities;
using Umbral.Puntuaciones.Domain.Enums;
using Xunit;

namespace Umbral.Puntuaciones.UnitTests.Application;

public class RankingCalculatorTests
{
    [Fact]
    public void Participante_sin_marcador_sale_ultimo_con_cero()
    {
        var juegoId = Guid.NewGuid();
        var partidaId = Guid.NewGuid();
        var anotador = Guid.NewGuid();
        var mudo = Guid.NewGuid();
        var m = Marcador.Nuevo(juegoId, anotador, partidaId, TipoCompetidor.Participante);
        m.Acreditar(10, 500);

        var r = RankingCalculator.Calcular(
            new[] { m },
            new[]
            {
                ParticipacionProyectada.Nueva(partidaId, anotador, TipoCompetidor.Participante),
                ParticipacionProyectada.Nueva(partidaId, mudo, TipoCompetidor.Participante)
            });

        Assert.Equal(2, r.Count);
        Assert.Equal(anotador, r[0].CompetidorId);
        Assert.Equal(mudo, r[1].CompetidorId);
        Assert.Equal(0, r[1].Puntos);
        Assert.Equal(2, r[1].Posicion);
    }

    [Fact]
    public void Al_arrancar_el_juego_todos_salen_a_cero_en_vez_de_lista_vacia()
    {
        var partidaId = Guid.NewGuid();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();

        var r = RankingCalculator.Calcular(
            Array.Empty<Marcador>(),
            new[]
            {
                ParticipacionProyectada.Nueva(partidaId, a, TipoCompetidor.Participante),
                ParticipacionProyectada.Nueva(partidaId, b, TipoCompetidor.Participante)
            });

        // Antes: entradas vacías hasta el primer acierto; el operador no veía a nadie.
        Assert.Equal(2, r.Count);
        Assert.All(r, e => Assert.Equal(0, e.Puntos));
        Assert.All(r, e => Assert.Equal(1, e.Posicion)); // 0/0 empatan exacto
    }

    [Fact]
    public void Sin_participaciones_ni_marcadores_devuelve_vacio()
    {
        var r = RankingCalculator.Calcular(Array.Empty<Marcador>(), Array.Empty<ParticipacionProyectada>());

        Assert.Empty(r);
    }

    [Fact]
    public void Competidor_con_marcador_pero_sin_participacion_sigue_saliendo()
    {
        // Si se perdió InscripcionAceptada (best-effort ADR-0012), el marcador prueba que jugó:
        // el universo es la UNIÓN, no solo las participaciones.
        var juegoId = Guid.NewGuid();
        var partidaId = Guid.NewGuid();
        var m = Marcador.Nuevo(juegoId, Guid.NewGuid(), partidaId, TipoCompetidor.Participante);
        m.Acreditar(5, 100);

        var r = RankingCalculator.Calcular(new[] { m }, Array.Empty<ParticipacionProyectada>());

        Assert.Single(r);
    }
}
