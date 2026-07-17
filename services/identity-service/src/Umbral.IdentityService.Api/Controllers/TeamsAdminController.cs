using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Umbral.IdentityService.Application.Queries;

namespace Umbral.IdentityService.Api.Controllers;

// Listado de equipos para quien tenga GestionarEquipos (vista web de solo lectura). Vive fuera de
// TeamsController porque administra equipos ajenos: TeamsController es para el equipo propio del
// Participante (viene con el rol), esto es para cualquiera con el privilegio de gestión.
[ApiController]
[Route("identity/teams")]
[Authorize(Policy = "GestionarEquipos")]
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
