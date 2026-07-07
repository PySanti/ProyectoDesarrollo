using Umbral.Puntuaciones.Application.Exceptions;
using Umbral.Puntuaciones.Application.Handlers.Queries;
using Umbral.Puntuaciones.Application.Queries;
using Umbral.Puntuaciones.Domain.Entities;
using Umbral.Puntuaciones.Domain.Enums;
using Umbral.Puntuaciones.UnitTests.Application.Fakes;

namespace Umbral.Puntuaciones.UnitTests.Application;

public class ObtenerRankingConsolidadoQueryHandlerTests
{
    private static readonly DateTime Ahora = DateTime.UtcNow;

    [Fact]
    public async Task Partida_desconocida_lanza_no_encontrada()
    {
        var handler = new ObtenerRankingConsolidadoQueryHandler(new FakeProyeccionesRepository());

        await Assert.ThrowsAsync<PartidaNoEncontradaException>(
            () => handler.Handle(new ObtenerRankingConsolidadoQuery(Guid.NewGuid()), CancellationToken.None));
    }

    [Theory]
    [InlineData("Lobby")]
    [InlineData("Iniciada")]
    [InlineData("Cancelada")]
    public async Task Partida_no_terminada_lanza_conflicto(string estado)
    {
        var partidaId = Guid.NewGuid();
        var repo = new FakeProyeccionesRepository();
        var partida = PartidaProyectada.DesdePublicacion(partidaId, Guid.NewGuid(), Modalidad.Individual);
        if (estado == "Iniciada") { partida.MarcarIniciada(Ahora); }
        if (estado == "Cancelada") { partida.MarcarCancelada(Ahora); }
        repo.AddPartida(partida);
        var handler = new ObtenerRankingConsolidadoQueryHandler(repo);

        await Assert.ThrowsAsync<PartidaNoTerminadaException>(
            () => handler.Handle(new ObtenerRankingConsolidadoQuery(partidaId), CancellationToken.None));
    }

    [Fact]
    public async Task Partida_terminada_devuelve_consolidado_ordenado()
    {
        var partidaId = Guid.NewGuid();
        var juego1 = Guid.NewGuid();
        var juego2 = Guid.NewGuid();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var repo = new FakeProyeccionesRepository();
        var partida = PartidaProyectada.DesdePublicacion(partidaId, Guid.NewGuid(), Modalidad.Individual);
        partida.MarcarTerminada(Ahora);
        repo.AddPartida(partida);

        void Sembrar(Guid juegoId, Guid competidorId, int puntos, long tiempo)
        {
            var m = Marcador.Nuevo(juegoId, competidorId, partidaId, TipoCompetidor.Participante);
            m.Acreditar(puntos, tiempo);
            repo.AddMarcador(m);
        }
        Sembrar(juego1, a, 20, 1000);
        Sembrar(juego1, b, 10, 500);
        Sembrar(juego2, b, 30, 2000);

        var handler = new ObtenerRankingConsolidadoQueryHandler(repo);
        var response = await handler.Handle(new ObtenerRankingConsolidadoQuery(partidaId), CancellationToken.None);

        Assert.Equal(partidaId, response.PartidaId);
        Assert.NotEqual(default, response.GeneradoEn);
        Assert.Equal(2, response.Entradas.Count);
        // a y b ganan 1 juego cada uno; b tiene más puntos totales (40 > 20) → b primero.
        Assert.Equal(b, response.Entradas[0].CompetidorId);
        Assert.Equal(1, response.Entradas[0].JuegosGanados);
        Assert.Equal(40, response.Entradas[0].PuntosTotales);
    }

    [Fact]
    public async Task Partida_terminada_sin_marcadores_devuelve_entradas_vacias()
    {
        var partidaId = Guid.NewGuid();
        var repo = new FakeProyeccionesRepository();
        var partida = PartidaProyectada.DesdePublicacion(partidaId, Guid.NewGuid(), Modalidad.Individual);
        partida.MarcarTerminada(Ahora);
        repo.AddPartida(partida);
        var handler = new ObtenerRankingConsolidadoQueryHandler(repo);

        var response = await handler.Handle(new ObtenerRankingConsolidadoQuery(partidaId), CancellationToken.None);

        Assert.Empty(response.Entradas);
    }
}
