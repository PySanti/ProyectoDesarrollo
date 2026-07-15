using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Umbral.IdentityService.Application.Queries;

namespace Umbral.IdentityService.Api.Controllers;

// Listado de equipos para Administrador/Operador (vista web de solo lectura).
// Vive fuera de TeamsController porque exige, además del rol, el privilegio GestionarEquipos
// (rol AND privilegio): los puertos de servicio están expuestos y una policy de sólo-privilegio
// dejaría escalar a cualquier rol al que el panel le dé GestionarEquipos. Con los defaults
// actuales el Administrador ya trae GestionarEquipos; el Operador lo necesita del panel.
[ApiController]
[Route("identity/teams")]
[Authorize(Policy = "OperadorOAdminGestionarEquipos")]
public sealed class TeamsAdminController : ControllerBase
{
    private readonly ISender _sender;

    public TeamsAdminController(ISender sender) => _sender = sender;

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        var equipos = await _sender.Send(new ListarEquiposQuery(), cancellationToken);
        return Ok(equipos);
    }
}
