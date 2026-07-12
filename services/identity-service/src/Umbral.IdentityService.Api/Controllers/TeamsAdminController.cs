using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Umbral.IdentityService.Application.Queries;

namespace Umbral.IdentityService.Api.Controllers;

// Listado de equipos para Administrador/Operador (vista web de solo lectura).
// Vive fuera de TeamsController porque la policy de clase GestionarEquipos es
// aditiva y esos roles no tienen ese permiso funcional.
[ApiController]
[Route("identity/teams")]
[Authorize(Policy = "OperadorOAdministrador")]
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
