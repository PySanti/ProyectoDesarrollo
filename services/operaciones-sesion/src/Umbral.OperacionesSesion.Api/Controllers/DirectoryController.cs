using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Umbral.OperacionesSesion.Application.DTOs;
using Umbral.OperacionesSesion.Application.Queries;

namespace Umbral.OperacionesSesion.Api.Controllers;

// Directorio de nombres de partida. Vive fuera de SesionesController porque aquel agrupa
// el ciclo de vida de la sesion y este es una lectura de apoyo; misma separacion que el
// DirectoryController de Identity.
//
// [Authorize] explicito pese a que el SetFallbackPolicy de Program.cs ya exige usuario
// autenticado: el punto del endpoint es ser alcanzable por Participante (el gateway le
// cierra /partidas/**), asi que la intencion queda escrita y no depende del fallback.
// Su ausencia en otros controllers NO significa anonimo: significa que heredan el fallback.
[ApiController]
[Route("operaciones-sesion/directory")]
[Authorize]
public sealed class DirectoryController : ControllerBase
{
    private readonly ISender _mediator;

    public DirectoryController(ISender mediator) => _mediator = mediator;

    // El tope del lote lo aplica ResolverNombresPartidaQueryValidator dentro del
    // ValidationBehavior de MediatR, y el middleware centralizado mapea la
    // ValidationException a 400: el controller queda como despachador puro (doctrine
    // audit M-2), sin inyectar el validador como hace Identity.
    [HttpPost("partidas")]
    public async Task<IActionResult> ResolverNombresPartida(
        [FromBody] ResolverNombresPartidaRequest request,
        CancellationToken cancellationToken)
        => Ok(await _mediator.Send(
            new ResolverNombresPartidaQuery(request.PartidaIds ?? Array.Empty<Guid>()),
            cancellationToken));
}
