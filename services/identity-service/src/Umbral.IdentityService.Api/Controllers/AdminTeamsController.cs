using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Umbral.IdentityService.Api.Contracts;
using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Application.Queries;

namespace Umbral.IdentityService.Api.Controllers;

[ApiController]
[Route("identity/admin/teams")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminTeamsController : ControllerBase
{
    private readonly ISender _sender;

    public AdminTeamsController(ISender sender) => _sender = sender;

    [HttpGet]
    public async Task<IActionResult> GetEquipos(CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new GetEquiposAdminQuery(), cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetEquipoById(Guid id, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new GetEquipoAdminByIdQuery(id), cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPost]
    public async Task<IActionResult> Crear(
        [FromBody] CrearEquipoAdminRequest request,
        [FromServices] IValidator<CrearEquipoAdminCommand> validator,
        CancellationToken cancellationToken)
    {
        var command = new CrearEquipoAdminCommand(request.NombreEquipo, request.LiderUserId);
        if (await ValidateAsync(validator, command, cancellationToken) is { } problem)
            return problem;

        var response = await _sender.Send(command, cancellationToken);
        return Created($"/identity/admin/teams/{response.EquipoId}", response);
    }

    [HttpPatch("{id:guid}/name")]
    public async Task<IActionResult> Renombrar(
        Guid id,
        [FromBody] RenombrarEquipoRequest request,
        [FromServices] IValidator<RenombrarEquipoAdminCommand> validator,
        CancellationToken cancellationToken)
    {
        var command = new RenombrarEquipoAdminCommand(id, request.NombreEquipo);
        if (await ValidateAsync(validator, command, cancellationToken) is { } problem)
            return problem;

        var response = await _sender.Send(command, cancellationToken);
        return Ok(response);
    }

    [HttpPatch("{id:guid}/leadership")]
    public async Task<IActionResult> ReasignarLiderazgo(
        Guid id,
        [FromBody] ReasignarLiderazgoAdminRequest request,
        [FromServices] IValidator<ReasignarLiderazgoAdminCommand> validator,
        CancellationToken cancellationToken)
    {
        var command = new ReasignarLiderazgoAdminCommand(id, request.NuevoLiderUserId);
        if (await ValidateAsync(validator, command, cancellationToken) is { } problem)
            return problem;

        var response = await _sender.Send(command, cancellationToken);
        return Ok(response);
    }

    [HttpPatch("{id:guid}/estado")]
    public async Task<IActionResult> CambiarEstado(
        Guid id,
        [FromBody] CambiarEstadoEquipoRequest request,
        [FromServices] IValidator<CambiarEstadoEquipoAdminCommand> validator,
        CancellationToken cancellationToken)
    {
        var command = new CambiarEstadoEquipoAdminCommand(id, request.Estado);
        if (await ValidateAsync(validator, command, cancellationToken) is { } problem)
            return problem;

        var response = await _sender.Send(command, cancellationToken);
        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new EliminarEquipoAdminCommand(id), cancellationToken);
        return NoContent();
    }

    private async Task<IActionResult?> ValidateAsync<T>(
        IValidator<T> validator,
        T instance,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(instance, cancellationToken);
        if (validation.IsValid)
            return null;

        foreach (var error in validation.Errors)
            ModelState.AddModelError(error.PropertyName, error.ErrorMessage);

        return new BadRequestObjectResult(new ValidationProblemDetails(ModelState));
    }
}
