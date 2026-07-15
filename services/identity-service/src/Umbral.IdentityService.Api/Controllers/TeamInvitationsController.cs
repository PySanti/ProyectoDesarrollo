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
[Authorize(Policy = "Participante")]
public sealed class TeamInvitationsController : ControllerBase
{
    private readonly ISender _sender;

    public TeamInvitationsController(ISender sender) => _sender = sender;

    [HttpPost("invitations")]
    public async Task<IActionResult> Enviar(
        [FromBody] EnviarInvitacionRequest request,
        [FromServices] IValidator<EnviarInvitacionEquipoCommand> validator,
        CancellationToken cancellationToken)
    {
        if (!AuthenticatedUserClaims.TryGetUserId(User, out var actorUserId))
            return Unauthorized();

        var command = new EnviarInvitacionEquipoCommand(actorUserId, request.InvitadoUserId);
        if (await ValidateAsync(validator, command, cancellationToken) is { } problem)
            return problem;

        var response = await _sender.Send(command, cancellationToken);
        return Created($"/identity/teams/invitations/{response.InvitacionEquipoId}", response);
    }

    [HttpGet("invitations")]
    public async Task<IActionResult> Recibidas(CancellationToken cancellationToken)
    {
        if (!AuthenticatedUserClaims.TryGetUserId(User, out var actorUserId))
            return Unauthorized();

        var response = await _sender.Send(new GetInvitacionesRecibidasQuery(actorUserId), cancellationToken);
        return Ok(response);
    }

    [HttpPost("invitations/{invitacionId:guid}/acceptance")]
    public async Task<IActionResult> Aceptar(
        Guid invitacionId,
        [FromServices] IValidator<AceptarInvitacionEquipoCommand> validator,
        CancellationToken cancellationToken)
    {
        if (!AuthenticatedUserClaims.TryGetUserId(User, out var actorUserId))
            return Unauthorized();

        var command = new AceptarInvitacionEquipoCommand(actorUserId, invitacionId);
        if (await ValidateAsync(validator, command, cancellationToken) is { } problem)
            return problem;

        var response = await _sender.Send(command, cancellationToken);
        return Ok(response);
    }

    [HttpPost("invitations/{invitacionId:guid}/rejection")]
    public async Task<IActionResult> Rechazar(
        Guid invitacionId,
        [FromServices] IValidator<RechazarInvitacionEquipoCommand> validator,
        CancellationToken cancellationToken)
    {
        if (!AuthenticatedUserClaims.TryGetUserId(User, out var actorUserId))
            return Unauthorized();

        var command = new RechazarInvitacionEquipoCommand(actorUserId, invitacionId);
        if (await ValidateAsync(validator, command, cancellationToken) is { } problem)
            return problem;

        var response = await _sender.Send(command, cancellationToken);
        return Ok(response);
    }

    [HttpGet("eligible-participants")]
    public async Task<IActionResult> Elegibles(CancellationToken cancellationToken)
    {
        if (!AuthenticatedUserClaims.TryGetUserId(User, out var actorUserId))
            return Unauthorized();

        var response = await _sender.Send(new GetParticipantesElegiblesQuery(actorUserId), cancellationToken);
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
