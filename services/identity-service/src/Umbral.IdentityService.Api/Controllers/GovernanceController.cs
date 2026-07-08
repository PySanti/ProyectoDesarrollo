using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Umbral.IdentityService.Api.Contracts;
using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Application.Queries;

namespace Umbral.IdentityService.Api.Controllers;

[ApiController]
[Route("identity/governance")]
[Authorize(Policy = "AdminOnly")]
public sealed class GovernanceController : ControllerBase
{
    private readonly ISender _sender;

    public GovernanceController(ISender sender) => _sender = sender;

    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new GetPermisosRolesQuery(), cancellationToken);
        return Ok(response);
    }

    [HttpPut("roles/{rol}/permisos")]
    public async Task<IActionResult> ActualizarPermisos(
        string rol,
        [FromBody] ActualizarPermisosRolRequest request,
        [FromServices] IValidator<ActualizarPermisosRolCommand> validator,
        CancellationToken cancellationToken)
    {
        var command = new ActualizarPermisosRolCommand(rol, request.Permisos);
        if (await ValidateAsync(validator, command, cancellationToken) is { } problem)
            return problem;

        var response = await _sender.Send(command, cancellationToken);
        return Ok(response);
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
