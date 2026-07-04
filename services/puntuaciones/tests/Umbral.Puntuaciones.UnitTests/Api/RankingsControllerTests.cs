using Microsoft.AspNetCore.Mvc;
using Umbral.Puntuaciones.Api.Controllers;
using Umbral.Puntuaciones.Application.DTOs;
using Umbral.Puntuaciones.Application.Queries;
using Umbral.Puntuaciones.Domain.Enums;

namespace Umbral.Puntuaciones.UnitTests.Api;

public class RankingsControllerTests
{
    [Fact]
    public async Task ObtenerRanking_despacha_query_y_devuelve_ok()
    {
        var partidaId = Guid.NewGuid();
        var juegoId = Guid.NewGuid();
        var respuesta = new RankingJuegoResponse(juegoId, TipoJuego.Trivia, DateTime.UtcNow, Array.Empty<EntradaRankingDto>());
        var sender = new FakeSender(respuesta);
        var controller = new RankingsController(sender);

        var result = await controller.ObtenerRanking(partidaId, juegoId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(respuesta, ok.Value);
        var query = Assert.IsType<ObtenerRankingJuegoQuery>(sender.LastRequest);
        Assert.Equal(partidaId, query.PartidaId);
        Assert.Equal(juegoId, query.JuegoId);
    }

    [Fact]
    public async Task ObtenerMarcador_despacha_query_y_devuelve_ok()
    {
        var partidaId = Guid.NewGuid();
        var juegoId = Guid.NewGuid();
        var competidorId = Guid.NewGuid();
        var respuesta = new MarcadorResponse(competidorId, TipoCompetidor.Participante, 10, 1000, 1, 1);
        var sender = new FakeSender(respuesta);
        var controller = new RankingsController(sender);

        var result = await controller.ObtenerMarcador(partidaId, juegoId, competidorId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(respuesta, ok.Value);
        var query = Assert.IsType<ObtenerMarcadorQuery>(sender.LastRequest);
        Assert.Equal(partidaId, query.PartidaId);
        Assert.Equal(juegoId, query.JuegoId);
        Assert.Equal(competidorId, query.CompetidorId);
    }
}
