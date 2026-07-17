using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Umbral.Puntuaciones.Application.Queries;

namespace Umbral.Puntuaciones.Api.Controllers;

[ApiController]
[Route("puntuaciones")]
[Authorize(Policy = "GestionarEquipos")]
public sealed class EquiposController : ControllerBase
{
    private readonly ISender _mediator;

    public EquiposController(ISender mediator) => _mediator = mediator;

    [HttpGet("equipos/{equipoId:guid}/rendimiento")]
    public async Task<IActionResult> ObtenerRendimiento(Guid equipoId, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new ObtenerRendimientoEquipoQuery(equipoId), cancellationToken);
        return Ok(response);
    }
}
