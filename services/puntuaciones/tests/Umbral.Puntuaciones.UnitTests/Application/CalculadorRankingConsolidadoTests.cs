using Umbral.Puntuaciones.Application.Handlers.Queries;
using Umbral.Puntuaciones.Domain.Entities;
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.UnitTests.Application;

public class CalculadorRankingConsolidadoTests
{
    private static readonly Guid Partida = Guid.NewGuid();

    private static Marcador Crear(Guid juegoId, Guid competidorId, int puntos, long tiempoMs,
        TipoCompetidor tipo = TipoCompetidor.Participante)
    {
        var marcador = Marcador.Nuevo(juegoId, competidorId, Partida, tipo);
        marcador.Acreditar(puntos, tiempoMs);
        return marcador;
    }

    [Fact]
    public void Sin_marcadores_devuelve_lista_vacia()
        => Assert.Empty(CalculadorRankingConsolidado.Calcular(Array.Empty<Marcador>()));

    [Fact]
    public void Ganador_de_cada_juego_acumula_juegos_ganados()
    {
        var juego1 = Guid.NewGuid();
        var juego2 = Guid.NewGuid();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();

        var entradas = CalculadorRankingConsolidado.Calcular(new[]
        {
            Crear(juego1, a, 20, 1000), Crear(juego1, b, 10, 500),
            Crear(juego2, a, 15, 2000), Crear(juego2, b, 10, 100)
        });

        Assert.Equal(a, entradas[0].CompetidorId);
        Assert.Equal(2, entradas[0].JuegosGanados);
        Assert.Equal(35, entradas[0].PuntosTotales);
        Assert.Equal(3000, entradas[0].TiempoTotalMs);
        Assert.Equal(1, entradas[0].Posicion);
        Assert.Equal(0, entradas[1].JuegosGanados);
        Assert.Equal(2, entradas[1].Posicion);
    }

    [Fact]
    public void Empate_de_puntos_en_un_juego_lo_gana_el_de_menor_tiempo()
    {
        var juego = Guid.NewGuid();
        var rapido = Guid.NewGuid();
        var lento = Guid.NewGuid();

        var entradas = CalculadorRankingConsolidado.Calcular(new[]
        {
            Crear(juego, lento, 10, 5000), Crear(juego, rapido, 10, 1000)
        });

        var deRapido = entradas.Single(e => e.CompetidorId == rapido);
        var deLento = entradas.Single(e => e.CompetidorId == lento);
        Assert.Equal(1, deRapido.JuegosGanados);
        Assert.Equal(0, deLento.JuegosGanados);
    }

    [Fact]
    public void Empate_exacto_en_un_juego_no_otorga_victoria_a_nadie()
    {
        var juego = Guid.NewGuid();

        var entradas = CalculadorRankingConsolidado.Calcular(new[]
        {
            Crear(juego, Guid.NewGuid(), 10, 1000), Crear(juego, Guid.NewGuid(), 10, 1000)
        });

        Assert.All(entradas, e => Assert.Equal(0, e.JuegosGanados));
    }

    [Fact]
    public void Juegos_ganados_manda_sobre_puntos_totales()
    {
        var juego1 = Guid.NewGuid();
        var juego2 = Guid.NewGuid();
        var juego3 = Guid.NewGuid();
        var ganador = Guid.NewGuid();
        var goleador = Guid.NewGuid();

        // ganador gana juego1 y juego2 con poco puntaje; goleador gana solo juego3 con muchos puntos.
        var entradas = CalculadorRankingConsolidado.Calcular(new[]
        {
            Crear(juego1, ganador, 10, 1000), Crear(juego1, goleador, 9, 500),
            Crear(juego2, ganador, 10, 1000), Crear(juego2, goleador, 9, 500),
            Crear(juego3, goleador, 50, 500)
        });

        Assert.Equal(ganador, entradas[0].CompetidorId);   // 2 juegos ganados, 20 puntos
        Assert.Equal(goleador, entradas[1].CompetidorId);  // 1 juego ganado, 68 puntos
    }

    [Fact]
    public void Mismos_juegos_ganados_desempata_por_puntos_y_luego_tiempo()
    {
        var juego1 = Guid.NewGuid();
        var juego2 = Guid.NewGuid();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();

        // a gana juego1, b gana juego2 (1 juego cada uno); a tiene más puntos totales que b.
        // c no gana nada, con puntos entre ambos: queda tercero por juegosGanados = 0.
        var entradas = CalculadorRankingConsolidado.Calcular(new[]
        {
            Crear(juego1, a, 30, 1000), Crear(juego1, c, 5, 500),
            Crear(juego2, b, 20, 1000), Crear(juego2, c, 6, 500)
        });

        Assert.Equal(new[] { a, b, c }, entradas.Select(e => e.CompetidorId).ToArray());
        Assert.Equal(new[] { 1, 2, 3 }, entradas.Select(e => e.Posicion).ToArray());
    }

    [Fact]
    public void Empate_total_comparte_posicion_y_la_siguiente_salta()
    {
        var juego = Guid.NewGuid();
        var primero = Guid.NewGuid();
        var empatadoA = Guid.NewGuid();
        var empatadoB = Guid.NewGuid();
        var cuarto = Guid.NewGuid();

        var entradas = CalculadorRankingConsolidado.Calcular(new[]
        {
            Crear(juego, primero, 50, 1000),
            Crear(juego, empatadoA, 20, 2000), Crear(juego, empatadoB, 20, 2000),
            Crear(juego, cuarto, 5, 3000)
        });

        Assert.Equal(new[] { 1, 2, 2, 4 }, entradas.Select(e => e.Posicion).ToArray());
    }

    [Fact]
    public void Conserva_tipo_competidor_equipo()
    {
        var juego = Guid.NewGuid();
        var equipo = Guid.NewGuid();

        var entradas = CalculadorRankingConsolidado.Calcular(new[]
        {
            Crear(juego, equipo, 10, 1000, TipoCompetidor.Equipo)
        });

        Assert.Equal(TipoCompetidor.Equipo, entradas[0].TipoCompetidor);
    }
}
