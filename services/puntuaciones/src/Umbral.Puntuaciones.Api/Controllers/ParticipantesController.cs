using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Umbral.Puntuaciones.Application.Queries;

namespace Umbral.Puntuaciones.Api.Controllers;

[ApiController]
[Route("puntuaciones")]
[Authorize]
public sealed class ParticipantesController : ControllerBase
{
    private readonly ISender _mediator;

    public ParticipantesController(ISender mediator) => _mediator = mediator;

    [HttpGet("participantes/{participanteId:guid}/historial-partidas")]
    public async Task<IActionResult> ObtenerHistorialPartidas(Guid participanteId, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new ObtenerHistorialPartidasQuery(participanteId), cancellationToken);
        return Ok(response);
    }
}
