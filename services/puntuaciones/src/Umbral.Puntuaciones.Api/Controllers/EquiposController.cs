using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Umbral.Puntuaciones.Application.Queries;

namespace Umbral.Puntuaciones.Api.Controllers;

[ApiController]
[Route("puntuaciones")]
// S7: la vista "rendimiento de mi equipo" del móvil la usa un Participante sobre su propio equipo,
// que no porta ningún privilegio de gobernanza. Solo autenticado (paridad con ParticipantesController,
// el historial individual). Un autenticado con GestionarEquipos/Operador/Admin también entra.
[Authorize]
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
