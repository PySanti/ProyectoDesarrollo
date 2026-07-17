using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Umbral.IdentityService.Api.Contracts;
using Umbral.IdentityService.Application.Queries;

namespace Umbral.IdentityService.Api.Controllers;

// Directorio de nombres para pintar competidores en las pantallas de operador y de
// participante. Vive fuera de UsersController porque ese está bajo AdminOnly y este
// endpoint debe ser alcanzable por cualquier usuario autenticado, incluido Participante
// (mismo razonamiento que TeamsAdminController con la policy aditiva GestionarEquipos).
[ApiController]
[Route("identity/directory")]
[Authorize]
public sealed class DirectoryController : ControllerBase
{
    private readonly ISender _sender;

    public DirectoryController(ISender sender) => _sender = sender;

    [HttpPost("names")]
    public async Task<IActionResult> ResolverNombres(
        [FromBody] ResolverNombresRequest request,
        [FromServices] IValidator<ResolverNombresQuery> validator,
        CancellationToken cancellationToken)
    {
        var query = new ResolverNombresQuery(
            request.ParticipanteIds ?? Array.Empty<Guid>(),
            request.EquipoIds ?? Array.Empty<Guid>());

        var validation = await validator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);

            return new BadRequestObjectResult(new ValidationProblemDetails(ModelState));
        }

        var response = await _sender.Send(query, cancellationToken);
        return Ok(response);
    }
}
