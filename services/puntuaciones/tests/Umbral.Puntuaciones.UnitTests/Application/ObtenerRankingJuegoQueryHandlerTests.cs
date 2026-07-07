using Umbral.Puntuaciones.Application.Exceptions;
using Umbral.Puntuaciones.Application.Handlers.Queries;
using Umbral.Puntuaciones.Application.Queries;
using Umbral.Puntuaciones.Domain.Entities;
using Umbral.Puntuaciones.Domain.Enums;
using Umbral.Puntuaciones.UnitTests.Application.Fakes;

namespace Umbral.Puntuaciones.UnitTests.Application;

public class ObtenerRankingJuegoQueryHandlerTests
{
    private readonly FakeProyeccionesRepository _repo = new();

    private (Guid partidaId, Guid juegoId) SembrarJuego(TipoJuego tipo = TipoJuego.Trivia)
    {
        var partidaId = Guid.NewGuid();
        var juegoId = Guid.NewGuid();
        _repo.AddJuego(JuegoProyectado.Desde(juegoId, partidaId, 1, tipo));
        return (partidaId, juegoId);
    }

    private void SembrarMarcador(Guid juegoId, Guid competidorId, int puntos, long tiempoMs, int unidades)
    {
        var m = Marcador.Nuevo(juegoId, competidorId, Guid.NewGuid(), TipoCompetidor.Participante);
        for (var i = 0; i < unidades; i++)
        {
            m.Acreditar(i == 0 ? puntos : 0, i == 0 ? tiempoMs : 0);
        }
        _repo.AddMarcador(m);
    }

    [Fact]
    public async Task Ordena_por_puntos_desc_y_tiempo_asc()
    {
        var (partidaId, juegoId) = SembrarJuego();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();
        SembrarMarcador(juegoId, a, 10, 5000, 1); // 2do: menos puntos que c
        SembrarMarcador(juegoId, b, 10, 9000, 1); // 3ro: mismos puntos que a, mas tiempo
        SembrarMarcador(juegoId, c, 20, 9999, 1); // 1ro: mas puntos, el tiempo no lo baja

        var r = await new ObtenerRankingJuegoQueryHandler(_repo).Handle(
            new ObtenerRankingJuegoQuery(partidaId, juegoId), CancellationToken.None);

        Assert.Equal(new[] { c, a, b }, r.Entradas.Select(e => e.CompetidorId).ToArray());
        Assert.Equal(new[] { 1, 2, 3 }, r.Entradas.Select(e => e.Posicion).ToArray());
        Assert.Equal(TipoJuego.Trivia, r.TipoJuego);
    }

    [Fact]
    public async Task Empate_exacto_comparte_posicion_y_la_siguiente_salta()
    {
        var (partidaId, juegoId) = SembrarJuego(TipoJuego.BusquedaDelTesoro);
        SembrarMarcador(juegoId, Guid.NewGuid(), 20, 1000, 2);
        SembrarMarcador(juegoId, Guid.NewGuid(), 10, 3000, 1); // empatado
        SembrarMarcador(juegoId, Guid.NewGuid(), 10, 3000, 1); // empatado
        SembrarMarcador(juegoId, Guid.NewGuid(), 5, 100, 1);

        var r = await new ObtenerRankingJuegoQueryHandler(_repo).Handle(
            new ObtenerRankingJuegoQuery(partidaId, juegoId), CancellationToken.None);

        Assert.Equal(new[] { 1, 2, 2, 4 }, r.Entradas.Select(e => e.Posicion).ToArray());
    }

    [Fact]
    public async Task Muchas_unidades_ganadas_no_ordenan_solo_los_puntos()
    {
        // Doctrina BDT: EtapasGanadas es informativo; gana quien acumula mas puntos.
        var (partidaId, juegoId) = SembrarJuego(TipoJuego.BusquedaDelTesoro);
        var muchasEtapas = Guid.NewGuid();
        var pocasEtapasMasPuntos = Guid.NewGuid();
        SembrarMarcador(juegoId, muchasEtapas, 10, 1000, 3);
        SembrarMarcador(juegoId, pocasEtapasMasPuntos, 50, 9000, 1);

        var r = await new ObtenerRankingJuegoQueryHandler(_repo).Handle(
            new ObtenerRankingJuegoQuery(partidaId, juegoId), CancellationToken.None);

        Assert.Equal(pocasEtapasMasPuntos, r.Entradas[0].CompetidorId);
        Assert.Equal(3, r.Entradas[1].UnidadesGanadas);
    }

    [Fact]
    public async Task Juego_sin_marcadores_devuelve_lista_vacia()
    {
        var (partidaId, juegoId) = SembrarJuego();

        var r = await new ObtenerRankingJuegoQueryHandler(_repo).Handle(
            new ObtenerRankingJuegoQuery(partidaId, juegoId), CancellationToken.None);

        Assert.Empty(r.Entradas);
        Assert.Equal(juegoId, r.JuegoId);
    }

    [Fact]
    public async Task Juego_desconocido_lanza_404()
    {
        await Assert.ThrowsAsync<JuegoNoEncontradoException>(() =>
            new ObtenerRankingJuegoQueryHandler(_repo).Handle(
                new ObtenerRankingJuegoQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Juego_de_otra_partida_lanza_404()
    {
        var (_, juegoId) = SembrarJuego();

        await Assert.ThrowsAsync<JuegoNoEncontradoException>(() =>
            new ObtenerRankingJuegoQueryHandler(_repo).Handle(
                new ObtenerRankingJuegoQuery(Guid.NewGuid(), juegoId), CancellationToken.None));
    }
}
