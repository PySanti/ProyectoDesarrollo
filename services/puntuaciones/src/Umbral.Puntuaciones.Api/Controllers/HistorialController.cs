using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Umbral.Puntuaciones.Application.Queries;

namespace Umbral.Puntuaciones.Api.Controllers;

// HU-43: el historial expone respuestas, pistas y ubicaciones de todos los participantes —
// primer endpoint de Puntuaciones con autorización por rol (solo operador/administrador).
[ApiController]
[Route("puntuaciones")]
[Authorize(Policy = "OperadorOAdminGestionarPartidas")]
public sealed class HistorialController : ControllerBase
{
    private readonly ISender _mediator;

    public HistorialController(ISender mediator) => _mediator = mediator;

    [HttpGet("partidas/{partidaId:guid}/historial")]
    public async Task<IActionResult> ObtenerHistorial(
        Guid partidaId,
        [FromQuery] int limit = 100,
        [FromQuery] int offset = 0,
        [FromQuery] string? tipo = null,
        CancellationToken cancellationToken = default)
    {
        var response = await _mediator.Send(
            new ObtenerHistorialPartidaQuery(partidaId, limit, offset, tipo), cancellationToken);
        return Ok(response);
    }
}
