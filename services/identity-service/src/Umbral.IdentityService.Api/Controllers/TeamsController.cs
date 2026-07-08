using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Umbral.IdentityService.Api.Contracts;
using Umbral.IdentityService.Api.Utils;
using Umbral.IdentityService.Application.Commands;
using Umbral.IdentityService.Application.Queries;

namespace Umbral.IdentityService.Api.Controllers;

[ApiController]
[Route("identity/teams")]
[Authorize(Policy = "GestionarEquipos")]
public sealed class TeamsController : ControllerBase
{
    private readonly ISender _sender;

    public TeamsController(ISender sender) => _sender = sender;

    [HttpPost]
    public async Task<IActionResult> Crear(
        [FromBody] CrearEquipoRequest request,
        [FromServices] IValidator<CrearEquipoCommand> validator,
        CancellationToken cancellationToken)
    {
        if (!AuthenticatedUserClaims.TryGetUserId(User, out var actorUserId))
            return Unauthorized();

        var command = new CrearEquipoCommand(actorUserId, request.NombreEquipo);
        if (await ValidateAsync(validator, command, cancellationToken) is { } problem)
            return problem;

        var response = await _sender.Send(command, cancellationToken);
        return Created($"/identity/teams/{response.EquipoId}", response);
    }

    [HttpGet("mine")]
    public async Task<IActionResult> MiEquipo(CancellationToken cancellationToken)
    {
        if (!AuthenticatedUserClaims.TryGetUserId(User, out var actorUserId))
            return Unauthorized();

        var equipo = await _sender.Send(new ObtenerMiEquipoQuery(actorUserId), cancellationToken);
        return equipo is null ? NotFound() : Ok(equipo);
    }

    [HttpGet("mine/history")]
    public async Task<IActionResult> MiHistorial(CancellationToken cancellationToken)
    {
        if (!AuthenticatedUserClaims.TryGetUserId(User, out var actorUserId))
            return Unauthorized();

        var response = await _sender.Send(new GetHistorialNombresEquipoQuery(actorUserId), cancellationToken);
        return Ok(response);
    }

    [HttpDelete("membership")]
    public async Task<IActionResult> Salir(
        [FromServices] IValidator<SalirDeEquipoCommand> validator,
        CancellationToken cancellationToken)
    {
        if (!AuthenticatedUserClaims.TryGetUserId(User, out var actorUserId))
            return Unauthorized();

        var command = new SalirDeEquipoCommand(actorUserId);
        if (await ValidateAsync(validator, command, cancellationToken) is { } problem)
            return problem;

        var response = await _sender.Send(command, cancellationToken);
        return Ok(response);
    }

    [HttpPatch("leadership")]
    public async Task<IActionResult> TransferirLiderazgo(
        [FromBody] TransferirLiderazgoRequest request,
        [FromServices] IValidator<TransferirLiderazgoCommand> validator,
        CancellationToken cancellationToken)
    {
        if (!AuthenticatedUserClaims.TryGetUserId(User, out var actorUserId))
            return Unauthorized();

        var command = new TransferirLiderazgoCommand(actorUserId, request.NuevoLiderUserId);
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
