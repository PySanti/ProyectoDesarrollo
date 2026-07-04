using MediatR;
using Microsoft.AspNetCore.Mvc;
using Umbral.Puntuaciones.Application.Queries;

namespace Umbral.Puntuaciones.Api.Controllers;

[ApiController]
[Route("puntuaciones")]
public sealed class RankingsController : ControllerBase
{
    private readonly ISender _mediator;

    public RankingsController(ISender mediator) => _mediator = mediator;

    [HttpGet("partidas/{partidaId:guid}/juegos/{juegoId:guid}/ranking")]
    public async Task<IActionResult> ObtenerRanking(Guid partidaId, Guid juegoId, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new ObtenerRankingJuegoQuery(partidaId, juegoId), cancellationToken);
        return Ok(response);
    }

    [HttpGet("partidas/{partidaId:guid}/juegos/{juegoId:guid}/marcadores/{competidorId:guid}")]
    public async Task<IActionResult> ObtenerMarcador(Guid partidaId, Guid juegoId, Guid competidorId, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new ObtenerMarcadorQuery(partidaId, juegoId, competidorId), cancellationToken);
        return Ok(response);
    }
}
